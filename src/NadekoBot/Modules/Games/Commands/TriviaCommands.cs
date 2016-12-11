using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Trivia;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

//todo Rewrite? Fix trivia not stopping bug
namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class TriviaCommands
        {
            public static ConcurrentDictionary<ulong, TriviaGame> RunningTrivias = new ConcurrentDictionary<ulong, TriviaGame>();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Trivia(IUserMessage umsg, params string[] args)
            {
                var channel = (ITextChannel)umsg.Channel;

                TriviaGame trivia;
                if (!RunningTrivias.TryGetValue(channel.Guild.Id, out trivia))
                {
                    var showHints = !args.Contains("nohint");
                    var number = args.Select(s =>
                    {
                        int num;
                        return new Tuple<bool, int>(int.TryParse(s, out num), num);
                    }).Where(t => t.Item1).Select(t => t.Item2).FirstOrDefault();
                    if (number < 0)
                        return;
                    var triviaGame = new TriviaGame(channel.Guild, (ITextChannel)umsg.Channel, showHints, number == 0 ? 10 : number);
                    if (RunningTrivias.TryAdd(channel.Guild.Id, triviaGame))
                        await channel.SendConfirmAsync($"**Trivia game started! {triviaGame.WinRequirement} points needed to win.**").ConfigureAwait(false);
                    else
                        await triviaGame.StopGame().ConfigureAwait(false);
                }
                else
                    await channel.SendErrorAsync("Trivia game is already running on this server.\n" + trivia.CurrentQuestion).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tl(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                TriviaGame trivia;
                if (RunningTrivias.TryGetValue(channel.Guild.Id, out trivia))
                    await channel.SendConfirmAsync("Leaderboard", trivia.GetLeaderboard()).ConfigureAwait(false);
                else
                    await channel.SendErrorAsync("No trivia is running on this server.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tq(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                TriviaGame trivia;
                if (RunningTrivias.TryGetValue(channel.Guild.Id, out trivia))
                {
                    await trivia.StopGame().ConfigureAwait(false);
                }
                else
                    await channel.SendErrorAsync("No trivia is running on this server.").ConfigureAwait(false);
            }
        }
    }
}