using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Core.Common
{
    public class ImageUrls
    {
        public CoinData Coins { get; set; }
        public string[] Currency { get; set; }
        public string[] Dice { get; set; }
        public RategirlData Rategirl { get; set; }
        public XpData Xp { get; set; }

        public class CoinData
        {
            public string[] Heads { get; set; }
            public string[] Tails { get; set; }
        }

        public class RategirlData
        {
            public string Matrix { get; set; }
            public string Dot { get; set; }
        }

        public class XpData
        {
            public string Bg { get; set; }
        }
    }
}
