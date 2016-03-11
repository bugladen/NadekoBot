using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules;

namespace NadekoBot.Commands {
    internal class DiceRollCommand : DiscordCommand {

        public DiceRollCommand(DiscordModule module) : base(module) { }

        public Func<CommandEventArgs, Task> DoFunc() {
            var r = new Random();
            return async e => {
                if (e.Args[0] == "") {
                    var num1 = r.Next(0, 10);
                    var num2 = r.Next(0, 10);

                    Image[] images;

                    if (num1 == 0 && num2 == 0 && r.Next(0, 2) == 1) {
                        images = new Image[3] { GetDice(1), GetDice(0), GetDice(0) };
                    } else {
                        images = new Image[2] { GetDice(num1), GetDice(num2) };
                    }

                    var bitmap = images.Merge();
                    await e.Channel.SendFile("dice.png", bitmap.ToStream(ImageFormat.Png));
                } else {
                    try {
                        var num = int.Parse(e.Args[0]);
                        if (num < 1) num = 1;
                        if (num > 30) {
                            await e.Channel.SendMessage("You can roll up to 30 dies at a time.");
                            num = 30;
                        }
                        var dices = new List<Image>(num);
                        var values = new List<int>(num);
                        for (var i = 0; i < num; i++) {
                            var randomNumber = r.Next(1, 7);
                            var toInsert = dices.Count;
                            if (randomNumber == 6 || dices.Count == 0)
                                toInsert = 0;
                            else if (randomNumber != 1)
                                for (var j = 0; j < dices.Count; j++) {
                                    if (values[j] < randomNumber) {
                                        toInsert = j;
                                        break;
                                    }
                                }
                            dices.Insert(toInsert, GetDice(randomNumber));
                            values.Insert(toInsert, randomNumber);
                        }

                        var bitmap = dices.Merge();
                        await e.Channel.SendMessage(values.Count + " Dice rolled. Total: **" + values.Sum() + "** Average: **" + (values.Sum() / (1.0f * values.Count)).ToString("N2") + "**");
                        await e.Channel.SendFile("dice.png", bitmap.ToStream(ImageFormat.Png));
                    } catch {
                        await e.Channel.SendMessage("Please enter a number of dice to roll.");
                    }
                }
            };
        }

        private Image GetDice(int num) => Properties.Resources.ResourceManager.GetObject("_" + num) as Image;

        internal override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(Module.Prefix + "roll")
                .Description("Rolls 2 dice from 0-10. If you supply a number [x] it rolls up to 30 normal dice.\n**Usage**: $roll [x]")
                .Parameter("num", ParameterType.Optional)
                .Do(DoFunc());
            cgb.CreateCommand(Module.Prefix + "nroll")
                .Description("Rolls in a given range.\n**Usage**: `$nroll 5` (rolls 0-5) or `$nroll 5-15`")
                .Parameter("range", ParameterType.Required)
                .Do(NDoFunc());
        }

        private Func<CommandEventArgs, Task> NDoFunc() =>
            async e => {
                try {
                    int rolled;
                    if (e.GetArg("range").Contains("-")) {
                        var arr = e.GetArg("range").Split('-')
                                                 .Take(2)
                                                 .Select(int.Parse)
                                                 .ToArray();
                        if (arr[0] > arr[1])
                            throw new ArgumentException("First argument should be bigger than the second one.");
                        rolled = new Random().Next(arr[0], arr[1] + 1);
                    } else {
                        rolled = new Random().Next(0, int.Parse(e.GetArg("range")) + 1);
                    }

                    await e.Channel.SendMessage($"{e.User.Mention} rolled **{rolled}**.");
                } catch (Exception ex) {
                    await e.Channel.SendMessage($":anger: {ex.Message}");
                }
            };
    }
}