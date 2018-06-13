using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Common
{
    public class OverwatchApiModel
    {
        public OverwatchPlayer Player { get; set; }

        public class OverwatchResponse
        {
            [JsonProperty("_request")]
            public object Request { get; set; }
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
            public int? Comprank { get; set; }
            public int Level { get; set; }
            public int Prestige { get; set; }
            public string Avatar { get; set; }
            public int Wins { get; set; }
            public int Losses { get; set; }
            public int Games { get; set; }
            [JsonProperty("rank_image")]
            public string RankImage { get; set; }
        }

        public class GameStats
        {
            [JsonProperty("time_played")]
            public float TimePlayed { get; set; }
        }


        public class Competitive
        {

        }
    }
}