using Discord;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Extensions;
using NadekoBot.Modules.Xp.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageSharp;
using Image = ImageSharp.Image;
using SixLabors.Fonts;
using System.IO;
using SixLabors.Primitives;
using System.Net.Http;
using ImageSharp.Drawing.Pens;
using ImageSharp.Drawing.Brushes;

namespace NadekoBot.Modules.Xp.Services
{
    public class XpService : INService, IUnloadableService
    {
        private enum NotifOf { Server, Global } // is it a server level-up or global level-up notification

        private readonly DbService _db;
        private readonly CommandHandler _cmd;
        private readonly IBotConfigProvider _bc;
        private readonly IImageCache _images;
        private readonly Logger _log;
        private readonly NadekoStrings _strings;
        private readonly IDataCache _cache;
        private readonly FontProvider _fonts;
        private readonly IBotCredentials _creds;
        private readonly ICurrencyService _cs;
        public const int XP_REQUIRED_LVL_1 = 36;

        private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> _excludedRoles
            = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>();

        private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> _excludedChannels
            = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>();

        private readonly ConcurrentHashSet<ulong> _excludedServers 
            = new ConcurrentHashSet<ulong>();

        private readonly ConcurrentHashSet<ulong> _rewardedUsers 
            = new ConcurrentHashSet<ulong>();

        private readonly ConcurrentQueue<UserCacheItem> _addMessageXp 
            = new ConcurrentQueue<UserCacheItem>();

        private readonly Timer _updateXpTimer;
        private readonly CancellationTokenSource _clearRewardTimerTokenSource;
        private readonly Task _clearRewardTimer;
        private readonly HttpClient http = new HttpClient();

        public XpService(CommandHandler cmd, IBotConfigProvider bc,
            NadekoBot bot, DbService db, NadekoStrings strings, IDataCache cache,
            FontProvider fonts, IBotCredentials creds, ICurrencyService cs)
        {
            _db = db;
            _cmd = cmd;
            _bc = bc;
            _images = cache.LocalImages;
            _log = LogManager.GetCurrentClassLogger();
            _strings = strings;
            _cache = cache;
            _fonts = fonts;
            _creds = creds;
            _cs = cs;

            //load settings
            var allGuildConfigs = bot.AllGuildConfigs.Where(x => x.XpSettings != null);
            _excludedChannels = allGuildConfigs
                .ToDictionary(
                    x => x.GuildId,
                    x => new ConcurrentHashSet<ulong>(x.XpSettings
                            .ExclusionList
                            .Where(ex => ex.ItemType == ExcludedItemType.Channel)
                            .Select(ex => ex.ItemId)
                            .Distinct()))
                .ToConcurrent();

            _excludedRoles = allGuildConfigs
                .ToDictionary(
                    x => x.GuildId,
                    x => new ConcurrentHashSet<ulong>(x.XpSettings
                            .ExclusionList
                            .Where(ex => ex.ItemType == ExcludedItemType.Role)
                            .Select(ex => ex.ItemId)
                            .Distinct()))
                .ToConcurrent();

            _excludedServers = new ConcurrentHashSet<ulong>(
                allGuildConfigs.Where(x => x.XpSettings.ServerExcluded)
                               .Select(x => x.GuildId));

            _cmd.OnMessageNoTrigger += _cmd_OnMessageNoTrigger;

            _updateXpTimer = new Timer(async _ =>
            {
                try
                {
                    var toNotify = new List<(IMessageChannel MessageChannel, IUser User, int Level, XpNotificationType NotifyType, NotifOf NotifOf)>();
                    var roleRewards = new Dictionary<ulong, List<XpRoleReward>>();
                    var curRewards = new Dictionary<ulong, List<XpCurrencyReward>>();

                    var toAddTo = new List<UserCacheItem>();
                    while (_addMessageXp.TryDequeue(out var usr))
                        toAddTo.Add(usr);

                    var group = toAddTo.GroupBy(x => (GuildId: x.Guild.Id, User: x.User));
                    if (toAddTo.Count == 0)
                        return;

                    using (var uow = _db.UnitOfWork)
                    {
                        foreach (var item in group)
                        {
                            var xp = item.Select(x => bc.BotConfig.XpPerMessage).Sum();

                            //1. Mass query discord users and userxpstats and get them from local dict
                            //2. (better but much harder) Move everything to the database, and get old and new xp
                            // amounts for every user (in order to give rewards)

                            var usr = uow.Xp.GetOrCreateUser(item.Key.GuildId, item.Key.User.Id);
                            var du = uow.DiscordUsers.GetOrCreate(item.Key.User);

                            var globalXp = du.TotalXp;
                            var oldGlobalLevelData = new LevelStats(globalXp);
                            var newGlobalLevelData = new LevelStats(globalXp + xp);

                            var oldGuildLevelData = new LevelStats(usr.Xp + usr.AwardedXp);
                            usr.Xp += xp;
                            du.TotalXp += xp;
                            if (du.Club != null)
                                du.Club.Xp += xp;
                            var newGuildLevelData = new LevelStats(usr.Xp + usr.AwardedXp);

                            if (oldGlobalLevelData.Level < newGlobalLevelData.Level)
                            {
                                du.LastLevelUp = DateTime.UtcNow;
                                var first = item.First();
                                if (du.NotifyOnLevelUp != XpNotificationType.None)
                                    toNotify.Add((first.Channel, first.User, newGlobalLevelData.Level, du.NotifyOnLevelUp, NotifOf.Global));
                            }

                            if (oldGuildLevelData.Level < newGuildLevelData.Level)
                            {
                                usr.LastLevelUp = DateTime.UtcNow;
                                //send level up notification
                                var first = item.First();
                                if (usr.NotifyOnLevelUp != XpNotificationType.None)
                                    toNotify.Add((first.Channel, first.User, newGuildLevelData.Level, usr.NotifyOnLevelUp, NotifOf.Server));

                                //give role
                                if (!roleRewards.TryGetValue(usr.GuildId, out var rrews))
                                {
                                    rrews = uow.GuildConfigs.XpSettingsFor(usr.GuildId).RoleRewards.ToList();
                                    roleRewards.Add(usr.GuildId, rrews);
                                }

                                if (!curRewards.TryGetValue(usr.GuildId, out var crews))
                                {
                                    crews = uow.GuildConfigs.XpSettingsFor(usr.GuildId).CurrencyRewards.ToList();
                                    curRewards.Add(usr.GuildId, crews);
                                }

                                var rrew = rrews.FirstOrDefault(x => x.Level == newGuildLevelData.Level);
                                if (rrew != null)
                                {
                                    var role = first.User.Guild.GetRole(rrew.RoleId);
                                    if (role != null)
                                    {
                                        var __ = first.User.AddRoleAsync(role);
                                    }
                                }
                                //get currency reward for this level
                                var crew = crews.FirstOrDefault(x => x.Level == newGuildLevelData.Level);
                                if (crew != null)
                                {
                                    //give the user the reward if it exists
                                    await _cs.AddAsync(item.Key.User.Id, "Level-up Reward", crew.Amount);
                                }
                            }
                        }

                        uow.Complete();
                    }

                    await Task.WhenAll(toNotify.Select(async x =>
                    {
                        if (x.NotifOf == NotifOf.Server)
                        {
                            if (x.NotifyType == XpNotificationType.Dm)
                            {
                                var chan = await x.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                                if (chan != null)
                                    await chan.SendConfirmAsync(_strings.GetText("level_up_dm",
                                        (x.MessageChannel as ITextChannel)?.GuildId,
                                        "xp",
                                        x.User.Mention, Format.Bold(x.Level.ToString()),
                                        Format.Bold((x.MessageChannel as ITextChannel)?.Guild.ToString() ?? "-")))
                                        .ConfigureAwait(false);
                            }
                            else // channel
                            {
                                await x.MessageChannel.SendConfirmAsync(_strings.GetText("level_up_channel",
                                          (x.MessageChannel as ITextChannel)?.GuildId,
                                          "xp",
                                          x.User.Mention, Format.Bold(x.Level.ToString())))
                                          .ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            IMessageChannel chan;
                            if (x.NotifyType == XpNotificationType.Dm)
                            {
                                chan = await x.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                            }
                            else // channel
                            {
                                chan = x.MessageChannel;
                            }
                            await chan.SendConfirmAsync(_strings.GetText("level_up_global",
                                          (x.MessageChannel as ITextChannel)?.GuildId,
                                          "xp",
                                          x.User.Mention, Format.Bold(x.Level.ToString())))
                                            .ConfigureAwait(false);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
            _clearRewardTimerTokenSource = new CancellationTokenSource();
            var token = _clearRewardTimerTokenSource.Token;
            //just a first line, in order to prevent queries. But since other shards can try to do this too,
            //i'll check in the db too.
            _clearRewardTimer = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    _rewardedUsers.Clear();
                    
                    await Task.Delay(TimeSpan.FromMinutes(_bc.BotConfig.XpMinutesTimeout));
                }
            }, token);
        }

        public void SetCurrencyReward(ulong guildId, int level, int amount)
        {
            using (var uow = _db.UnitOfWork)
            {
                var settings = uow.GuildConfigs.XpSettingsFor(guildId);

                if (amount <= 0)
                {
                    var toRemove = settings.CurrencyRewards.FirstOrDefault(x => x.Level == level);
                    if (toRemove != null)
                    {
                        uow._context.Remove(toRemove);
                        settings.CurrencyRewards.Remove(toRemove);
                    }
                }
                else
                {

                    var rew = settings.CurrencyRewards.FirstOrDefault(x => x.Level == level);

                    if (rew != null)
                        rew.Amount = amount;
                    else
                        settings.CurrencyRewards.Add(new XpCurrencyReward()
                        {
                            Level = level,
                            Amount = amount,
                        });
                }

                uow.Complete();
            }
        }

        public IEnumerable<XpCurrencyReward> GetCurrencyRewards(ulong id)
        {
            using (var uow = _db.UnitOfWork)
            {
                return uow.GuildConfigs.XpSettingsFor(id)
                    .CurrencyRewards
                    .ToArray();
            }
        }

        public IEnumerable<XpRoleReward> GetRoleRewards(ulong id)
        {
            using (var uow = _db.UnitOfWork)
            {
                return uow.GuildConfigs.XpSettingsFor(id)
                    .RoleRewards
                    .ToArray();
            }
        }

        public void SetRoleReward(ulong guildId, int level, ulong? roleId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var settings = uow.GuildConfigs.XpSettingsFor(guildId);

                if (roleId == null)
                {
                    var toRemove = settings.RoleRewards.FirstOrDefault(x => x.Level == level);
                    if (toRemove != null)
                    {
                        uow._context.Remove(toRemove);
                        settings.RoleRewards.Remove(toRemove);
                    }
                }
                else
                {

                    var rew = settings.RoleRewards.FirstOrDefault(x => x.Level == level);

                    if (rew != null)
                        rew.RoleId = roleId.Value;
                    else
                        settings.RoleRewards.Add(new XpRoleReward()
                        {
                            Level = level,
                            RoleId = roleId.Value,
                        });
                }

                uow.Complete();
            }
        }

        public UserXpStats[] GetUserXps(ulong guildId, int page)
        {
            using (var uow = _db.UnitOfWork)
            {
                return uow.Xp.GetUsersFor(guildId, page);
            }
        }

        public DiscordUser[] GetUserXps(int page)
        {
            using (var uow = _db.UnitOfWork)
            {
                return uow.DiscordUsers.GetUsersXpLeaderboardFor(page);
            }
        }

        public async Task ChangeNotificationType(ulong userId, ulong guildId, XpNotificationType type)
        {
            using (var uow = _db.UnitOfWork)
            {
                var user = uow.Xp.GetOrCreateUser(guildId, userId);
                user.NotifyOnLevelUp = type;
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async Task ChangeNotificationType(IUser user, XpNotificationType type)
        {
            using (var uow = _db.UnitOfWork)
            {
                var du = uow.DiscordUsers.GetOrCreate(user);
                du.NotifyOnLevelUp = type;
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private Task _cmd_OnMessageNoTrigger(IUserMessage arg)
        {
            if (!(arg.Author is SocketGuildUser user) || user.IsBot)
                return Task.CompletedTask;

            var _ = Task.Run(() =>
            {
                if (_excludedChannels.TryGetValue(user.Guild.Id, out var chans) &&
                    chans.Contains(arg.Channel.Id))
                    return;

                if (_excludedServers.Contains(user.Guild.Id))
                    return;

                if (_excludedRoles.TryGetValue(user.Guild.Id, out var roles) &&
                    user.Roles.Any(x => roles.Contains(x.Id)))
                    return;

                if (!arg.Content.Contains(' ') && arg.Content.Length < 5)
                    return;

                if (!SetUserRewarded(user.Id))
                    return;

                _addMessageXp.Enqueue(new UserCacheItem { Guild = user.Guild, Channel = arg.Channel, User = user });
            });
            return Task.CompletedTask;
        }

        public void AddXp(ulong userId, ulong guildId, int amount)
        {
            using (var uow = _db.UnitOfWork)
            {
                var usr = uow.Xp.GetOrCreateUser(guildId, userId);

                usr.AwardedXp += amount;

                uow.Complete();
            }
        }

        public bool IsServerExcluded(ulong id)
        {
            return _excludedServers.Contains(id);
        }

        public IEnumerable<ulong> GetExcludedRoles(ulong id)
        {
            if (_excludedRoles.TryGetValue(id, out var val))
                return val.ToArray();

            return Enumerable.Empty<ulong>();
        }

        public IEnumerable<ulong> GetExcludedChannels(ulong id)
        {
            if (_excludedChannels.TryGetValue(id, out var val))
                return val.ToArray();

            return Enumerable.Empty<ulong>();
        }

        private bool SetUserRewarded(ulong userId)
        {
            var r = _cache.Redis.GetDatabase();
            var key = $"{_creds.RedisKey()}_user_xp_gain_{userId}";

            return r.StringSet(key, 
                true, 
                TimeSpan.FromMinutes(_bc.BotConfig.XpMinutesTimeout), 
                StackExchange.Redis.When.NotExists);
        }

        public async Task<FullUserStats> GetUserStatsAsync(IGuildUser user)
        {
            DiscordUser du;
            UserXpStats stats = null;
            int totalXp;
            int globalRank;
            int guildRank;
            using (var uow = _db.UnitOfWork)
            {
                du = uow.DiscordUsers.GetOrCreate(user);
                totalXp = du.TotalXp;

                var t1 = Task.Run(() => stats = uow.Xp.GetOrCreateUser(user.GuildId, user.Id));
                var ranks = await Task.WhenAll(
                    uow.DiscordUsers.GetUserGlobalRankingAsync(user.Id),
                    uow.Xp.GetUserGuildRankingAsync(user.Id, user.GuildId));
                await t1;
                globalRank = ranks[0];
                guildRank = ranks[1];
            }

            return new FullUserStats(du,
                stats,
                new LevelStats(totalXp),
                new LevelStats(stats.Xp + stats.AwardedXp),
                globalRank,
                guildRank);
        }

        public static (int Level, int LevelXp, int LevelRequiredXp) GetLevelData(UserXpStats stats)
        {
            var baseXp = XpService.XP_REQUIRED_LVL_1;

            var required = baseXp;
            var totalXp = 0;
            var lvl = 1;
            while (true)
            {
                required = (int)(baseXp + baseXp / 4.0 * (lvl - 1));

                if (required + totalXp > stats.Xp)
                    break;

                totalXp += required;
                lvl++;
            }

            return (lvl - 1, stats.Xp - totalXp, required);
        }

        public bool ToggleExcludeServer(ulong id)
        {
            using (var uow = _db.UnitOfWork)
            {
                var xpSetting = uow.GuildConfigs.XpSettingsFor(id);
                if (_excludedServers.Add(id))
                {
                    xpSetting.ServerExcluded = true;
                    uow.Complete();
                    return true;
                }

                _excludedServers.TryRemove(id);
                xpSetting.ServerExcluded = false;
                uow.Complete();
                return false;
            }
        }

        public bool ToggleExcludeRole(ulong guildId, ulong rId)
        {
            var roles = _excludedRoles.GetOrAdd(guildId, _ => new ConcurrentHashSet<ulong>());
            using (var uow = _db.UnitOfWork)
            {
                var xpSetting = uow.GuildConfigs.XpSettingsFor(guildId);
                var excludeObj = new ExcludedItem
                {
                    ItemId = rId,
                    ItemType = ExcludedItemType.Role,
                };

                if (roles.Add(rId))
                {

                    if (xpSetting.ExclusionList.Add(excludeObj))
                    {
                        uow.Complete();
                    }

                    return true;
                }
                else
                {
                    roles.TryRemove(rId);

                    if (xpSetting.ExclusionList.Remove(excludeObj))
                    {
                        uow.Complete();
                    }

                    return false;
                }
            }
        }

        public bool ToggleExcludeChannel(ulong guildId, ulong chId)
        {
            var channels = _excludedChannels.GetOrAdd(guildId, _ => new ConcurrentHashSet<ulong>());
            using (var uow = _db.UnitOfWork)
            {
                var xpSetting = uow.GuildConfigs.XpSettingsFor(guildId);
                var excludeObj = new ExcludedItem
                {
                    ItemId = chId,
                    ItemType = ExcludedItemType.Channel,
                };

                if (channels.Add(chId))
                {

                    if (xpSetting.ExclusionList.Add(excludeObj))
                    {
                        uow.Complete();
                    }

                    return true;
                }
                else
                {
                    channels.TryRemove(chId);

                    if (xpSetting.ExclusionList.Remove(excludeObj))
                    {
                        uow.Complete();
                    }

                    return false;
                }
            }
        }

        public async Task<MemoryStream> GenerateImageAsync(IGuildUser user)
        {
            var stats = await GetUserStatsAsync(user);
            return await GenerateImageAsync(stats);
        }


        public Task<MemoryStream> GenerateImageAsync(FullUserStats stats) => Task.Run(async () =>
        {
            using (var img = Image.Load(_images.XpCard))
            {
                var username = stats.User.ToString();
                var usernameFont = _fonts.UsernameFontFamily
                    .CreateFont(username.Length <= 6
                        ? 50
                        : 50 - username.Length);

                img.DrawText("@" + username, usernameFont, Rgba32.White,
                    new PointF(130, 5));
                // level

                img.DrawText(stats.Global.Level.ToString(), _fonts.LevelFont, Rgba32.White,
                    new PointF(47, 137));

                img.DrawText(stats.Guild.Level.ToString(), _fonts.LevelFont, Rgba32.White,
                    new PointF(47, 285));

                //club name

                var clubName = stats.User.Club?.ToString() ?? "-";

                var clubFont = _fonts.ClubFontFamily
                    .CreateFont(clubName.Length <= 8
                        ? 35
                        : 35 - (clubName.Length / 2));

                img.DrawText(clubName, clubFont, Rgba32.White,
                    new PointF(650 - clubName.Length * 10, 40));

                var pen = new Pen<Rgba32>(Rgba32.Black, 1);
                var brush = Brushes.Solid<Rgba32>(Rgba32.White);
                var xpBgBrush = Brushes.Solid<Rgba32>(new Rgba32(0, 0, 0, 0.4f));

                var global = stats.Global;
                var guild = stats.Guild;

                //xp bar

                img.FillPolygon(xpBgBrush, new[] {
                    new PointF(321, 104),
                    new PointF(321 + (450 * (global.LevelXp / (float)global.RequiredXp)), 104),
                    new PointF(286 + (450 * (global.LevelXp / (float)global.RequiredXp)), 235),
                    new PointF(286, 235),
                });
                img.DrawText($"{global.LevelXp}/{global.RequiredXp}", _fonts.XpFont, brush, pen,
                    new PointF(430, 130));

                img.FillPolygon(xpBgBrush, new[] {
                    new PointF(282, 248),
                    new PointF(282 + (450 * (guild.LevelXp / (float)guild.RequiredXp)), 248),
                    new PointF(247 + (450 * (guild.LevelXp / (float)guild.RequiredXp)), 379),
                    new PointF(247, 379),
                });
                img.DrawText($"{guild.LevelXp}/{guild.RequiredXp}", _fonts.XpFont, brush, pen,
                    new PointF(400, 270));

                if (stats.FullGuildStats.AwardedXp != 0)
                {
                    var sign = stats.FullGuildStats.AwardedXp > 0
                        ? "+ "
                        : "";
                    img.DrawText($"({sign}{stats.FullGuildStats.AwardedXp})", _fonts.AwardedFont, brush, pen,
                        new PointF(445 - (Math.Max(0, (stats.FullGuildStats.AwardedXp.ToString().Length - 2)) * 5), 335));
                }

                //ranking

                img.DrawText(stats.GlobalRanking.ToString(), _fonts.RankFont, Rgba32.White,
                    new PointF(148, 170));

                img.DrawText(stats.GuildRanking.ToString(), _fonts.RankFont, Rgba32.White,
                    new PointF(148, 317));

                //time on this level

                string GetTimeSpent(DateTime time)
                {
                    var offset = DateTime.UtcNow - time;
                    return $"{offset.Days}d{offset.Hours}h{offset.Minutes}m";
                }

                img.DrawText(GetTimeSpent(stats.User.LastLevelUp), _fonts.TimeFont, Rgba32.White,
                    new PointF(50, 197));

                img.DrawText(GetTimeSpent(stats.FullGuildStats.LastLevelUp), _fonts.TimeFont, Rgba32.White,
                    new PointF(50, 344));
                //avatar

                if (stats.User.AvatarId != null)
                {
                    try
                    {
                        var avatarUrl = stats.User.RealAvatarUrl();

                        var (succ, data) = await _cache.TryGetImageDataAsync(avatarUrl);
                        if (!succ)
                        {
                            using (var temp = await http.GetStreamAsync(avatarUrl))
                            using (var tempDraw = Image.Load(temp).Resize(69, 70))
                            {
                                tempDraw.ApplyRoundedCorners(35);
                                data = tempDraw.ToStream().ToArray();
                            }

                            await _cache.SetImageDataAsync(avatarUrl, data);
                        }
                        using (var toDraw = Image.Load(data))
                        {
                            img.DrawImage(toDraw,
                                1,
                                new Size(69, 70),
                                new Point(32, 10));
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }

                //club image
                await DrawClubImage(img, stats).ConfigureAwait(false);
                var s = img.Resize(432, 211).ToStream();
                return s;
            }
        });


        private async Task DrawClubImage(Image<Rgba32> img, FullUserStats stats)
        {
            if (!string.IsNullOrWhiteSpace(stats.User.Club?.ImageUrl))
            {
                var imgUrl = stats.User.Club.ImageUrl;
                try
                {
                    var (succ, data) = await _cache.TryGetImageDataAsync(imgUrl);
                    if (!succ)
                    {
                        using (var temp = await http.GetAsync(imgUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            if (temp.Content.Headers.ContentType.MediaType != "image/png"
                                && temp.Content.Headers.ContentType.MediaType != "image/jpeg"
                                && temp.Content.Headers.ContentType.MediaType != "image/gif")
                                return;
                            using (var tempDraw = Image.Load(await temp.Content.ReadAsStreamAsync()).Resize(45, 45))
                            {
                                tempDraw.ApplyRoundedCorners(22.5f);
                                data = tempDraw.ToStream().ToArray();
                            }
                        }

                        await _cache.SetImageDataAsync(imgUrl, data);
                    }
                    using (var toDraw = Image.Load(data))
                    {
                        img.DrawImage(toDraw,
                            1,
                            new Size(45, 45),
                            new Point(722, 25));
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }
        }

        public Task Unload()
        {
            _cmd.OnMessageNoTrigger -= _cmd_OnMessageNoTrigger;

            if (!_clearRewardTimerTokenSource.IsCancellationRequested)
                _clearRewardTimerTokenSource.Cancel();

            _updateXpTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _clearRewardTimerTokenSource.Dispose();
            return Task.CompletedTask;
        }
    }
}
