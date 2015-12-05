using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System.Drawing;

namespace NadekoBot
{
    class DrawCommand : DiscordCommand
    {
        private Cards cards = null;

        public DrawCommand() : base() { }

        public override Func<CommandEventArgs,Task> DoFunc() {
            return async (e) =>
            {
                if (cards == null)
                {
                    cards = new Cards();
                    await client.SendMessage(e.Channel, "Shuffling cards...");
                }

                try
                {
                    int num = int.Parse(e.Args[0]);
                    if (num > 5)
                    {
                        num = 5;
                    }
                    else if (num < 1)
                    {
                        num = 1;
                    }
                    Image[] images = new Image[num];
                    for (int i = 0; i < num; i++)
                    {
                        if (cards.CardPool.Count == 0)
                        {
                            await client.SendMessage(e.Channel, "No more cards in a deck...\nGetting a new deck...\nShuffling cards...");
                        }
                        images[i] = Image.FromFile(cards.DrawACard().Path);
                    }
                    Bitmap bitmap = ImageHandler.MergeImages(images);
                    bitmap.Save("cards.png");
                    await client.SendFile(e.Channel, "cards.png");
                }
                catch (Exception) {
                    Console.WriteLine("Error drawing (a) card(s)");
                }
            };
        }

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("$draw")
                .Description("Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck.\nUsage: $draw [x]")
                .Parameter("count", ParameterType.Multiple)
                .Do(DoFunc());
        }
        
    }
}
