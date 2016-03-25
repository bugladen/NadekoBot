using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using static NadekoBot.Modules.Gambling.Helpers.Cards;

namespace Tests
{
    [TestClass]
    public class TestCards
    {
        [TestMethod]
        public void TestHandValues()
        {
            var setting1 = new List<Card> {
                new Card(CardSuit.Clubs,10),
                new Card(CardSuit.Clubs,10),
                new Card(CardSuit.Clubs,10),
                new Card(CardSuit.Clubs,11),
                new Card(CardSuit.Diamonds,12),
            };
            var result1 = "Three Of A Kind";

            var setting2 = new List<Card> {
                new Card(CardSuit.Clubs,1),
                new Card(CardSuit.Hearts,2),
                new Card(CardSuit.Clubs,3),
                new Card(CardSuit.Spades,4),
                new Card(CardSuit.Diamonds,5),
            };
            var result2 = "Straight";

            var setting3 = new List<Card> {
                new Card(CardSuit.Diamonds,10),
                new Card(CardSuit.Diamonds,11),
                new Card(CardSuit.Diamonds,12),
                new Card(CardSuit.Diamonds,13),
                new Card(CardSuit.Diamonds,1),
            };
            var result3 = "Royal Flush";

            Assert.AreEqual(GetHandValue(setting1), result1);
            Assert.AreEqual(GetHandValue(setting2), result2);
            Assert.AreEqual(GetHandValue(setting3), result3);
        }
    }
}
