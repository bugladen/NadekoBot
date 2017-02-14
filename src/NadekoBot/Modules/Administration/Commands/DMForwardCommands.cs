using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class DmForwardCommands : NadekoSubmodule
        {
            private static volatile bool _forwardDMs;
            private static volatile bool _forwardDMsToAllOwners;

            private static readonly object _locker = new object();
            
            static DmForwardCommands()
            {

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    _forwardDMs = config.ForwardMessages;
                    _forwardDMsToAllOwners = config.ForwardToAllOwners;
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ForwardMessages()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    lock(_locker)
                        _forwardDMs = config.ForwardMessages = !config.ForwardMessages;
                    uow.Complete();
                }
                if (_forwardDMs)
                    await ReplyConfirmLocalized("fwdm_start").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("fwdm_stop").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ForwardToAll()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    lock(_locker)
                        _forwardDMsToAllOwners = config.ForwardToAllOwners = !config.ForwardToAllOwners;
                    uow.Complete();
                }
                if (_forwardDMsToAllOwners)
                    await ReplyConfirmLocalized("fwall_start").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("fwall_stop").ConfigureAwait(false);

            }

            public static async Task HandleDmForwarding(SocketMessage msg, List<IDMChannel> ownerChannels)
            {
                if (_forwardDMs && ownerChannels.Any())
                {
                    var title =
                        GetTextStatic("dm_from", NadekoBot.Localization.DefaultCultureInfo,
                            typeof(Administration).Name.ToLowerInvariant()) + $" [{msg.Author}]({msg.Author.Id})";
                    if (_forwardDMsToAllOwners)
                    {
                        await Task.WhenAll(ownerChannels.Where(ch => ch.Recipient.Id != msg.Author.Id)
                            .Select(ch => ch.SendConfirmAsync(title, msg.Content))).ConfigureAwait(false);
                    }
                    else
                    {
                        var firstOwnerChannel = ownerChannels.First();
                        if (firstOwnerChannel.Recipient.Id != msg.Author.Id)
                            try { await firstOwnerChannel.SendConfirmAsync(title, msg.Content).ConfigureAwait(false); }
                            catch
                            {
                                // ignored
                            }
                    }
                }
            }
        }
    }
}
