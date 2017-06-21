using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using NadekoBot.Services.Games;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PollCommands : NadekoSubmodule
        {
            private readonly DiscordSocketClient _client;
            private readonly PollService _polls;

            public PollCommands(DiscordSocketClient client, PollService polls)
            {
                _client = client;
                _polls = polls;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public Task Poll([Remainder] string arg = null)
                => InternalStartPoll(arg, false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public Task PublicPoll([Remainder] string arg = null)
                => InternalStartPoll(arg, true);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task PollStats()
            {
                if (!_polls.ActivePolls.TryGetValue(Context.Guild.Id, out var poll))
                    return;

                await Context.Channel.EmbedAsync(poll.GetStats(GetText("current_poll_results")));
            }
            //todo enable private polls, or completely remove them
            private async Task InternalStartPoll(string arg, bool isPublic = false)
            {
                if (isPublic == false)
                {
                    await ReplyErrorLocalized($"Temporarily disabled. Use `{Prefix}ppoll`");
                    return;
                }
                if(await _polls.StartPoll((ITextChannel)Context.Channel, Context.Message, arg, isPublic) == false)
                    await ReplyErrorLocalized("poll_already_running").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task Pollend()
            {
                var channel = (ITextChannel)Context.Channel;

                _polls.ActivePolls.TryRemove(channel.Guild.Id, out var poll);
                await poll.StopPoll().ConfigureAwait(false);
            }
        }

        
    }
}