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
        public class BlacklistCommands
        {
            public static ConcurrentHashSet<BlacklistItem> BlacklistedItems { get; set; } = new ConcurrentHashSet<BlacklistItem>();

            static BlacklistCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    BlacklistedItems = new ConcurrentHashSet<BlacklistItem>(uow.BotConfig.GetOrCreate().Blacklist);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(IUserMessage imsg, AddRemove action, ulong id)
                => Blacklist(imsg, action, id, BlacklistType.User);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(IUserMessage imsg, AddRemove action, IUser usr)
                => Blacklist(imsg, action, usr.Id, BlacklistType.User);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ChannelBlacklist(IUserMessage imsg, AddRemove action, ulong id)
                => Blacklist(imsg, action, id, BlacklistType.Channel);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(IUserMessage imsg, AddRemove action, ulong id)
                => Blacklist(imsg, action, id, BlacklistType.Server);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(IUserMessage imsg, AddRemove action, IGuild guild)
                => Blacklist(imsg, action, guild.Id, BlacklistType.Server);

            private async Task Blacklist(IUserMessage imsg, AddRemove action, ulong id, BlacklistType type)
            {
                var channel = imsg.Channel;

                using (var uow = DbHandler.UnitOfWork())
                {
                    if (action == AddRemove.Add)
                    {
                        var item = new BlacklistItem { ItemId = id, Type = type };
                        uow.BotConfig.GetOrCreate().Blacklist.Add(item);
                        BlacklistedItems.Add(item);
                    }
                    else
                    {
                        uow.BotConfig.GetOrCreate().Blacklist.RemoveWhere(bi => bi.ItemId == id && bi.Type == type);
                        BlacklistedItems.RemoveWhere(bi => bi.ItemId == id && bi.Type == type);
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

                await channel.SendConfirmAsync($"Blacklisted a `{type}` with id `{id}`").ConfigureAwait(false);
            }
        }
    }
}
