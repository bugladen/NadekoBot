using Discord.Commands;
using NadekoBot.Commands;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling
{
    internal class DiceRollCommand : DiscordCommand
    {

        public DiceRollCommand(DiscordModule module) : base(module) { }


        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "roll")
                .Description("Rolls 0-100. If you supply a number [x] it rolls up to 30 normal dice.\n**Usage**: $roll [x]")
                .Parameter("num", ParameterType.Optional)
                .Do(Roll0to10Func());
            cgb.CreateCommand(Module.Prefix + "nroll")
                .Description("Rolls in a given range.\n**Usage**: `$nroll 5` (rolls 0-5) or `$nroll 5-15`")
                .Parameter("range", ParameterType.Required)
                .Do(Roll0to5Func());
        }

        private Image GetDice(int num) => num != 10
                                          ? Properties.Resources.ResourceManager.GetObject("_" + num) as Image
                                          : new[]
                                            {
                                              (Properties.Resources.ResourceManager.GetObject("_" + 1) as Image),
                                              (Properties.Resources.ResourceManager.GetObject("_" + 0) as Image),
                                            }.Merge();

        private Func<CommandEventArgs, Task> Roll0to10Func()
        {
            var r = new Random();
            return async e =>
            {
                if (e.Args[0] == "")
                {
                    var gen = r.Next(0, 101);

                    var num1 = gen / 10;
                    var num2 = gen % 10;

                    var imageStream = new Image[2] { GetDice(num1), GetDice(num2) }.Merge().ToStream(ImageFormat.Png);

                    await e.Channel.SendFile("dice.png", imageStream);
                }
                else
                {
                    try
                    {
                        var num = int.Parse(e.Args[0]);
                        if (num < 1) num = 1;
                        if (num > 30)
                        {
                            await e.Channel.SendMessage("You can roll up to 30 dice at a time.");
                            num = 30;
                        }
                        var dices = new List<Image>(num);
                        var values = new List<int>(num);
                        for (var i = 0; i < num; i++)
                        {
                            var randomNumber = r.Next(1, 7);
                            var toInsert = dices.Count;
                            if (randomNumber == 6 || dices.Count == 0)
                                toInsert = 0;
                            else if (randomNumber != 1)
                                for (var j = 0; j < dices.Count; j++)
                                {
                                    if (values[j] < randomNumber)
                                    {
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
                    }
                    catch
                    {
                        await e.Channel.SendMessage("Please enter a number of dice to roll.");
                    }
                }
            };
        }


        private Func<CommandEventArgs, Task> Roll0to5Func() =>
            async e =>
            {
                try
                {
                    int rolled;
                    if (e.GetArg("range").Contains("-"))
                    {
                        var arr = e.GetArg("range").Split('-')
                                                 .Take(2)
                                                 .Select(int.Parse)
                                                 .ToArray();
                        if (arr[0] > arr[1])
                            throw new ArgumentException("First argument should be bigger than the second one.");
                        rolled = new Random().Next(arr[0], arr[1] + 1);
                    }
                    else
                    {
                        rolled = new Random().Next(0, int.Parse(e.GetArg("range")) + 1);
                    }

                    await e.Channel.SendMessage($"{e.User.Mention} rolled **{rolled}**.");
                }
                catch (Exception ex)
                {
                    await e.Channel.SendMessage($":anger: {ex.Message}");
                }
            };
    }
}