using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Collections.Concurrent;
using Discord;
using TriviaGame = NadekoBot.Classes.Trivia.TriviaGame;

namespace NadekoBot.Commands {
    class Trivia : DiscordCommand {
        public static ConcurrentDictionary<Server, TriviaGame> runningTrivias = new ConcurrentDictionary<Server, TriviaGame>();

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            TriviaGame trivia;
            if (!runningTrivias.TryGetValue(e.Server, out trivia)) {
                var triviaGame = new TriviaGame(e);
                if (runningTrivias.TryAdd(e.Server, triviaGame))
                    await e.Channel.SendMessage("**Trivia game started!**\nFirst player to get to 10 points wins! You have 30 seconds per question.\nUse command `tq` if game was started by accident.**");
                else
                    await triviaGame.StopGame();
            } else
                await e.Channel.SendMessage("Trivia game is already running on this server.\n" + trivia.CurrentQuestion);
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
                    TriviaGame trivia;
                    if (runningTrivias.TryGetValue(e.Server, out trivia))
                        await e.Channel.SendMessage(trivia.GetLeaderboard());
                    else
                        await e.Channel.SendMessage("No trivia is running on this server.");
                });

            cgb.CreateCommand("tq")
                .Description("Quits current trivia after current question.")
                .Alias("-tq")
                .Do(async e=> {
                    TriviaGame trivia;
                    if (runningTrivias.TryGetValue(e.Server, out trivia)) {
                        await trivia.StopGame();
                    } else
                        await e.Channel.SendMessage("No trivia is running on this server.");
                });
        }
    }
}
