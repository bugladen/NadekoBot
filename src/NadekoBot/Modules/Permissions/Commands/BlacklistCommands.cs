using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Trivia;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using static NadekoBot.Services.Database.Models.BlacklistItem;

namespace NadekoBot.Modules.Permissions
{
    public partial class Permissions
    {
        public enum AddRemove
        {
            Add,
            Rem
        }

        [Group]
        public class BlacklistCommands : ModuleBase
        {
            public static ConcurrentHashSet<ulong> BlacklistedUsers { get; set; } = new ConcurrentHashSet<ulong>();
            public static ConcurrentHashSet<ulong> BlacklistedGuilds { get; set; } = new ConcurrentHashSet<ulong>();
            public static ConcurrentHashSet<ulong> BlacklistedChannels { get; set; } = new ConcurrentHashSet<ulong>();

            static BlacklistCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var blacklist = uow.BotConfig.GetOrCreate().Blacklist;
                    BlacklistedUsers = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.User).Select(c => c.ItemId));
                    BlacklistedGuilds = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.Server).Select(c => c.ItemId));
                    BlacklistedChannels = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.Channel).Select(c => c.ItemId));
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.User);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(AddRemove action, IUser usr)
                => Blacklist(action, usr.Id, BlacklistType.User);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ChannelBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.Channel);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.Server);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(AddRemove action, IGuild guild)
                => Blacklist(action, guild.Id, BlacklistType.Server);

            private async Task Blacklist(AddRemove action, ulong id, BlacklistType type)
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    if (action == AddRemove.Add)
                    {
                        var item = new BlacklistItem { ItemId = id, Type = type };
                        uow.BotConfig.GetOrCreate().Blacklist.Add(item);
                        if (type == BlacklistType.Server)
                        {
                            BlacklistedGuilds.Add(id);
                        }
                        else if (type == BlacklistType.Channel)
                        {
                            BlacklistedChannels.Add(id);
                        }
                        else if (type == BlacklistType.User)
                        {
                            BlacklistedUsers.Add(id);
                        }                        
                    }
                    else
                    {
                        uow.BotConfig.GetOrCreate().Blacklist.RemoveWhere(bi => bi.ItemId == id && bi.Type == type);
                        if (type == BlacklistType.Server)
                        {
                            BlacklistedGuilds.TryRemove(id);
                        }
                        else if (type == BlacklistType.Channel)
                        {
                            BlacklistedChannels.TryRemove(id);
                        }
                        else if (type == BlacklistType.User)
                        {
                            BlacklistedUsers.TryRemove(id);
                        }
                    }
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (action == AddRemove.Add)
                {
                    TriviaGame tg;
                    switch (type)
                    {
                        case BlacklistType.Server:
                            Games.Games.TriviaCommands.RunningTrivias.TryRemove(id, out tg);
                            if (tg != null)
                            {
                                await tg.StopGame().ConfigureAwait(false);
                            }
                            break;
                        case BlacklistType.Channel:
                            var item = Games.Games.TriviaCommands.RunningTrivias.FirstOrDefault(kvp => kvp.Value.channel.Id == id);
                            Games.Games.TriviaCommands.RunningTrivias.TryRemove(item.Key, out tg);
                            if (tg != null)
                            {
                                await tg.StopGame().ConfigureAwait(false);
                            }
                            break;
                        case BlacklistType.User:
                            break;
                        default:
                            break;
                    }

                }

                if(action == AddRemove.Add)
                    await Context.Channel.SendConfirmAsync($"Blacklisted a `{type}` with id `{id}`").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync($"Unblacklisted a `{type}` with id `{id}`").ConfigureAwait(false);
            }
        }
    }
}
