using Newtonsoft.Json;

namespace NadekoBot.Core.Modules.Searches.Common
{
    public class CryptoData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public int Rank { get; set; }
        public string Price_Usd { get; set; }
        public string Price_Btc { get; set; }
        public decimal? Market_Cap_Usd { get; set; }
        [JsonProperty("24h_volume_usd")]
        public double? _24h_Volume_Usd { get; set; }
        public string Percent_Change_1h { get; set; }
        public string Percent_Change_24h { get; set; }
        public string Percent_Change_7d { get; set; }
        public string LastUpdated { get; set; }
    }
}
