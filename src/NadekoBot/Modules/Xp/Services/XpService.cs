using Discord;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Extensions;
using NadekoBot.Modules.Xp.Common;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Impl;
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
using SixLabors.Shapes;
using System.Numerics;
using ImageSharp.Drawing.Pens;
using ImageSharp.Drawing.Brushes;

namespace NadekoBot.Modules.Xp.Services
{
    public class XpService : INService
    {
        private enum NotifOf { Server, Global } // is it a server level-up or global level-up notification

        private readonly DbService _db;
        private readonly CommandHandler _cmd;
        private readonly IBotConfigProvider _bc;
        private readonly IImagesService _images;
        private readonly Logger _log;
        private readonly NadekoStrings _strings;
        private readonly IDataCache _cache;
        private readonly FontCollection _fonts = new FontCollection();
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

        private readonly Timer updateXpTimer;
        private readonly HttpClient http = new HttpClient();
        private FontFamily _usernameFontFamily;
        private FontFamily _clubFontFamily;
        private Font _levelFont;
        private Font _xpFont;
        private Font _awardedFont;
        private Font _rankFont;
        private Font _timeFont;

        public XpService(CommandHandler cmd, IBotConfigProvider bc,
            IEnumerable<GuildConfig> allGuildConfigs, IImagesService images,
            DbService db, NadekoStrings strings, IDataCache cache)
        {
            _db = db;
            _cmd = cmd;
            _bc = bc;
            _images = images;
            _log = LogManager.GetCurrentClassLogger();
            _strings = strings;
            _cache = cache;

            //load settings
            allGuildConfigs = allGuildConfigs.Where(x => x.XpSettings != null);
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

            //todo 60 move to font provider or somethign
            _fonts = new FontCollection();
            if (Directory.Exists("data/fonts"))
                foreach (var file in Directory.GetFiles("data/fonts"))
                {
                    _fonts.Install(file);
                }

            InitializeFonts();

            _cmd.OnMessageNoTrigger += _cmd_OnMessageNoTrigger;

            updateXpTimer = new Timer(async _ =>
            {
                try
                {
                    var toNotify = new List<(IMessageChannel MessageChannel, IUser User, int Level, XpNotificationType NotifyType, NotifOf NotifOf)>();
                    var roleRewards = new Dictionary<ulong, List<XpRoleReward>>();

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

                            var usr = uow.Xp.GetOrCreateUser(item.Key.GuildId, item.Key.User.Id);
                            var du = uow.DiscordUsers.GetOrCreate(item.Key.User);

                            if (du.LastXpGain + TimeSpan.FromMinutes(_bc.BotConfig.XpMinutesTimeout) > DateTime.UtcNow)
                                continue;

                            du.LastXpGain = DateTime.UtcNow;

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
                                if (!roleRewards.TryGetValue(usr.GuildId, out var rewards))
                                {
                                    rewards = uow.GuildConfigs.XpSettingsFor(usr.GuildId).RoleRewards.ToList();
                                    roleRewards.Add(usr.GuildId, rewards);
                                }

                                var rew = rewards.FirstOrDefault(x => x.Level == newGuildLevelData.Level);
                                if (rew != null)
                                {
                                    var role = first.User.Guild.GetRole(rew.RoleId);
                                    if (role != null)
                                    {
                                        var __ = first.User.AddRoleAsync(role);
                                    }
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


            //just a first line, in order to prevent queries. But since other shards can try to do this too,
            //i'll check in the db too.
            var clearRewardTimer = Task.Run(async () =>
            {
                while (true)
                {
                    _rewardedUsers.Clear();
                    
                    await Task.Delay(TimeSpan.FromMinutes(_bc.BotConfig.XpMinutesTimeout));
                }
            });
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
            return _rewardedUsers.Add(userId);
        }

        public FullUserStats GetUserStats(IGuildUser user)
        {
            DiscordUser du;
            UserXpStats stats;
            int totalXp;
            int globalRank;
            int guildRank;
            using (var uow = _db.UnitOfWork)
            {
                du = uow.DiscordUsers.GetOrCreate(user);
                stats = uow.Xp.GetOrCreateUser(user.GuildId, user.Id);
                totalXp = du.TotalXp;
                globalRank = uow.DiscordUsers.GetUserGlobalRanking(user.Id);
                guildRank = uow.Xp.GetUserGuildRanking(user.Id, user.GuildId);
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

        public Task<MemoryStream> GenerateImageAsync(IGuildUser user)
        {
            return GenerateImageAsync(GetUserStats(user));
        }

        private void InitializeFonts()
        {
            _usernameFontFamily = _fonts.Find("Whitney-Bold");
            _clubFontFamily = _fonts.Find("Whitney-Bold");
            _levelFont = _fonts.Find("Whitney-Bold").CreateFont(45);
            _xpFont = _fonts.Find("Whitney-Bold").CreateFont(50);
            _awardedFont = _fonts.Find("Whitney-Bold").CreateFont(25);
            _rankFont = _fonts.Find("Uni Sans Thin CAPS").CreateFont(30);
            _timeFont = _fonts.Find("Whitney-Bold").CreateFont(20);
        }

        public Task<MemoryStream> GenerateImageAsync(FullUserStats stats) => Task.Run(async () =>
        {
            using (var img = Image.Load(_images.XpCard.ToArray()))
            {

                var username = stats.User.ToString();
                var usernameFont = _usernameFontFamily
                    .CreateFont(username.Length <= 6
                        ? 50
                        : 50 - username.Length);

                img.DrawText("@" + username, usernameFont, Rgba32.White,
                    new PointF(130, 5));

                // level

                img.DrawText(stats.Global.Level.ToString(), _levelFont, Rgba32.White,
                    new PointF(47, 137));

                img.DrawText(stats.Guild.Level.ToString(), _levelFont, Rgba32.White,
                    new PointF(47, 285));

                //club name

                var clubName = stats.User.Club?.ToString() ?? "-";

                var clubFont = _clubFontFamily
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
                img.DrawText($"{global.LevelXp}/{global.RequiredXp}", _xpFont, brush, pen,
                    new PointF(430, 130));

                img.FillPolygon(xpBgBrush, new[] {
                    new PointF(282, 248),
                    new PointF(282 + (450 * (guild.LevelXp / (float)guild.RequiredXp)), 248),
                    new PointF(247 + (450 * (guild.LevelXp / (float)guild.RequiredXp)), 379),
                    new PointF(247, 379),
                });
                img.DrawText($"{guild.LevelXp}/{guild.RequiredXp}", _xpFont, brush, pen,
                    new PointF(400, 270));

                if (stats.FullGuildStats.AwardedXp != 0)
                {
                    var sign = stats.FullGuildStats.AwardedXp > 0
                        ? "+ "
                        : "";
                    img.DrawText($"({sign}{stats.FullGuildStats.AwardedXp})", _awardedFont, brush, pen,
                        new PointF(445 - (Math.Max(0, (stats.FullGuildStats.AwardedXp.ToString().Length - 2)) * 5), 335));
                }

                //ranking

                img.DrawText(stats.GlobalRanking.ToString(), _rankFont, Rgba32.White,
                    new PointF(148, 170));

                img.DrawText(stats.GuildRanking.ToString(), _rankFont, Rgba32.White,
                    new PointF(148, 317));

                //time on this level

                string GetTimeSpent(DateTime time)
                {
                    var offset = DateTime.UtcNow - time;
                    return $"{offset.Days}d{offset.Hours}h{offset.Minutes}m";
                }

                img.DrawText(GetTimeSpent(stats.User.LastLevelUp), _timeFont, Rgba32.White,
                    new PointF(50, 197));

                img.DrawText(GetTimeSpent(stats.FullGuildStats.LastLevelUp), _timeFont, Rgba32.White,
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
                                ApplyRoundedCorners(tempDraw, 35);
                                data = tempDraw.ToStream().ToArray();
                            }

                            await _cache.SetImageDataAsync(avatarUrl, data);
                        }
                        var toDraw = Image.Load(data);


                        img.DrawImage(toDraw,
                            1,
                            new Size(69, 70),
                            new Point(32, 10));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }

                //club image

                if (!string.IsNullOrWhiteSpace(stats.User.Club?.ImageUrl))
                {
                    var imgUrl = stats.User.Club.ImageUrl;
                    try
                    {
                        var (succ, data) = await _cache.TryGetImageDataAsync(imgUrl);
                        if (!succ)
                        {
                            using (var temp = await http.GetStreamAsync(imgUrl))
                            using (var tempDraw = Image.Load(temp).Resize(45, 45))
                            {
                                ApplyRoundedCorners(tempDraw, 22.5f);
                                data = tempDraw.ToStream().ToArray();
                            }

                            await _cache.SetImageDataAsync(imgUrl, data);
                        }
                        var toDraw = Image.Load(data);

                        img.DrawImage(toDraw,
                            1,
                            new Size(45, 45),
                            new Point(722, 25));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }

                return img.Resize(432, 211).ToStream();
            }
        });


        // https://github.com/SixLabors/ImageSharp/tree/master/samples/AvatarWithRoundedCorner
        public static void ApplyRoundedCorners(Image<Rgba32> img, float cornerRadius)
        {
            var corners = BuildCorners(img.Width, img.Height, cornerRadius);
            // now we have our corners time to draw them
            img.Fill(Rgba32.Transparent, corners, new GraphicsOptions(true)
            {
                BlenderMode = ImageSharp.PixelFormats.PixelBlenderMode.Src // enforces that any part of this shape that has color is punched out of the background
            });
        }

        public static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            // first create a square
            var rect = new RectangularePolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            // then cut out of the square a circle so we are left with a corner
            var cornerToptLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            // corner is now a corner shape positions top left
            //lets make 3 more positioned correctly, we can do that by translating the orgional around the center of the image
            var center = new Vector2(imageWidth / 2, imageHeight / 2);

            float rightPos = imageWidth - cornerToptLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerToptLeft.Bounds.Height + 1;

            // move it across the width of the image - the width of the shape
            var cornerTopRight = cornerToptLeft.RotateDegree(90).Translate(rightPos, 0);
            var cornerBottomLeft = cornerToptLeft.RotateDegree(-90).Translate(0, bottomPos);
            var cornerBottomRight = cornerToptLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerToptLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }
    }
}
