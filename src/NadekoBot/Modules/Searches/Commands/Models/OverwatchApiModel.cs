using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NadekoBot.Modules.Searches.Models
{
    public class OverwatchApiModel
    {

        //patch notes
        [JsonProperty("patchNotes")]
        public List<OverwatchPatchNotes> PatchNotes { get; set; }
        [JsonProperty("pagination")]
        public OverwatchPagination Pagination { get; set; }

        //Player All Heroes Stats
        public OverwatchAllHeroes AllHeroes { get; set; }

        //achievements
        [JsonProperty("achievements")]
        public List<OverwatchAchievements> Achievements { get; set; }
        public float totalNumberOfAchievements { get; set; }
        public float numberOfAchievementsCompleted { get; set; }
        public string finishedAchievements { get; set; }

        public OverwatchPlayer Player { get; set; }


        public class OverwatchAllHeroes
        {
            public string MeleeFinalBlows { get; set; }
            public string SoloKills { get; set; }
            public string ObjectiveKills { get; set; }
            public string FinalBlows { get; set; }
            public string DamageDone { get; set; }
            public string Eliminations { get; set; }
            public string Multikills { get; set; }
            public string ReconAssists { get; set; }
            public string HealingDone { get; set; }
            public string TeleporterPadDestroyed { get; set; }
            [JsonProperty("Eliminations-MostinGame")]
            public string Eliminations_MostinGame { get; set; }
            [JsonProperty("FinalBlows-MostinGame")]
            public string FinalBlows_MostinGame { get; set; }
            [JsonProperty("DamageDone-MostinGame")]
            public string DamageDone_MostinGame { get; set; }
            [JsonProperty("HealingDone-MostinGame")]
            public string HealingDone_MostinGame { get; set; }
            [JsonProperty("DefensiveAssists-MostinGame")]
            public string DefensiveAssists_MostinGame { get; set; }
            [JsonProperty("OffensiveAssists-MostinGame")]
            public string OffensiveAssists_MostinGame { get; set; }
            [JsonProperty("ObjectiveKills-MostinGame")]
            public string ObjectiveKills_MostinGame { get; set; }
            [JsonProperty("ObjectiveTime-MostinGame")]
            public string ObjectiveTime_MostinGame { get; set; }
            [JsonProperty("Multikill-Best")]
            public string Multikill_Best { get; set; }
            [JsonProperty("SoloKills-MostinGame")]
            public string SoloKills_MostinGame { get; set; }
            [JsonProperty("TimeSpentonFire-MostinGame")]
            public string TimeSpentonFire_MostinGame { get; set; }
            [JsonProperty("MeleeFinalBlows-Average")]
            public string MeleeFinalBlows_Average { get; set; }
            [JsonProperty("TimeSpentonFire-Average")]
            public string TimeSpentonFire_Average { get; set; }
            [JsonProperty("SoloKills-Average")]
            public string SoloKills_Average { get; set; }
            [JsonProperty("ObjectiveTime-Average")]
            public string ObjectiveTime_Average { get; set; }
            [JsonProperty("ObjectiveKills-Average")]
            public string ObjectiveKills_Average { get; set; }
            [JsonProperty("HealingDone-Average")]
            public string HealingDone_Average { get; set; }
            [JsonProperty("FinalBlows-Average")]
            public string FinalBlows_Average { get; set; }
            [JsonProperty("Deaths-Average")]
            public string Deaths_Average { get; set; }
            [JsonProperty("DamageDone-Average")]
            public string DamageDone_Average { get; set; }
            [JsonProperty("Eliminations-Average")]
            public string Eliminations_Average { get; set; }
            public string Deaths { get; set; }
            public string EnvironmentalDeaths { get; set; }
            public string Cards { get; set; }
            public string Medals { get; set; }
            [JsonProperty("Medals-Gold")]
            public string Medals_Gold { get; set; }
            [JsonProperty("Medals-Silver")]
            public string Medals_Silver { get; set; }
            [JsonProperty("Medals-Bronze")]
            public string Medals_Bronze { get; set; }
            public string GamesPlayed { get; set; }
            public string GamesWon { get; set; }
            public string TimeSpentonFire { get; set; }
            public string ObjectiveTime { get; set; }
            public string TimePlayed { get; set; }
            [JsonProperty("MeleeFinalBlows-MostinGame")]
            public string MeleeFinalBlows_MostinGame { get; set; }
            public string GamesTied { get; set; }
            public string GamesLost { get; set; }
            [JsonProperty("ReconAssists-Average")]
            public string ReconAssists_Average { get; set; }
            public string DefensiveAssists { get; set; }
            [JsonProperty("DefensiveAssists-Average")]
            public string DefensiveAssists_Average { get; set; }
            public string OffensiveAssists { get; set; }
            [JsonProperty("OffensiveAssists-Average")]
            public string OffensiveAssists_Average { get; set; }
        }
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

        public class OverwatchPatchNotes
        {
            public bool Missing { get; set; } = false;
            public string program { get; set; }
            public string locale { get; set; }
            public string type { get; set; }
            public string patchVersion { get; set; }
            public string status { get; set; }
            public string detail { get; set; }
            public int buildNumber { get; set; }
            public object publish { get; set; }
            public object created { get; set; }
            public bool updated { get; set; }
            public string slug { get; set; }
            public string version { get; set; }
        }

        public class OverwatchPagination
        {
            public bool Missing { get; set; } = false;
            public float totalEntries { get; set; }
            public float totalPages { get; set; }
            public float pageSize { get; set; }
            public float page { get; set; }
        }

        public class OverwatchAchievements
        {
            public bool Missing { get; set; } = false;
            public string name { get; set; }
            public bool finished { get; set; }
            public string image { get; set; }
            public string description { get; set; }
            public object category { get; set; }
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