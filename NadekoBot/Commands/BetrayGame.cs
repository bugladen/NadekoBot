using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Modules;

namespace NadekoBot.Commands {
    class BetrayGame : DiscordCommand {
        public BetrayGame(DiscordModule module) : base(module) { }

        private enum Answers {
            Cooperate,
            Betray
        }
        internal override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(Module.Prefix + "betray")
                .Description("BETRAY GAME. Betray nadeko next turn." +
                             "If Nadeko cooperates - you get extra points, nadeko loses a LOT." +
                             "If Nadeko betrays - you both lose some points.")
                .Do(async e => {
                    await ReceiveAnswer(e, Answers.Betray);
                });

            cgb.CreateCommand(Module.Prefix + "cooperate")
                .Description("BETRAY GAME. Cooperate with nadeko next turn." +
                             "If Nadeko cooperates - you both get bonus points." +
                             "If Nadeko betrays - you lose A LOT, nadeko gets extra.")
                .Do(async e => {

                    await ReceiveAnswer(e, Answers.Cooperate);
                });
        }

        private int userPoints = 0;

        private int UserPoints {
            get { return userPoints; }
            set {
                if (value < 0)
                    userPoints = 0;
                userPoints = value;
            }
        }
        private int nadekoPoints = 0;
        private int NadekoPoints {
            get { return nadekoPoints; }
            set {
                if (value < 0)
                    nadekoPoints = 0;
                nadekoPoints = value;
            }
        }

        private int round = 0;
        private Answers NextAnswer = Answers.Cooperate;
        private async Task ReceiveAnswer(CommandEventArgs e, Answers userAnswer) {
            var response = userAnswer == Answers.Betray
                ? ":no_entry: `You betrayed nadeko` - you monster."
                : ":ok: `You cooperated with nadeko.` ";
            var currentAnswer = NextAnswer;
            var nadekoResponse = currentAnswer == Answers.Betray
                ? ":no_entry: `aww Nadeko betrayed you` - she is so cute"
                : ":ok: `Nadeko cooperated.`";
            NextAnswer = userAnswer;
            if (userAnswer == Answers.Betray && currentAnswer == Answers.Betray) {
                NadekoPoints--;
                UserPoints--;
            } else if (userAnswer == Answers.Cooperate && currentAnswer == Answers.Cooperate) {
                NadekoPoints += 2;
                UserPoints += 2;
            } else if (userAnswer == Answers.Betray && currentAnswer == Answers.Cooperate) {
                NadekoPoints -= 3;
                UserPoints += 3;
            } else if (userAnswer == Answers.Cooperate && currentAnswer == Answers.Betray) {
                NadekoPoints += 3;
                UserPoints -= 3;
            }

            await e.Channel.SendMessage($"**ROUND {++round}**" +
                                        $"{response}\n" +
                                        $"{nadekoResponse}\n" +
                                        $"--------------------------------" +
                                        $"Nadeko has {NadekoPoints} points." +
                                        $"You have {UserPoints} points." +
                                        $"--------------------------------");
            if (round < 10) return;
            if (nadekoPoints == userPoints)
                await e.Channel.SendMessage("Its a draw");
            else if (nadekoPoints > userPoints)
                await e.Channel.SendMessage("Nadeko won.");
            else
                await e.Channel.SendMessage("You won.");
        }
    }
}
