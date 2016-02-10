using System.Collections.Generic;
using System.Linq;
using System;

public class Cards
{
    private static Dictionary<int, string> cardNames = new Dictionary<int, string>() {
        { 1, "Ace" },
        { 2, "Two" },
        { 3, "Three" },
        { 4, "Four" },
        { 5, "Five" },
        { 6, "Six" },
        { 7, "Seven" },
        { 8, "Eight" },
        { 9, "Nine" },
        { 10, "Ten" },
        { 11, "Jack" },
        { 12, "Queen" },
        { 13, "King" },
    };

    private static Dictionary<string, Func<List<Card>, bool>> handValues;


    public enum CARD_SUIT
    {
        Spades = 1,
        Hearts = 2,
        Diamonds = 3,
        Clubs = 4
    }

    public class Card : IComparable
    {
        public CARD_SUIT suit;
		public int number;
        
        public string Name
        {
            get
            {
                string str = "";

                if (number <= 10 && number > 1)
                {
                    str += "_"+number;
                }
                else
                {
                    str += GetName().ToLower();
                }
                return str + "_of_" + suit.ToString().ToLower();
            }
        }

        public Card(CARD_SUIT s, int card_num) {
            this.suit = s;
            this.number = card_num;
        }

        public string GetName() => cardNames[number];

        public override string ToString() => cardNames[number] + " Of " + suit;

        public int CompareTo(object obj)
        {
            if (!(obj is Card)) return 0;
            var c = (Card)obj;
            return this.number - c.number;
        }
    }

    private List<Card> cardPool;
    public List<Card> CardPool
    {
        get { return cardPool; }
        set { cardPool = value; }
    }

    /// <summary>
    /// Creates a new instance of the BlackJackGame, this allows you to create multiple games running at one time.
    /// </summary>
    public Cards()
    {
        cardPool = new List<Card>(52);
        RefillPool();
        InitHandValues();
    }
    /// <summary>
    /// Restart the game of blackjack. It will only refill the pool for now. Probably wont be used, unless you want to have only 1 bjg running at one time,
    /// then you will restart the same game every time.
    /// </summary>
    public void Restart() => RefillPool();

    /// <summary>
    /// Removes all cards from the pool and refills the pool with all of the possible cards. NOTE: I think this is too expensive.
    /// We should probably make it so it copies another premade list with all the cards, or something.
    /// </summary>
    private void RefillPool()
    {
        cardPool.Clear();
        //foreach suit
        for (int j = 1; j < 14; j++)
        {
            // and number
            for (int i = 1; i < 5; i++)
            {
                //generate a card of that suit and number and add it to the pool

                // the pool will go from ace of spades,hears,diamonds,clubs all the way to the king of spades. hearts, ...
                cardPool.Add(new Card((CARD_SUIT)i, j));
            }
        }
    }
    private Random r = new Random();
    /// <summary>
    /// Take a card from the pool, you either take it from the top if the deck is shuffled, or from a random place if the deck is in the default order.
    /// </summary>
    /// <returns>A card from the pool</returns>
    public Card DrawACard()
    {
        if (CardPool.Count == 0)
            Restart();
        //you can either do this if your deck is not shuffled
        
        int num = r.Next(0, cardPool.Count);
        Card c = cardPool[num];
        cardPool.RemoveAt(num);
        return c;

        // if you want to shuffle when you fill, then take the first one
        /*
        Card c = cardPool[0];
        cardPool.RemoveAt(0);
        return c;
        */
    }
    /// <summary>
    /// Shuffles the deck. Use this if you want to take cards from the top of the deck, instead of randomly. See DrawACard method.
    /// </summary>
    private void Shuffle() {
        if (cardPool.Count > 1)
        {
            cardPool.OrderBy(x => r.Next());
        }
    }
    public override string ToString() => string.Join("", cardPool.Select(c => c.ToString())) + Environment.NewLine;

    public void InitHandValues() {
        Func<List<Card>, bool> hasPair =
                              cards => cards.GroupBy(card => card.number)
                                            .Count(group => group.Count() == 2) == 1;
        Func<List<Card>, bool> isPair =
                              cards => cards.GroupBy(card => card.number)
                                            .Count(group => group.Count() == 3) == 0
                                       && hasPair(cards);

        Func<List<Card>, bool> isTwoPair =
                              cards => cards.GroupBy(card => card.number)
                                            .Count(group => group.Count() >= 2) == 2;

        Func<List<Card>, bool> isStraight =
                              cards => cards.GroupBy(card => card.number)
                                            .Count() == cards.Count()
                                       && cards.Max(card => (int)card.number)
                                        - cards.Min(card => (int)card.number) == 4;

        Func<List<Card>, bool> hasThreeOfKind =
                              cards => cards.GroupBy(card => card.number)
                                            .Any(group => group.Count() == 3);

        Func<List<Card>, bool> isThreeOfKind =
                              cards => hasThreeOfKind(cards) && !hasPair(cards);

        Func<List<Card>, bool> isFlush =
                              cards => cards.GroupBy(card => card.suit).Count() == 1;

        Func<List<Card>, bool> isFourOfKind =
                              cards => cards.GroupBy(card => card.number)
                                            .Any(group => group.Count() == 4);

        Func<List<Card>, bool> isFullHouse =
                              cards => hasPair(cards) && hasThreeOfKind(cards);

        Func<List<Card>, bool> hasStraightFlush =
                              cards => isFlush(cards) && isStraight(cards);

        Func<List<Card>, bool> isRoyalFlush =
                              cards => cards.Min(card => (int)card.number) == 10
                                       && hasStraightFlush(cards);

        Func<List<Card>, bool> isStraightFlush =
                              cards => hasStraightFlush(cards) && !isRoyalFlush(cards);

        handValues = new Dictionary<string, Func<List<Card>, bool>>
                     {
                         { "Royal Flush", isRoyalFlush },
                         { "Straight Flush", isStraightFlush },
                         { "Four Of A Kind", isFourOfKind },
                         { "Full House", isFullHouse },
                         { "Flush", isFlush },
                         { "Straight", isStraight },
                         { "Three Of A Kind", isThreeOfKind },
                         { "Two Pairs", isTwoPair },
                         { "A Pair", isPair }
                     };
    }

    public static string GetHandValue(List<Card> cards)
    {
        foreach (var KVP in handValues)
        {
            if (KVP.Value(cards)) {
                return KVP.Key;
            }
        }
        return "High card "+cards.Max().GetName();
    }
}

