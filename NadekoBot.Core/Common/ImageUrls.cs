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
        public Uri[] Currency { get; set; }
        public Uri[] Dice { get; set; }
        public RategirlData Rategirl { get; set; }
        public XpData Xp { get; set; }

        public class CoinData
        {
            public Uri[] Heads { get; set; }
            public Uri[] Tails { get; set; }
        }

        public class RategirlData
        {
            public Uri Matrix { get; set; }
            public Uri Dot { get; set; }
        }

        public class XpData
        {
            public Uri Bg { get; set; }
        }
    }
}
