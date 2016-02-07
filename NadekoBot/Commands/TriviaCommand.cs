using System;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Collections.Concurrent;
using Discord;
using TriviaGame = NadekoBot.Classes.Trivia.TriviaGame;

namespace NadekoBot.Commands {
    class Trivia : DiscordCommand {
        public static ConcurrentDictionary<Server, TriviaGame> runningTrivias = new ConcurrentDictionary<Server, TriviaGame>();

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            if (!runningTrivias.ContainsKey(e.Server)) {
                runningTrivias.TryAdd(e.Server, new TriviaGame(e));
                await e.Send("**Trivia game started!**\nFirst player to get to 10 points wins! You have 30 seconds per question.\nUse command `tq` if game was started by accident.**");
            } else
                await e.Send("Trivia game is already running on this server.\n" + runningTrivias[e.Server].CurrentQuestion);
        };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("t")
                .Description("Starts a game of trivia.")
                .Alias("-t")
                .Do(DoFunc());

            cgb.CreateCommand("tl")
                .Description("Shows a current trivia leaderboard.")
                .Alias("-tl")
                .Alias("tlb")
                .Alias("-tlb")
                .Do(async e=> {
                    if (runningTrivias.ContainsKey(e.Server))
                        await e.Send(runningTrivias[e.Server].GetLeaderboard());
                    else
                        await e.Send("No trivia is running on this server.");
                });

            cgb.CreateCommand("tq")
                .Description("Quits current trivia after current question.")
                .Alias("-tq")
                .Do(async e=> {
                    if (runningTrivias.ContainsKey(e.Server)) {
                        runningTrivias[e.Server].StopGame();
                    } else
                        await e.Send("No trivia is running on this server.");
                });
        }
    }
}
