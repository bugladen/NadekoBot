using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Collections.Concurrent;
using Discord;
using NadekoBot.Modules;
using TriviaGame = NadekoBot.Classes.Trivia.TriviaGame;

namespace NadekoBot.Commands {
    internal class Trivia : DiscordCommand {
        public static ConcurrentDictionary<ulong, TriviaGame> RunningTrivias = new ConcurrentDictionary<ulong, TriviaGame>();

        public Func<CommandEventArgs, Task> DoFunc() => async e => {
            TriviaGame trivia;
            if (!RunningTrivias.TryGetValue(e.Server.Id, out trivia)) {
                var triviaGame = new TriviaGame(e);
                if (RunningTrivias.TryAdd(e.Server.Id, triviaGame))
                    await e.Channel.SendMessage("**Trivia game started!**\nFirst player to get to 10 points wins! You have 30 seconds per question.\nUse command `tq` if game was started by accident.**");
                else
                    await triviaGame.StopGame();
            } else
                await e.Channel.SendMessage("Trivia game is already running on this server.\n" + trivia.CurrentQuestion);
        };

        internal override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(Module.Prefix + "t")
                .Description("Starts a game of trivia.")
                .Do(DoFunc());

            cgb.CreateCommand(Module.Prefix + "tl")
                .Description("Shows a current trivia leaderboard.")
                .Do(async e=> {
                    TriviaGame trivia;
                    if (RunningTrivias.TryGetValue(e.Server.Id, out trivia))
                        await e.Channel.SendMessage(trivia.GetLeaderboard());
                    else
                        await e.Channel.SendMessage("No trivia is running on this server.");
                });

            cgb.CreateCommand(Module.Prefix + "tq")
                .Description("Quits current trivia after current question.")
                .Do(async e=> {
                    TriviaGame trivia;
                    if (RunningTrivias.TryGetValue(e.Server.Id, out trivia)) {
                        await trivia.StopGame();
                    } else
                        await e.Channel.SendMessage("No trivia is running on this server.");
                });
        }

        public Trivia(DiscordModule module) : base(module) {}
    }
}
