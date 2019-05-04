using Discord;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Collections;
using NadekoBot.Core.Modules.Xp.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Extensions;
using NadekoBot.Modules.Xp.Common;
using Newtonsoft.Json;
using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Drawing.Brushes;
using SixLabors.ImageSharp.Processing.Drawing.Pens;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.ImageSharp.Processing.Transforms;
using SixLabors.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

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

        private readonly ConcurrentQueue<UserCacheItem> _addMessageXp
            = new ConcurrentQueue<UserCacheItem>();

        private readonly Task updateXpTask;
        private readonly IHttpClientFactory _httpFactory;
        private XpTemplate _template;

        public XpService(DiscordSocketClient client, CommandHandler cmd, IBotConfigProvider bc,
            NadekoBot bot, DbService db, NadekoStrings strings, IDataCache cache,
            FontProvider fonts, IBotCredentials creds, ICurrencyService cs, IHttpClientFactory http)
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
            _httpFactory = http;
            InternalReloadXpTemplate();

            if (client.ShardId == 0)
            {
                var sub = _cache.Redis.GetSubscriber();
                sub.Subscribe(_creds.RedisKey() + "_reload_xp_template",
                    (ch, val) => InternalReloadXpTemplate());
            }
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

            updateXpTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        var toNotify = new List<(IMessageChannel MessageChannel, IUser User, int Level, XpNotificationLocation NotifyType, NotifOf NotifOf)>();
                        var roleRewards = new Dictionary<ulong, List<XpRoleReward>>();
                        var curRewards = new Dictionary<ulong, List<XpCurrencyReward>>();

                        var toAddTo = new List<UserCacheItem>();
                        while (_addMessageXp.TryDequeue(out var usr))
                            toAddTo.Add(usr);

                        var group = toAddTo.GroupBy(x => (GuildId: x.Guild.Id, x.User));
                        if (toAddTo.Count == 0)
                            continue;

                        using (var uow = _db.GetDbContext())
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
                                    if (du.NotifyOnLevelUp != XpNotificationLocation.None)
                                        toNotify.Add((first.Channel, first.User, newGlobalLevelData.Level, du.NotifyOnLevelUp, NotifOf.Global));
                                }

                                if (oldGuildLevelData.Level < newGuildLevelData.Level)
                                {
                                    usr.LastLevelUp = DateTime.UtcNow;
                                    //send level up notification
                                    var first = item.First();
                                    if (usr.NotifyOnLevelUp != XpNotificationLocation.None)
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

                            uow.SaveChanges();
                        }

                        await Task.WhenAll(toNotify.Select(async x =>
                        {
                            if (x.NotifOf == NotifOf.Server)
                            {
                                if (x.NotifyType == XpNotificationLocation.Dm)
                                {
                                    var chan = await x.User.GetOrCreateDMChannelAsync();
                                    if (chan != null)
                                        await chan.SendConfirmAsync(_strings.GetText("level_up_dm",
                                            (x.MessageChannel as ITextChannel)?.GuildId,
                                            "xp",
                                            x.User.Mention, Format.Bold(x.Level.ToString()),
                                            Format.Bold((x.MessageChannel as ITextChannel)?.Guild.ToString() ?? "-")));
                                }
                                else // channel
                                {
                                    await x.MessageChannel.SendConfirmAsync(_strings.GetText("level_up_channel",
                                              (x.MessageChannel as ITextChannel)?.GuildId,
                                              "xp",
                                              x.User.Mention, Format.Bold(x.Level.ToString())));
                                }
                            }
                            else
                            {
                                IMessageChannel chan;
                                if (x.NotifyType == XpNotificationLocation.Dm)
                                {
                                    chan = await x.User.GetOrCreateDMChannelAsync();
                                }
                                else // channel
                                {
                                    chan = x.MessageChannel;
                                }
                                await chan.SendConfirmAsync(_strings.GetText("level_up_global",
                                              (x.MessageChannel as ITextChannel)?.GuildId,
                                              "xp",
                                              x.User.Mention, Format.Bold(x.Level.ToString())));
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }
            });
        }

        private void InternalReloadXpTemplate()
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new RequireObjectPropertiesContractResolver()
                };
                _template = JsonConvert.DeserializeObject<XpTemplate>(
                    File.ReadAllText("./data/xp_template.json"), settings);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                _log.Error("Xp template is invalid. Loaded default values.");
                _template = new XpTemplate();
                File.WriteAllText("./data/xp_template_backup.json",
                    JsonConvert.SerializeObject(_template, Formatting.Indented));
            }
        }

        public void ReloadXpTemplate()
        {
            var sub = _cache.Redis.GetSubscriber();
            sub.Publish(_creds.RedisKey() + "_reload_xp_template", "");
        }

        public void SetCurrencyReward(ulong guildId, int level, int amount)
        {
            using (var uow = _db.GetDbContext())
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

                uow.SaveChanges();
            }
        }

        public IEnumerable<XpCurrencyReward> GetCurrencyRewards(ulong id)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.GuildConfigs.XpSettingsFor(id)
                    .CurrencyRewards
                    .ToArray();
            }
        }

        public IEnumerable<XpRoleReward> GetRoleRewards(ulong id)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.GuildConfigs.XpSettingsFor(id)
                    .RoleRewards
                    .ToArray();
            }
        }

        public void SetRoleReward(ulong guildId, int level, ulong? roleId)
        {
            using (var uow = _db.GetDbContext())
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

                uow.SaveChanges();
            }
        }

        public UserXpStats[] GetUserXps(ulong guildId, int page)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.Xp.GetUsersFor(guildId, page);
            }
        }

        public DiscordUser[] GetUserXps(int page)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.DiscordUsers.GetUsersXpLeaderboardFor(page);
            }
        }

        public async Task ChangeNotificationType(ulong userId, ulong guildId, XpNotificationLocation type)
        {
            using (var uow = _db.GetDbContext())
            {
                var user = uow.Xp.GetOrCreateUser(guildId, userId);
                user.NotifyOnLevelUp = type;
                await uow.SaveChangesAsync();
            }
        }

        public async Task ChangeNotificationType(IUser user, XpNotificationLocation type)
        {
            using (var uow = _db.GetDbContext())
            {
                var du = uow.DiscordUsers.GetOrCreate(user);
                du.NotifyOnLevelUp = type;
                await uow.SaveChangesAsync();
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
            using (var uow = _db.GetDbContext())
            {
                var usr = uow.Xp.GetOrCreateUser(guildId, userId);

                usr.AwardedXp += amount;

                uow.SaveChanges();
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
            using (var uow = _db.GetDbContext())
            {
                du = uow.DiscordUsers.GetOrCreate(user);
                totalXp = du.TotalXp;
                globalRank = uow.DiscordUsers.GetUserGlobalRank(user.Id);
                guildRank = uow.Xp.GetUserGuildRanking(user.Id, user.GuildId);
                stats = uow.Xp.GetOrCreateUser(user.GuildId, user.Id);
                await uow.SaveChangesAsync();
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
            using (var uow = _db.GetDbContext())
            {
                var xpSetting = uow.GuildConfigs.XpSettingsFor(id);
                if (_excludedServers.Add(id))
                {
                    xpSetting.ServerExcluded = true;
                    uow.SaveChanges();
                    return true;
                }

                _excludedServers.TryRemove(id);
                xpSetting.ServerExcluded = false;
                uow.SaveChanges();
                return false;
            }
        }

        public bool ToggleExcludeRole(ulong guildId, ulong rId)
        {
            var roles = _excludedRoles.GetOrAdd(guildId, _ => new ConcurrentHashSet<ulong>());
            using (var uow = _db.GetDbContext())
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
                        uow.SaveChanges();
                    }

                    return true;
                }
                else
                {
                    roles.TryRemove(rId);

                    var toDelete = xpSetting.ExclusionList.FirstOrDefault(x => x.Equals(excludeObj));
                    if (toDelete != null)
                    {
                        uow._context.Remove(toDelete);
                        uow.SaveChanges();
                    }

                    return false;
                }
            }
        }

        public bool ToggleExcludeChannel(ulong guildId, ulong chId)
        {
            var channels = _excludedChannels.GetOrAdd(guildId, _ => new ConcurrentHashSet<ulong>());
            using (var uow = _db.GetDbContext())
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
                        uow.SaveChanges();
                    }

                    return true;
                }
                else
                {
                    channels.TryRemove(chId);

                    if (xpSetting.ExclusionList.Remove(excludeObj))
                    {
                        uow.SaveChanges();
                    }

                    return false;
                }
            }
        }

        public async Task<(Stream Image, IImageFormat Format)> GenerateXpImageAsync(IGuildUser user)
        {
            var stats = await GetUserStatsAsync(user);
            return await GenerateXpImageAsync(stats);
        }


        public Task<(Stream Image, IImageFormat Format)> GenerateXpImageAsync(FullUserStats stats) => Task.Run(async () =>
        {
            using (var img = Image.Load(_images.XpBackground, out var imageFormat))
            {
                if (_template.User.Name.Show)
                {
                    var username = stats.User.ToString();
                    var usernameFont = _fonts.NotoSans
                        .CreateFont(username.Length <= 6
                            ? _template.User.Name.FontSize
                            : _template.User.Name.FontSize - username.Length, FontStyle.Bold);

                    img.Mutate(x =>
                    {
                        x.DrawText("@" + username, usernameFont,
                            _template.User.Name.Color,
                            new PointF(_template.User.Name.Pos.X, _template.User.Name.Pos.Y));
                    });
                }

                if (_template.User.GlobalLevel.Show)
                {
                    img.Mutate(x =>
                    {
                        x.DrawText(stats.Global.Level.ToString(),
                            _fonts.NotoSans.CreateFont(_template.User.GlobalLevel.FontSize, FontStyle.Bold),
                            _template.User.GlobalLevel.Color,
                            new PointF(_template.User.GlobalLevel.Pos.X, _template.User.GlobalLevel.Pos.Y)); //level
                    });
                }

                if (_template.User.GuildLevel.Show)
                {
                    img.Mutate(x =>
                    {
                        x.DrawText(stats.Guild.Level.ToString(),
                            _fonts.NotoSans.CreateFont(_template.User.GuildLevel.FontSize, FontStyle.Bold),
                            _template.User.GuildLevel.Color,
                            new PointF(_template.User.GuildLevel.Pos.X, _template.User.GuildLevel.Pos.Y));
                    });
                }

                //club name

                if (_template.Club.Name.Show)
                {
                    var clubName = stats.User.Club?.ToString() ?? "-";

                    var clubFont = _fonts.NotoSans
                        .CreateFont(clubName.Length <= 8
                            ? _template.Club.Name.FontSize
                            : _template.Club.Name.FontSize - (clubName.Length / 2), FontStyle.Bold);

                    img.Mutate(x => x.DrawText(clubName, clubFont,
                        _template.Club.Name.Color,
                        new PointF(_template.Club.Name.Pos.X - clubName.Length * 10, _template.Club.Name.Pos.Y)));
                }


                var pen = new Pen<Rgba32>(Rgba32.Black, 1);

                var global = stats.Global;
                var guild = stats.Guild;

                //xp bar
                if (_template.User.Xp.Bar.Show)
                {
                    var xpPercent = (global.LevelXp / (float)global.RequiredXp);
                    DrawXpBar(xpPercent, _template.User.Xp.Bar.Global, img);
                    xpPercent = (guild.LevelXp / (float)guild.RequiredXp);
                    DrawXpBar(xpPercent, _template.User.Xp.Bar.Guild, img);
                }

                if (_template.User.Xp.Global.Show)
                {
                    img.Mutate(x => x.DrawText($"{global.LevelXp}/{global.RequiredXp}",
                        _fonts.NotoSans.CreateFont(_template.User.Xp.Global.FontSize, FontStyle.Bold),
                        Brushes.Solid(_template.User.Xp.Global.Color),
                        pen,
                        new PointF(_template.User.Xp.Global.Pos.X, _template.User.Xp.Global.Pos.Y)));
                }
                if (_template.User.Xp.Guild.Show)
                {
                    img.Mutate(x => x.DrawText($"{guild.LevelXp}/{guild.RequiredXp}",
                        _fonts.NotoSans.CreateFont(_template.User.Xp.Guild.FontSize, FontStyle.Bold),
                        Brushes.Solid(_template.User.Xp.Guild.Color),
                        pen,
                        new PointF(_template.User.Xp.Guild.Pos.X, _template.User.Xp.Guild.Pos.Y)));
                }
                if (stats.FullGuildStats.AwardedXp != 0 && _template.User.Xp.Awarded.Show)
                {
                    var sign = stats.FullGuildStats.AwardedXp > 0
                        ? "+ "
                        : "";
                    var awX = _template.User.Xp.Awarded.Pos.X - (Math.Max(0, (stats.FullGuildStats.AwardedXp.ToString().Length - 2)) * 5);
                    var awY = _template.User.Xp.Awarded.Pos.Y;
                    img.Mutate(x => x.DrawText($"({sign}{stats.FullGuildStats.AwardedXp})",
                        _fonts.NotoSans.CreateFont(_template.User.Xp.Awarded.FontSize, FontStyle.Bold),
                        Brushes.Solid(_template.User.Xp.Awarded.Color),
                        pen,
                        new PointF(awX, awY)));
                }

                //ranking
                if (_template.User.GlobalRank.Show)
                {
                    img.Mutate(x => x.DrawText(stats.GlobalRanking.ToString(),
                        _fonts.RankFontFamily.CreateFont(_template.User.GlobalRank.FontSize, FontStyle.Bold),
                        _template.User.GlobalRank.Color,
                        new PointF(_template.User.GlobalRank.Pos.X, _template.User.GlobalRank.Pos.Y)));
                }

                if (_template.User.GuildRank.Show)
                {
                    img.Mutate(x => x.DrawText(stats.GuildRanking.ToString(),
                        _fonts.RankFontFamily.CreateFont(_template.User.GuildRank.FontSize, FontStyle.Bold),
                        _template.User.GuildRank.Color,
                        new PointF(_template.User.GuildRank.Pos.X, _template.User.GuildRank.Pos.Y)));
                }

                //time on this level

                string GetTimeSpent(DateTime time, string format)
                {
                    var offset = DateTime.UtcNow - time;
                    return string.Format(format, offset.Days, offset.Hours, offset.Minutes);
                }

                if (_template.User.TimeOnLevel.Global.Show)
                {
                    img.Mutate(x => x.DrawText(GetTimeSpent(stats.User.LastLevelUp, _template.User.TimeOnLevel.Format),
                        _fonts.NotoSans.CreateFont(_template.User.TimeOnLevel.Global.FontSize, FontStyle.Bold),
                        _template.User.TimeOnLevel.Global.Color,
                        new PointF(_template.User.TimeOnLevel.Global.Pos.X, _template.User.TimeOnLevel.Global.Pos.Y)));
                }

                if (_template.User.TimeOnLevel.Guild.Show)
                {
                    img.Mutate(x => x.DrawText(GetTimeSpent(stats.FullGuildStats.LastLevelUp, _template.User.TimeOnLevel.Format),
                        _fonts.NotoSans.CreateFont(_template.User.TimeOnLevel.Guild.FontSize, FontStyle.Bold),
                        _template.User.TimeOnLevel.Guild.Color,
                        new PointF(_template.User.TimeOnLevel.Guild.Pos.X, _template.User.TimeOnLevel.Guild.Pos.Y)));
                }
                //avatar

                if (stats.User.AvatarId != null && _template.User.Icon.Show)
                {
                    try
                    {
                        var avatarUrl = stats.User.RealAvatarUrl(128);

                        var (succ, data) = await _cache.TryGetImageDataAsync(avatarUrl);
                        if (!succ)
                        {
                            using (var http = _httpFactory.CreateClient())
                            {
                                var avatarData = await http.GetByteArrayAsync(avatarUrl);
                                using (var tempDraw = Image.Load(avatarData))
                                {
                                    tempDraw.Mutate(x => x.Resize(_template.User.Icon.Size.X, _template.User.Icon.Size.Y));
                                    tempDraw.ApplyRoundedCorners(Math.Max(_template.User.Icon.Size.X, _template.User.Icon.Size.Y) / 2);
                                    using (var stream = tempDraw.ToStream())
                                    {
                                        data = stream.ToArray();
                                    }
                                }
                            }
                            await _cache.SetImageDataAsync(avatarUrl, data);
                        }
                        using (var toDraw = Image.Load(data))
                        {
                            if (toDraw.Size() != new Size(_template.User.Icon.Size.X, _template.User.Icon.Size.Y))
                            {
                                toDraw.Mutate(x => x.Resize(_template.User.Icon.Size.X, _template.User.Icon.Size.Y));
                            }
                            img.Mutate(x => x.DrawImage(GraphicsOptions.Default,
                                toDraw,
                                new Point(_template.User.Icon.Pos.X, _template.User.Icon.Pos.Y)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }

                //club image
                if (_template.Club.Icon.Show)
                {
                    await DrawClubImage(img, stats);
                }
                img.Mutate(x => x.Resize(_template.OutputSize.X, _template.OutputSize.Y));
                return ((Stream)img.ToStream(imageFormat), imageFormat);
            }
        });

        void DrawXpBar(float percent, XpBar info, Image<Rgba32> img)
        {
            var x1 = info.PointA.X;
            var y1 = info.PointA.Y;

            var x2 = info.PointB.X;
            var y2 = info.PointB.Y;

            var length = info.Length * percent;

            float x3 = 0, x4 = 0, y3 = 0, y4 = 0;

            if (info.Direction == XpTemplateDirection.Down)
            {
                x3 = x1;
                x4 = x2;
                y3 = y1 + length;
                y4 = y2 + length;
            }
            else if (info.Direction == XpTemplateDirection.Up)
            {
                x3 = x1;
                x4 = x2;
                y3 = y1 - length;
                y4 = y2 - length;
            }
            else if (info.Direction == XpTemplateDirection.Left)
            {
                x3 = x1 - length;
                x4 = x2 - length;
                y3 = y1;
                y4 = y2;
            }
            else
            {
                x3 = x1 + length;
                x4 = x2 + length;
                y3 = y1;
                y4 = y2;
            }

            img.Mutate(x => x.FillPolygon(info.Color,
                new[] {
                    new PointF(x1, y1),
                    new PointF(x3, y3),
                    new PointF(x4, y4),
                    new PointF(x2, y2),
                }));
        }

        private async Task DrawClubImage(Image<Rgba32> img, FullUserStats stats)
        {
            if (!string.IsNullOrWhiteSpace(stats.User.Club?.ImageUrl))
            {
                try
                {
                    var imgUrl = new Uri(stats.User.Club.ImageUrl);
                    var (succ, data) = await _cache.TryGetImageDataAsync(imgUrl);
                    if (!succ)
                    {
                        using (var http = _httpFactory.CreateClient())
                        using (var temp = await http.GetAsync(imgUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            if (!temp.IsImage() || temp.GetImageSize() > 11)
                                return;
                            var imgData = await temp.Content.ReadAsByteArrayAsync();
                            using (var tempDraw = Image.Load(imgData))
                            {
                                tempDraw.Mutate(x => x.Resize(_template.Club.Icon.Size.X, _template.Club.Icon.Size.Y));
                                tempDraw.ApplyRoundedCorners(Math.Max(_template.Club.Icon.Size.X, _template.Club.Icon.Size.Y) / 2.0f);
                                using (var tds = tempDraw.ToStream())
                                {
                                    data = tds.ToArray();
                                }
                            }
                        }

                        await _cache.SetImageDataAsync(imgUrl, data);
                    }
                    using (var toDraw = Image.Load(data))
                    {
                        if (toDraw.Size() != new Size(_template.Club.Icon.Size.X, _template.Club.Icon.Size.Y))
                        {
                            toDraw.Mutate(x => x.Resize(_template.Club.Icon.Size.X, _template.Club.Icon.Size.Y));
                        }
                        img.Mutate(x => x.DrawImage(GraphicsOptions.Default,
                            toDraw,
                            new Point(_template.Club.Icon.Pos.X, _template.Club.Icon.Pos.Y)));
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
            return Task.CompletedTask;
        }

        public void XpReset(ulong guildId, ulong userId)
        {
            using (var uow = _db.GetDbContext())
            {
                uow.Xp.ResetGuildUserXp(userId, guildId);
                uow.SaveChanges();
            }
        }

        public void XpReset(ulong guildId)
        {
            using (var uow = _db.GetDbContext())
            {
                uow.Xp.ResetGuildXp(guildId);
                uow.SaveChanges();
            }
        }
    }
}
