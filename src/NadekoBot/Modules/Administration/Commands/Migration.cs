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

            [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
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
                                Migrate0_9To1_0();
                                break;
                        }
                    }
                    await umsg.Channel.SendMessageAsync("Migration done.").ConfigureAwait(false);
                }
                catch (MigrationException)
                {
                    await umsg.Channel.SendMessageAsync("Error while migrating, check logs for more informations").ConfigureAwait(false);
                }
            }

            private void Migrate0_9To1_0()
            {
                Config0_9 oldData;
                try
                {
                    oldData = JsonConvert.DeserializeObject<Config0_9>(File.ReadAllText("./data/config.json"));
                }
                catch (FileNotFoundException)
                {
                    _log.Warn("config.json not found, assuming not needed");
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var botConfig = uow.BotConfig.GetOrCreate();
                        botConfig.MigrationVersion = 1;
                        uow.CompleteAsync().ConfigureAwait(false);
                    }
                    return;
                }
                catch (Exception)
                {
                    _log.Error("Unknow error while deserializing file config.json, pls check its integrity, aborting migration");
                    throw new MigrationException();
                }
                using (var uow = DbHandler.UnitOfWork())
                {
                    var botConfig = uow.BotConfig.GetOrCreate();

                    //Basic
                    botConfig.DontJoinServers = oldData.DontJoinServers;
                    botConfig.ForwardMessages = oldData.ForwardMessages;
                    botConfig.ForwardToAllOwners = oldData.ForwardToAllOwners;
                    botConfig.BufferSize = (ulong) oldData.BufferSize;
                    botConfig.RemindMessageFormat = oldData.RemindMessageFormat;
                    botConfig.CurrencySign = oldData.CurrencySign;
                    botConfig.CurrencyName = oldData.CurrencyName;
                    botConfig.DMHelpString = oldData.DMHelpString;
                    botConfig.HelpString = oldData.HelpString;

                    //messages
                    botConfig.RotatingStatuses = oldData.IsRotatingStatus;
                    var messages = new List<PlayingStatus>();

                    oldData.RotatingStatuses.ForEach(i => messages.Add(new PlayingStatus { Status = i }));
                    botConfig.RotatingStatusMessages = messages;

                    //races
                    var races = new List<RaceAnimal>();
                    oldData.RaceAnimals.ForEach(i => races.Add(new RaceAnimal() { Icon =  i, Name = i }));
                    botConfig.RaceAnimals = races;

                    //Prefix
                    var prefix = new List<ModulePrefix>
                    {
                        new ModulePrefix()
                        {
                            ModuleName = "Administration",
                            Prefix = oldData.CommandPrefixes.Administration
                        },
                        new ModulePrefix()
                        {
                            ModuleName = "Searches",
                            Prefix = oldData.CommandPrefixes.Searches
                        },
                        new ModulePrefix() {ModuleName = "NSFW", Prefix = oldData.CommandPrefixes.NSFW},
                        new ModulePrefix()
                        {
                            ModuleName = "Conversations",
                            Prefix = oldData.CommandPrefixes.Conversations
                        },
                        new ModulePrefix()
                        {
                            ModuleName = "ClashOfClans",
                            Prefix = oldData.CommandPrefixes.ClashOfClans
                        },
                        new ModulePrefix() {ModuleName = "Help", Prefix = oldData.CommandPrefixes.Help},
                        new ModulePrefix() {ModuleName = "Music", Prefix = oldData.CommandPrefixes.Music},
                        new ModulePrefix() {ModuleName = "Trello", Prefix = oldData.CommandPrefixes.Trello},
                        new ModulePrefix() {ModuleName = "Games", Prefix = oldData.CommandPrefixes.Games},
                        new ModulePrefix()
                        {
                            ModuleName = "Gambling",
                            Prefix = oldData.CommandPrefixes.Gambling
                        },
                        new ModulePrefix()
                        {
                            ModuleName = "Permissions",
                            Prefix = oldData.CommandPrefixes.Permissions
                        },
                        new ModulePrefix()
                        {
                            ModuleName = "Programming",
                            Prefix = oldData.CommandPrefixes.Programming
                        },
                        new ModulePrefix() {ModuleName = "Pokemon", Prefix = oldData.CommandPrefixes.Pokemon},
                        new ModulePrefix() {ModuleName = "Utility", Prefix = oldData.CommandPrefixes.Utility}
                    };
                    botConfig.ModulePrefixes = prefix;

                    //Blacklist
                    var blacklist = oldData.ServerBlacklist.Select(server => new BlacklistItem() {ItemId = server, Type = BlacklistItem.BlacklistType.Server}).ToList();
                    blacklist.AddRange(oldData.ChannelBlacklist.Select(channel => new BlacklistItem() {ItemId = channel, Type = BlacklistItem.BlacklistType.Channel}));
                    blacklist.AddRange(oldData.UserBlacklist.Select(user => new BlacklistItem() {ItemId = user, Type = BlacklistItem.BlacklistType.User}));
                    botConfig.Blacklist = new HashSet<BlacklistItem>(blacklist);

                    //Eightball
                    botConfig.EightBallResponses = oldData._8BallResponses.Select(response => new EightBallResponse() {Text = response}).ToList();

                    //NOW save it
                    botConfig.MigrationVersion = 1;
                    uow.CompleteAsync();
                }
            }

            private class MigrationException : Exception
            {
            }

            protected class CommandPrefixes0_9
            {
                public string Administration { get; set; }
                public string Searches { get; set; }
                public string NSFW { get; set; }
                public string Conversations { get; set; }
                public string ClashOfClans { get; set; }
                public string Help { get; set; }
                public string Music { get; set; }
                public string Trello { get; set; }
                public string Games { get; set; }
                public string Gambling { get; set; }
                public string Permissions { get; set; }
                public string Programming { get; set; }
                public string Pokemon { get; set; }
                public string Utility { get; set; }
            }

            protected class Config0_9
            {
                public bool DontJoinServers { get; set; }
                public bool ForwardMessages { get; set; }
                public bool ForwardToAllOwners { get; set; }
                public bool IsRotatingStatus { get; set; }
                public int BufferSize { get; set; }
                public List<string> RaceAnimals { get; set; }
                public string RemindMessageFormat { get; set; }
                public Dictionary<string, List<string>> CustomReactions { get; set; }
                public List<string> RotatingStatuses { get; set; }
                public CommandPrefixes0_9 CommandPrefixes { get; set; }
                public List<ulong> ServerBlacklist { get; set; }
                public List<ulong> ChannelBlacklist { get; set; }
                public List<ulong> UserBlacklist { get; set; }
                public List<string> _8BallResponses { get; set; }
                public string CurrencySign { get; set; }
                public string CurrencyName { get; set; }
                public string DMHelpString { get; set; }
                public string HelpString { get; set; }
            }

        }
    }
}
