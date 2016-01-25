using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using Discord.Legacy;
using NadekoBot.Extensions;

namespace NadekoBot
{
    class DrawCommand : DiscordCommand
    {
        private Cards cards = null;

        public DrawCommand() : base() {
            cards = new Cards();
        }

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
                      await e.Channel.SendFile(cards.DrawACard().Path);
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
                  await e.Channel.SendFile(images.Count + " cards.jpg", ImageHandler.ImageToStream(bitmap, ImageFormat.Jpeg));
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
                .Description("Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck.\n**Usage**: $draw [x]")
                .Parameter("count", ParameterType.Optional)
                .Do(DoFunc());

            cgb.CreateCommand("$shuffle")
                .Alias("$reshuffle")
                .Description("Reshuffles all cards back into the deck.")
                .Do(async e => {
                    if (cards == null) {
                        cards = new Cards();
                    }
                    cards.Restart();
                    await e.Send("Deck reshuffled.");
                });
        }
        
    }
}
