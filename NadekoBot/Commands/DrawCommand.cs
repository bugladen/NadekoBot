using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NadekoBot.Extensions;

namespace NadekoBot {
    class DrawCommand : DiscordCommand {
        private static ConcurrentDictionary<Discord.Server, Cards> AllDecks = new ConcurrentDictionary<Discord.Server, Cards>();

        public DrawCommand() : base() {

        }

        public override Func<CommandEventArgs, Task> DoFunc() => async (e) => {
            if (!AllDecks.ContainsKey(e.Server)) {
                await e.Send("Shuffling cards...");
                AllDecks.TryAdd(e.Server, new Cards());
            }

            try {
                var cards = AllDecks[e.Server];
                int num = 1;
                var isParsed = int.TryParse(e.GetArg("count"), out num);
                if (!isParsed || num < 2) {
                    var c = cards.DrawACard();
                    await e.Channel.SendFile(c.Name + ".jpg", (Properties.Resources.ResourceManager.GetObject(c.Name) as Image).ToStream());
                    return;
                }
                if (num > 5)
                    num = 5;

                List<Image> images = new List<Image>();
                List<Cards.Card> cardObjects = new List<Cards.Card>();
                for (int i = 0; i < num; i++) {
                    if (cards.CardPool.Count == 0 && i != 0) {
                        await e.Send("No more cards in a deck.");
                        break;
                    }
                    var currentCard = cards.DrawACard();
                    cardObjects.Add(currentCard);
                    images.Add(Properties.Resources.ResourceManager.GetObject(currentCard.Name) as Image);
                }
                Bitmap bitmap = images.Merge();
                await e.Channel.SendFile(images.Count + " cards.jpg", bitmap.ToStream());
                if (cardObjects.Count == 5) {
                    await e.Send(Cards.GetHandValue(cardObjects));
                }
            } catch (Exception ex) {
                Console.WriteLine("Error drawing (a) card(s) " + ex.ToString());
            }
        };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("$draw")
                .Description("Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck.\n**Usage**: $draw [x]")
                .Parameter("count", ParameterType.Optional)
                .Do(DoFunc());

            cgb.CreateCommand("$shuffle")
                .Alias("$reshuffle")
                .Description("Reshuffles all cards back into the deck.")
                .Do(async e => {
                    if (!AllDecks.ContainsKey(e.Server))
                        AllDecks.TryAdd(e.Server, new Cards());
                    AllDecks[e.Server].Restart();
                    await e.Send("Deck reshuffled.");
                });
        }

    }
}
