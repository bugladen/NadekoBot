using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using NLog;
using NadekoBot.Modules.Administration.Commands.Migration;
using System.Collections.Concurrent;
using NadekoBot.Extensions;
using NadekoBot.Services.Database;
using Microsoft.Data.Sqlite;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class Migration
        {
            private const int CURRENT_VERSION = 1;

            private Logger _log { get; }

            public Migration()
            {
                _log = LogManager.GetCurrentClassLogger();
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task MigrateData(IUserMessage umsg)
            {
                var channel = (ITextChannel) umsg.Channel;

                var version = 0;
                using (var uow = DbHandler.UnitOfWork())
                {
                    version = uow.BotConfig.GetOrCreate().MigrationVersion;
                }
                try
                {
                    for (var i = version; i < CURRENT_VERSION; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                await Migrate0_9To1_0();
                                break;
                        }
                    }
                    await umsg.Channel.SendMessageAsync("Migration done.").ConfigureAwait(false);
                }
                catch (MigrationException)
                {
                    await umsg.Channel.SendMessageAsync(":warning: Error while migrating, check logs for more informations.").ConfigureAwait(false);
                }
            }

            private async Task Migrate0_9To1_0()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var botConfig = uow.BotConfig.GetOrCreate();
                    MigrateConfig0_9(uow, botConfig);
                    MigratePermissions0_9(uow);
                    MigrateServerSpecificConfigs0_9(uow);
                    MigrateDb0_9(uow);

                    //NOW save it
                    botConfig.MigrationVersion = 1;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

            private void MigrateDb0_9(IUnitOfWork uow)
            {
                var db = new SqliteConnection("Data Source=data/nadekobot.sqlite");
                db.Open();

                var com = db.CreateCommand();
                com.CommandText = "SELECT * FROM Announcement";

                var reader = com.ExecuteReader();
                while(reader.Read())
                {
                    var gid = (ulong)reader["ServerId"];
                    var greet = (bool)reader["Greet"];
                    var greetDM = (bool)reader["GreetPM"];
                    var greetChannel = (ulong)reader["GreetChannelId"];
                    var greetMsg = (string)reader["GreetText"];
                    var bye = (bool)reader["Bye"];
                    var byeDM = (bool)reader["ByePM"];
                    var byeChannel = (ulong)reader["ByeChannelId"];
                    var byeMsg = (string)reader["ByeText"];
                    bool grdel =  (bool)reader["DeleteGreetMessages"];
                    var byedel = grdel;
                    var gc = uow.GuildConfigs.For(gid);

                    if (greetDM)
                        gc.SendDmGreetMessage = greet;
                    else
                        gc.SendChannelGreetMessage = greet;
                    gc.GreetMessageChannelId = greetChannel;
                    gc.ChannelGreetMessageText = greetMsg;

                    gc.SendChannelByeMessage = bye;
                    gc.ByeMessageChannelId = byeChannel;
                    gc.ChannelByeMessageText = byeMsg;

                    gc.AutoDeleteByeMessages = gc.AutoDeleteGreetMessages = grdel;
                }

                var com2 = db.CreateCommand();
                com.CommandText = "SELECT * FROM Announcement";

                var reader2 = com.ExecuteReader();
                while (reader2.Read())
                {
                    uow.Currency.Add(new Currency()
                    {
                        Amount = (long)reader2["Value"],
                        UserId = (ulong)reader2["UserId"]
                    });
                }
                db.Close();
            }

            private void MigrateServerSpecificConfigs0_9(IUnitOfWork uow)
            {
                const string specificConfigsPath = "data/ServerSpecificConfigs.json";
                var configs = new ConcurrentDictionary<ulong, ServerSpecificConfig>();
                try
                {
                    configs = JsonConvert
                        .DeserializeObject<ConcurrentDictionary<ulong, ServerSpecificConfig>>(
                            File.ReadAllText(specificConfigsPath), new JsonSerializerSettings()
                            {
                                Error = (s, e) =>
                                {
                                    if (e.ErrorContext.Member.ToString() == "GenerateCurrencyChannels")
                                    {
                                        e.ErrorContext.Handled = true;
                                    }
                                }
                            });
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, "ServerSpecificConfig deserialization failed");
                    return;
                }

                foreach (var config in configs)
                {
                    var guildId = config.Key;
                    var data = config.Value;

                    var guildConfig = uow.GuildConfigs.For(guildId);

                    guildConfig.AutoAssignRoleId = data.AutoAssignedRole;
                    guildConfig.DeleteMessageOnCommand = data.AutoDeleteMessagesOnCommand;
                    guildConfig.DefaultMusicVolume = data.DefaultMusicVolume;
                    guildConfig.ExclusiveSelfAssignedRoles = data.ExclusiveSelfAssignedRoles;
                    guildConfig.GenerateCurrencyChannelIds = new HashSet<GCChannelId>(data.GenerateCurrencyChannels.Select(gc => new GCChannelId() { ChannelId = gc.Key }));
                    uow.SelfAssignedRoles.AddRange(data.ListOfSelfAssignableRoles.Select(r => new SelfAssignedRole() { GuildId = guildId, RoleId = r }).ToArray());
                    var logSetting = guildConfig.LogSetting;
                    guildConfig.LogSetting.IsLogging = data.LogChannel != null;
                    guildConfig.LogSetting.ChannelId = data.LogChannel ?? 0;
                    guildConfig.LogSetting.IgnoredChannels = new HashSet<IgnoredLogChannel>(data.LogserverIgnoreChannels.Select(id => new IgnoredLogChannel() { ChannelId = id }));

                    guildConfig.LogSetting.LogUserPresence = data.LogPresenceChannel != null;
                    guildConfig.LogSetting.UserPresenceChannelId = data.LogPresenceChannel ?? 0;
                    

                    guildConfig.FollowedStreams = new HashSet<FollowedStream>(data.ObservingStreams.Select(x =>
                    {
                        FollowedStream.FollowedStreamType type = FollowedStream.FollowedStreamType.Twitch;
                        switch (x.Type)
                        {
                            case StreamNotificationConfig0_9.StreamType.Twitch:
                                type = FollowedStream.FollowedStreamType.Twitch;
                                break;
                            case StreamNotificationConfig0_9.StreamType.Beam:
                                type = FollowedStream.FollowedStreamType.Beam;
                                break;
                            case StreamNotificationConfig0_9.StreamType.Hitbox:
                                type = FollowedStream.FollowedStreamType.Hitbox;
                                break;
                            default:
                                break;
                        }

                        return new FollowedStream()
                        {
                            ChannelId = x.ChannelId,
                            GuildId = guildId,
                            Username = x.Username.ToLowerInvariant(),
                            Type = type
                        };
                    }));
                    guildConfig.VoicePlusTextEnabled = data.VoicePlusTextEnabled;
                }
                try { File.Move("data/ServerSpecificConfigs.json", "data/DELETE_ME_ServerSpecificCOnfigs.json"); } catch { }
            }

            private void MigratePermissions0_9(IUnitOfWork uow)
            {
                var PermissionsDict = new ConcurrentDictionary<ulong, ServerPermissions0_9>();
                if (!Directory.Exists("data/permissions/"))
                    throw new MigrationException();
                foreach (var file in Directory.EnumerateFiles("data/permissions/"))
                {
                    try
                    {
                        var strippedFileName = Path.GetFileNameWithoutExtension(file);
                        if (string.IsNullOrWhiteSpace(strippedFileName)) continue;
                        var id = ulong.Parse(strippedFileName);
                        var data = JsonConvert.DeserializeObject<ServerPermissions0_9>(File.ReadAllText(file));
                        PermissionsDict.TryAdd(id, data);
                    }
                    catch { }
                }
                foreach (var perms in PermissionsDict)
                {
                    var guildId = perms.Key;
                    var data = perms.Value;

                    _log.Info("Migrating data from permissions folder for {0}", guildId);

                    var gconfig = uow.GuildConfigs.For(guildId);

                    gconfig.PermissionRole = data.PermissionsControllerRole;
                    gconfig.VerbosePermissions = data.Verbose;
                    gconfig.FilteredWords = new HashSet<FilteredWord>(data.Words.Select(w => w.ToLowerInvariant())
                                                                                .Distinct()
                                                                                .Select(w => new FilteredWord() { Word = w }));
                    gconfig.FilterWords = data.Permissions.FilterWords;
                    gconfig.FilterInvites = data.Permissions.FilterInvites;

                    gconfig.FilterInvitesChannelIds = new HashSet<FilterChannelId>();
                    gconfig.FilterInvitesChannelIds.AddRange(data.ChannelPermissions.Where(kvp => kvp.Value.FilterInvites)
                                                                                    .Select(cp => new FilterChannelId()
                                                                                    {
                                                                                        ChannelId = cp.Key
                                                                                    }));

                    gconfig.FilterWordsChannelIds = new HashSet<FilterChannelId>();
                    gconfig.FilterWordsChannelIds.AddRange(data.ChannelPermissions.Where(kvp => kvp.Value.FilterWords)
                                                                                    .Select(cp => new FilterChannelId()
                                                                                    {
                                                                                        ChannelId = cp.Key
                                                                                    }));

                    gconfig.CommandCooldowns = new HashSet<CommandCooldown>(data.CommandCooldowns
                                                                                .Where(cc => !string.IsNullOrWhiteSpace(cc.Key) && cc.Value > 0)
                                                                                .Select(cc => new CommandCooldown()
                                                                                {
                                                                                    CommandName = cc.Key,
                                                                                    Seconds = cc.Value
                                                                                }));
                    var smodules = data.Permissions.Modules.Where(m => !m.Value);

                    try { Directory.Move("data/permissions","data/DELETE_ME_permissions"); } catch { }
                }

            }

            private void MigrateConfig0_9(IUnitOfWork uow, BotConfig botConfig)
            {
                Config0_9 oldConfig;
                const string configPath = "data/config.json";
                try
                {
                    oldConfig = JsonConvert.DeserializeObject<Config0_9>(File.ReadAllText(configPath));
                }
                catch (FileNotFoundException)
                {
                    _log.Warn("config.json not found");
                    return;
                }
                catch (Exception)
                {
                    _log.Error("Unknown error while deserializing file config.json, pls check its integrity, aborting migration");
                    throw new MigrationException();
                }

                //Basic
                botConfig.ForwardMessages = oldConfig.ForwardMessages;
                botConfig.ForwardToAllOwners = oldConfig.ForwardToAllOwners;
                botConfig.BufferSize = (ulong)oldConfig.BufferSize;
                botConfig.RemindMessageFormat = oldConfig.RemindMessageFormat;
                botConfig.CurrencySign = oldConfig.CurrencySign;
                botConfig.CurrencyName = oldConfig.CurrencyName;
                botConfig.DMHelpString = oldConfig.DMHelpString;
                botConfig.HelpString = oldConfig.HelpString;

                //messages
                botConfig.RotatingStatuses = oldConfig.IsRotatingStatus;
                var messages = new List<PlayingStatus>();

                oldConfig.RotatingStatuses.ForEach(i => messages.Add(new PlayingStatus { Status = i }));
                botConfig.RotatingStatusMessages = messages;

                //races
                var races = new HashSet<RaceAnimal>();
                oldConfig.RaceAnimals.ForEach(i => races.Add(new RaceAnimal() { Icon = i, Name = i }));
                if (races.Any())
                    botConfig.RaceAnimals = races;

                //Prefix
                botConfig.ModulePrefixes.Clear();
                botConfig.ModulePrefixes.AddRange(new HashSet<ModulePrefix>
                {
                    new ModulePrefix()
                    {
                        ModuleName = "Administration",
                        Prefix = oldConfig.CommandPrefixes.Administration
                    },
                    new ModulePrefix()
                    {
                        ModuleName = "Searches",
                        Prefix = oldConfig.CommandPrefixes.Searches
                    },
                    new ModulePrefix() {ModuleName = "NSFW", Prefix = oldConfig.CommandPrefixes.NSFW},
                    new ModulePrefix()
                    {
                        ModuleName = "Conversations",
                        Prefix = oldConfig.CommandPrefixes.Conversations
                    },
                    new ModulePrefix()
                    {
                        ModuleName = "ClashOfClans",
                        Prefix = oldConfig.CommandPrefixes.ClashOfClans
                    },
                    new ModulePrefix() {ModuleName = "Help", Prefix = oldConfig.CommandPrefixes.Help},
                    new ModulePrefix() {ModuleName = "Music", Prefix = oldConfig.CommandPrefixes.Music},
                    new ModulePrefix() {ModuleName = "Trello", Prefix = oldConfig.CommandPrefixes.Trello},
                    new ModulePrefix() {ModuleName = "Games", Prefix = oldConfig.CommandPrefixes.Games},
                    new ModulePrefix()
                    {
                        ModuleName = "Gambling",
                        Prefix = oldConfig.CommandPrefixes.Gambling
                    },
                    new ModulePrefix()
                    {
                        ModuleName = "Permissions",
                        Prefix = oldConfig.CommandPrefixes.Permissions
                    },
                    new ModulePrefix()
                    {
                        ModuleName = "Programming",
                        Prefix = oldConfig.CommandPrefixes.Programming
                    },
                    new ModulePrefix() {ModuleName = "Pokemon", Prefix = oldConfig.CommandPrefixes.Pokemon},
                    new ModulePrefix() {ModuleName = "Utility", Prefix = oldConfig.CommandPrefixes.Utility}
                });

                //Blacklist
                var blacklist = new HashSet<BlacklistItem>(oldConfig.ServerBlacklist.Select(server => new BlacklistItem() { ItemId = server, Type = BlacklistItem.BlacklistType.Server }));
                blacklist.AddRange(oldConfig.ChannelBlacklist.Select(channel => new BlacklistItem() { ItemId = channel, Type = BlacklistItem.BlacklistType.Channel }));
                blacklist.AddRange(oldConfig.UserBlacklist.Select(user => new BlacklistItem() { ItemId = user, Type = BlacklistItem.BlacklistType.User }));
                botConfig.Blacklist = blacklist;

                //Eightball
                botConfig.EightBallResponses = new HashSet<EightBallResponse>(oldConfig._8BallResponses.Select(response => new EightBallResponse() { Text = response }));

                //customreactions
                uow.CustomReactions.AddRange(oldConfig.CustomReactions.SelectMany(cr =>
                {
                    return cr.Value.Select(res => new CustomReaction()
                    {
                        GuildId = 0,
                        IsRegex = false,
                        OwnerOnly = false,
                        Response = res,
                        Trigger = cr.Key,
                    });
                }).ToArray());

                try { File.Move(configPath, "./data/DELETE_ME_config.json"); } catch { }
            }
        }
    }
}
