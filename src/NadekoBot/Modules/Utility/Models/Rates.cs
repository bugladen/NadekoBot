using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NadekoBot.Modules.Utility.Commands.Models
{
    public class Rates
    {
        public string Base { get; set; }
        public DateTime Date { get; set; }
        [JsonProperty("rates")]
        public Dictionary<string, decimal> ConversionRates { get; set; }
    }
}
