using NadekoBot.Core.Modules.Gambling.Common.Blackjack;
using NadekoBot.Modules.Gambling.Common;
using NUnit.Framework;
using System;

namespace NadekoBot.Tests
{
    [TestFixture]
    public class BlackJackTests
    {
        [Test]
        public void TestHandValues()
        {
            var usr = new User(null, 1);
            usr.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 1));
            usr.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 13));

            var usr1 = new User(null, 1);
            usr1.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 1));
            usr1.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 13));
            usr1.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 13));

            var usr2 = new User(null, 1);
            usr2.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 9));
            usr2.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 8));
            usr2.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 1));

            var usr3 = new User(null, 1);
            usr3.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 4));
            usr3.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 4));
            usr3.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 1));
            usr3.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 7));
            usr3.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 3));

            var usr5 = new User(null, 1);
            usr5.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 7));
            usr5.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 1));
            usr5.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 11));

            var usr4 = new User(null, 1);
            usr4.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 2));
            usr4.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 2));
            usr4.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 5));
            usr4.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 2));
            usr4.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 1));
            usr4.Cards.Add(new Deck.Card(Deck.CardSuit.Clubs, 13));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(21, usr.GetHandValue());
                Assert.AreEqual(21, usr1.GetHandValue());
                Assert.AreEqual(18, usr2.GetHandValue());
                Assert.AreEqual(19, usr3.GetHandValue());
                Assert.AreEqual(18, usr5.GetHandValue());
                Assert.AreEqual(22, usr4.GetHandValue());
            });
        }
    }
}
