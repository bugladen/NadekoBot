using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NadekoBot.Modules.Searches.Models
{
    public class OverwatchApiModel
    {
        public OverwatchPlayer Player { get; set; }

        public class OverwatchPlayer
        {
            public Data data { get; set; }
            public class Data
            {
                public bool Missing { get; set; } = false;
                public string username { get; set; }
                public int level { get; set; }
                public string avatar { get; set; }
                public string levelFrame { get; set; }
                public string star { get; set; }
                [JsonProperty("games")]
                public OverwatchGames Games { get; set; }
                [JsonProperty("playtime")]
                public OverwatchPlaytime Playtime { get; set; }
                [JsonProperty("competitive")]
                public OverwatchCompetitive Competitive { get; set; }
                public class OverwatchGames
                {
                    [JsonProperty("quick")]
                    public OverwatchQG Quick { get; set; }
                    [JsonProperty("competitive")]
                    public OverwatchCOMP Competitive { get; set; }

                    public class OverwatchQG
                    {
                        public string wins { get; set; }
                    }
                    public class OverwatchCOMP
                    {
                        public string wins { get; set; }
                        public int lost { get; set; }
                        public string played { get; set; }
                    }
                }
                public class OverwatchCompetitive
                {
                    public string rank { get; set; }
                    public string rank_img { get; set; }
                }
                public class OverwatchPlaytime
                {
                    public string quick { get; set; }
                    public string competitive { get; set; }
                }
            }
        }
        //This is to strip the html from patch notes content
        internal static string StripHTML(string input)
        {
            var re = Regex.Replace(input, "<.*?>", String.Empty);
            re = Regex.Replace(re, "&#160;", $@" ");
            return re;
        }
    }
}