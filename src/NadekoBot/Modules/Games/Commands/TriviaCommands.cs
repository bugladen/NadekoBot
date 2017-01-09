using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Trivia;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;


namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class TriviaCommands : ModuleBase
        {
            public static ConcurrentDictionary<ulong, TriviaGame> RunningTrivias { get; } = new ConcurrentDictionary<ulong, TriviaGame>();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task Trivia([Remainder] string additionalArgs = "")
                => Trivia(10, additionalArgs);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Trivia(int winReq = 10, [Remainder] string additionalArgs = "")
            {
                var channel = (ITextChannel)Context.Channel;

                var showHints = !additionalArgs.Contains("nohint");

                TriviaGame trivia = new TriviaGame(channel.Guild, channel, showHints, winReq);
                if (RunningTrivias.TryAdd(channel.Guild.Id, trivia))
                {
                    try
                    {
                        await trivia.StartGame().ConfigureAwait(false);
                    }
                    finally
                    {
                        RunningTrivias.TryRemove(channel.Guild.Id, out trivia);
                        await trivia.EnsureStopped().ConfigureAwait(false);
                    }
                    return;                    
                }
                else
                    await Context.Channel.SendErrorAsync("Trivia game is already running on this server.\n" + trivia.CurrentQuestion).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tl()
            {
                var channel = (ITextChannel)Context.Channel;

                TriviaGame trivia;
                if (RunningTrivias.TryGetValue(channel.Guild.Id, out trivia))
                {
                    await channel.SendConfirmAsync("Leaderboard", trivia.GetLeaderboard()).ConfigureAwait(false);
                    return;
                }

                await channel.SendErrorAsync("No trivia is running on this server.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tq()
            {
                var channel = (ITextChannel)Context.Channel;

                TriviaGame trivia;
                if (RunningTrivias.TryGetValue(channel.Guild.Id, out trivia))
                {
                    await trivia.StopGame().ConfigureAwait(false);
                    return;
                }

                await channel.SendErrorAsync("No trivia is running on this server.").ConfigureAwait(false);
            }
        }
    }
}