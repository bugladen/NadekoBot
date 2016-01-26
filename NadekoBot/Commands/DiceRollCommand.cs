using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using System.Drawing;
using System.Drawing.Imaging;
using NadekoBot.Extensions;

namespace NadekoBot
{
    class DiceRollCommand : DiscordCommand
    {
        public DiceRollCommand() : base() { }

        public override Func<CommandEventArgs, Task> DoFunc()
        {
            Random r = new Random();
            return async e => {
                if (e.Args[0] == "")
                {
                    int num1 = r.Next(0, 10);
                    int num2 = r.Next(0, 10);

                    Image[] images;

                    if (num1 == 0 && num2 == 0 && r.Next(0, 2) == 1)
                    {
                        images = new Image[3] { GetDice(1), GetDice(0), GetDice(0) };
                    }
                    else
                    {
                        images = new Image[2] { GetDice(num1), GetDice(num2) };
                    }

                    Bitmap bitmap = ImageHandler.MergeImages(images);
                    await e.Channel.SendFile("dice.png", ImageHandler.ImageToStream(bitmap, ImageFormat.Png));
                    return;
                }
                else
                {
                    try
                    {
                        int num = int.Parse(e.Args[0]);
                        if (num < 1) num = 1;
                        if (num > 30)
                        {
                            await e.Send( "You can roll up to 30 dies at a time.");
                            num = 30;
                        }
                        List<Image> dices = new List<Image>(num);
                        List<int> values = new List<int>(num);
                        for (int i = 0; i < num; i++)
                        {
                            int randomNumber = r.Next(1, 7);
                            int toInsert = dices.Count;
                            if (randomNumber == 6 || dices.Count==0)
                                toInsert = 0;
                            else if (randomNumber!=1)
                                for (int j = 0; j < dices.Count; j++) {
                                    if (values[j] < randomNumber)
                                    {
                                        toInsert = j;
                                        break;
                                    }
                                }
                            dices.Insert(toInsert, GetDice(randomNumber));
                            values.Insert(toInsert, randomNumber);
                        }

                        Bitmap bitmap = ImageHandler.MergeImages(dices.ToArray());
                        await e.Send( values.Count + " Dies rolled. Total: **"+values.Sum()+"** Average: **"+(values.Sum()/(1.0f*values.Count)).ToString("N2")+"**");
                        await e.Channel.SendFile("dices.png", ImageHandler.ImageToStream(bitmap, ImageFormat.Png));
                    }
                    catch (Exception)
                    {
                        await e.Send( "Please enter a number of dices to roll.");
                        return;
                    }
                }
            };
        }

        private Image GetDice(int num) => Image.FromFile("images/dice/" + num + ".png");

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("$roll")
                .Description("Rolls 2 dice from 0-10. If you supply a number [x] it rolls up to 30 normal dice.\n**Usage**: $roll [x]")
                .Parameter("num", ParameterType.Optional)
                .Do(DoFunc());
        }
    }
}