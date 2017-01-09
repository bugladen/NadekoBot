using Discord;
using Discord.Commands;
using ImageSharp;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Image = ImageSharp.Image;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class DrawCommands : ModuleBase
        {
            private static readonly ConcurrentDictionary<IGuild, Cards> AllDecks = new ConcurrentDictionary<IGuild, Cards>();

            private const string cardsPath = "data/images/cards";

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Draw(int num = 1)
            {
                var cards = AllDecks.GetOrAdd(Context.Guild, (s) => new Cards());
                var images = new List<Image>();
                var cardObjects = new List<Cards.Card>();
                if (num > 5) num = 5;
                for (var i = 0; i < num; i++)
                {
                    if (cards.CardPool.Count == 0 && i != 0)
                    {
                        try { await Context.Channel.SendErrorAsync("No more cards in a deck.").ConfigureAwait(false); } catch { }
                        break;
                    }
                    var currentCard = cards.DrawACard();
                    cardObjects.Add(currentCard);
                    using (var stream = File.OpenRead(Path.Combine(cardsPath, currentCard.ToString().ToLowerInvariant()+ ".jpg").Replace(' ','_')))
                        images.Add(new Image(stream));
                }
                MemoryStream bitmapStream = new MemoryStream();
                images.Merge().SaveAsPng(bitmapStream);
                bitmapStream.Position = 0;
                var toSend = $"{Context.User.Mention}";
                if (cardObjects.Count == 5)
                    toSend += $" drew `{Cards.GetHandValue(cardObjects)}`";

                await Context.Channel.SendFileAsync(bitmapStream, images.Count + " cards.jpg", toSend).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ShuffleDeck()
            {
                //var channel = (ITextChannel)Context.Channel;

                AllDecks.AddOrUpdate(Context.Guild,
                        (g) => new Cards(),
                        (g, c) =>
                        {
                            c.Restart();
                            return c;
                        });

                await Context.Channel.SendConfirmAsync("Deck reshuffled.").ConfigureAwait(false);
            }
        }
    }
}