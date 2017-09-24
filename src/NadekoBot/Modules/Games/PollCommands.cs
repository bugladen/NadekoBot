using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PollCommands : NadekoSubmodule<PollService>
        {
            private readonly DiscordSocketClient _client;

            public PollCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public Task Poll([Remainder] string arg = null)
                => InternalStartPoll(arg);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task PollStats()
            {
                if (!_service.ActivePolls.TryGetValue(Context.Guild.Id, out var poll))
                    return;

                await Context.Channel.EmbedAsync(poll.GetStats(GetText("current_poll_results")));
            }

            private async Task InternalStartPoll(string arg)
            {
                if(await _service.StartPoll((ITextChannel)Context.Channel, Context.Message, arg) == false)
                    await ReplyErrorLocalized("poll_already_running").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task Pollend()
            {
                var channel = (ITextChannel)Context.Channel;

                _service.ActivePolls.TryRemove(channel.Guild.Id, out var poll);
                await poll.StopPoll().ConfigureAwait(false);
            }
        }

        
    }
}