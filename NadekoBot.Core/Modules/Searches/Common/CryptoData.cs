using System.Collections.Generic;

namespace NadekoBot.Core.Modules.Searches.Common
{
    public class CryptoResponse
    {
        public Dictionary<string, CryptoData> Data { get; set; }
    }

    public class CryptoData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Website_Slug { get; set; }
        public int Rank { get; set; }
        public Dictionary<string, Quote> Quotes { get; set; }
    }

    public class Quote
    {
        public decimal Price { get; set; }
        public decimal Market_Cap { get; set; }
        public string Percent_Change_1h { get; set; }
        public string Percent_Change_24h { get; set; }
        public string Percent_Change_7d { get; set; }
        public decimal Volume_24h { get; set; }
    }
}
