using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Utility;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class CrossServerTextChannel : NadekoSubmodule
        {
            private readonly CrossServerTextService _service;

            public CrossServerTextChannel(CrossServerTextService service)
            {
                _service = service;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Scsc()
            {
                var token = new NadekoRandom().Next();
                var set = new ConcurrentHashSet<ITextChannel>();
                if (_service.Subscribers.TryAdd(token, set))
                {
                    set.Add((ITextChannel) Context.Channel);
                    await ((IGuildUser) Context.User).SendConfirmAsync(GetText("csc_token"), token.ToString())
                        .ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Jcsc(int token)
            {
                ConcurrentHashSet<ITextChannel> set;
                if (!_service.Subscribers.TryGetValue(token, out set))
                    return;
                set.Add((ITextChannel) Context.Channel);
                await ReplyConfirmLocalized("csc_join").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Lcsc()
            {
                foreach (var subscriber in _service.Subscribers)
                {
                    subscriber.Value.TryRemove((ITextChannel) Context.Channel);
                }
                await ReplyConfirmLocalized("csc_leave").ConfigureAwait(false);
            }
        }
    }
}