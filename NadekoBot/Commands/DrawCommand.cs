using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

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
                    int num = 1;
                    var isParsed = int.TryParse(e.GetArg("count"), out num);
                    if (!isParsed || num <2)
                    {
                        await client.SendFile(e.Channel, cards.DrawACard().Path);
                        return;
                    }
                    if (num > 5)
                        num = 5;

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
                    await client.SendFile(e.Channel, num+" cards.jpg",ImageHandler.ImageToStream(bitmap,ImageFormat.Jpeg));
                }
                catch (Exception ex) {
                    Console.WriteLine("Error drawing (a) card(s) "+ex.ToString());
                }
            };
        }

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("$draw")
                .Description("Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck.\nUsage: $draw [x]")
                .Parameter("count", ParameterType.Optional)
                .Do(DoFunc());
        }
        
    }
}
