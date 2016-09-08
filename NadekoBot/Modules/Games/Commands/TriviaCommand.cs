using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Games.Commands.Trivia;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NadekoBot.Modules.Games.Commands
{
    internal class TriviaCommands : DiscordCommand
    {
        public static ConcurrentDictionary<ulong, TriviaGame> RunningTrivias = new ConcurrentDictionary<ulong, TriviaGame>();

        public TriviaCommands(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "t")
                .Description($"Starts a game of trivia. You can add nohint to prevent hints." +
                              "First player to get to 10 points wins by default. You can specify a different number. 30 seconds per question." +
                              $" |`{Module.Prefix}t nohint` or `{Module.Prefix}t 5 nohint`")
                .Parameter("args", ParameterType.Multiple)
                .Do(async e =>
                {
                    TriviaGame trivia;
                    if (!RunningTrivias.TryGetValue(e.Server.Id, out trivia))
                    {
                        var showHints = !e.Args.Contains("nohint");
                        var number = e.Args.Select(s =>
                        {
                            int num;
                            return new Tuple<bool, int>(int.TryParse(s, out num), num);
                        }).Where(t => t.Item1).Select(t => t.Item2).FirstOrDefault();
                        if (number < 0)
                            return;
                        var triviaGame = new TriviaGame(e, showHints, number == 0 ? 10 : number);
                        if (RunningTrivias.TryAdd(e.Server.Id, triviaGame))
                            await e.Channel.SendMessage($"**Trivia game started! {triviaGame.WinRequirement} points needed to win.**").ConfigureAwait(false);
                        else
                            await triviaGame.StopGame().ConfigureAwait(false);
                    }
                    else
                        await e.Channel.SendMessage("Trivia game is already running on this server.\n" + trivia.CurrentQuestion).ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "tl")
                .Description($"Shows a current trivia leaderboard. | `{Prefix}tl`")
                .Do(async e =>
                {
                    TriviaGame trivia;
                    if (RunningTrivias.TryGetValue(e.Server.Id, out trivia))
                        await e.Channel.SendMessage(trivia.GetLeaderboard()).ConfigureAwait(false);
                    else
                        await e.Channel.SendMessage("No trivia is running on this server.").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "tq")
                .Description($"Quits current trivia after current question. | `{Prefix}tq`")
                .Do(async e =>
                {
                    TriviaGame trivia;
                    if (RunningTrivias.TryGetValue(e.Server.Id, out trivia))
                    {
                        await trivia.StopGame().ConfigureAwait(false);
                    }
                    else
                        await e.Channel.SendMessage("No trivia is running on this server.").ConfigureAwait(false);
                });
        }
    }
}
