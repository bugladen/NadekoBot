using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Common
{
    public class OverwatchApiModel
    {
        public OverwatchPlayer Player { get; set; }

        public class OverwatchResponse
        {
            public object _request;
            public OverwatchPlayer Eu { get; set; }
            public OverwatchPlayer Kr { get; set; }
            public OverwatchPlayer Us { get; set; }
        }
        public class OverwatchPlayer
        {
            public StatsField Stats { get; set; }
        }

        public class StatsField
        {
            public Stats Quickplay { get; set; }
            public Stats Competitive { get; set; }

        }

        public class Stats
        {
            [JsonProperty("overall_stats")]
            public OverallStats OverallStats { get; set; }
            [JsonProperty("game_stats")]
            public GameStats GameStats { get; set; }
        }

        public class OverallStats
        {
            public int? comprank;
            public int level;
            public int prestige;
            public string avatar;
            public int wins;
            public int losses;
            public int games;
            public string rank_image;
        }

        public class GameStats
        {
            [JsonProperty("time_played")]
            public float timePlayed;
        }


        public class Competitive
        {

        }
    }
}