using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace NadekoBot
{
    class DrawCommand : DiscordCommand
    {
        private Cards cards = null;

        public DrawCommand() : base() { }

        public override Func<CommandEventArgs, Task> DoFunc() => async (e) =>
          {
              if (cards == null)
              {
                  await e.Send("Shuffling cards...");
                  cards = new Cards();
              }

              try
              {
                  int num = 1;
                  var isParsed = int.TryParse(e.GetArg("count"), out num);
                  if (!isParsed || num < 2)
                  {
                      await client.SendFile(e.Channel, cards.DrawACard().Path);
                      return;
                  }
                  if (num > 5)
                      num = 5;

                  List<Image> images = new List<Image>();
                  List<Cards.Card> cardObjects = new List<Cards.Card>();
                  for (int i = 0; i < num; i++)
                  {
                      if (cards.CardPool.Count == 0 && i != 0)
                      {
                          await e.Send("No more cards in a deck.");
                          break;
                      }
                      var currentCard = cards.DrawACard();
                      cardObjects.Add(currentCard);
                      images.Add(Image.FromFile(currentCard.Path));
                  }
                  Bitmap bitmap = ImageHandler.MergeImages(images);
                  await client.SendFile(e.Channel, images.Count + " cards.jpg", ImageHandler.ImageToStream(bitmap, ImageFormat.Jpeg));
                  if (cardObjects.Count == 5)
                  {
                      await e.Send(Cards.GetHandValue(cardObjects));
                  }
              }
              catch (Exception ex)
              {
                  Console.WriteLine("Error drawing (a) card(s) " + ex.ToString());
              }
          };

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("$draw")
                .Description("Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck.\nUsage: $draw [x]")
                .Parameter("count", ParameterType.Optional)
                .Do(DoFunc());
        }
        
    }
}
