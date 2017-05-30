using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling.Models;
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
        public class DrawCommands : NadekoSubmodule
        {
            private static readonly ConcurrentDictionary<IGuild, Cards> _allDecks = new ConcurrentDictionary<IGuild, Cards>();
            private const string _cardsPath = "data/images/cards";

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Draw(int num = 1)
            {
                if (num < 1)
                    num = 1;
                var cards = _allDecks.GetOrAdd(Context.Guild, (s) => new Cards());
                var images = new List<Image>();
                var cardObjects = new List<Cards.Card>();
                if (num > 10) num = 10;
                for (var i = 0; i < num; i++)
                {
                    if (cards.CardPool.Count == 0 && i != 0)
                    {
                        try
                        {
                            await ReplyErrorLocalized("no_more_cards").ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                        break;
                    }
                    var currentCard = cards.DrawACard();
                    cardObjects.Add(currentCard);
                    using (var stream = File.OpenRead(Path.Combine(_cardsPath, currentCard.ToString().ToLowerInvariant()+ ".jpg").Replace(' ','_')))
                        images.Add(new Image(stream));
                }
                MemoryStream bitmapStream = new MemoryStream();
                images.Merge().Save(bitmapStream);
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

                _allDecks.AddOrUpdate(Context.Guild,
                        (g) => new Cards(),
                        (g, c) =>
                        {
                            c.Restart();
                            return c;
                        });

                await ReplyConfirmLocalized("deck_reshuffled").ConfigureAwait(false);
            }
        }
    }
}