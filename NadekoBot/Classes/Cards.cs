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

    public enum CARD_SUIT
    {
        Spades = 1,
        Hearts = 2,
        Diamonds = 3,
        Clubs = 4
    }

    public class Card
    {
        public CARD_SUIT suit;
		public int number;
        
        public string Path
        {
            get
            {
                string str = "";

                if (number <= 10 && number > 1)
                {
                    str += number;
                }
                else
                {
                    str += GetName().ToLower();
                }
                return @"./images/cards/" + str + "_of_" + suit.ToString().ToLower() + ".jpg";
            }
        }

        public Card(CARD_SUIT s, int card_num) {
            this.suit = s;
            this.number = card_num;
        }
        public string GetName() {
            return cardNames[number];
        }

        public override string ToString()
        {
            return cardNames[number] + " Of " + suit;
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
    }
    /// <summary>
    /// Restart the game of blackjack. It will only refill the pool for now. Probably wont be used, unless you want to have only 1 bjg running at one time,
    /// then you will restart the same game every time.
    /// </summary>
    public void Restart()
    {
        // you dont have to uncover what is actually happening anda da hood
        RefillPool();
    }

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
    /// <summary>
    /// Take a card from the pool, you either take it from the top if the deck is shuffled, or from a random place if the deck is in the default order.
    /// </summary>
    /// <returns>A card from the pool</returns>
    public Card DrawACard()
    {
        if (CardPool.Count > 0)
        {
            //you can either do this if your deck is not shuffled
            Random r = new Random((int)DateTime.Now.Ticks);
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
        else {

            //for now return null
            Restart();
            return null;
        }
    }
    /// <summary>
    /// Shuffles the deck. Use this if you want to take cards from the top of the deck, instead of randomly. See DrawACard method.
    /// </summary>
    private void Shuffle() {
        if (cardPool.Count > 1)
        {
            Random r = new Random();
            cardPool.OrderBy(x => r.Next());
        }
    }
    //public override string ToString() => string.Join("", cardPool.Select(c => c.ToString())) + Environment.NewLine;
    public override string ToString() => string.Join("", cardPool.Select(c => c.ToString())) + Environment.NewLine;
}

