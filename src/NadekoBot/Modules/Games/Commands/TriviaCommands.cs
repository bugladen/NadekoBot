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
        public class TriviaCommands : ModuleBase
        {
            public static ConcurrentDictionary<ulong, TriviaGame> RunningTrivias { get; } = new ConcurrentDictionary<ulong, TriviaGame>();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Trivia(params string[] args)
            {
                TriviaGame trivia;
                if (!RunningTrivias.TryGetValue(Context.Guild.Id, out trivia))
                {
                    var showHints = !args.Contains("nohint");
                    var number = args.Select(s =>
                    {
                        int num;
                        return new Tuple<bool, int>(int.TryParse(s, out num), num);
                    }).Where(t => t.Item1).Select(t => t.Item2).FirstOrDefault();
                    if (number < 0)
                        return;
                    var triviaGame = new TriviaGame(Context.Guild, (ITextChannel)Context.Channel, showHints, number == 0 ? 10 : number);
                    if (RunningTrivias.TryAdd(Context.Guild.Id, triviaGame))
                        await Context.Channel.SendConfirmAsync($"**Trivia game started! {triviaGame.WinRequirement} points needed to win.**").ConfigureAwait(false);
                    else
                        await triviaGame.StopGame().ConfigureAwait(false);
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
                    await channel.SendConfirmAsync("Leaderboard", trivia.GetLeaderboard()).ConfigureAwait(false);
                else
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
                }
                else
                    await channel.SendErrorAsync("No trivia is running on this server.").ConfigureAwait(false);
            }
        }
    }
}