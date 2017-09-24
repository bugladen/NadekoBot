using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Common
{
    public class HitboxResponse
    {
        public bool Success { get; set; } = true;
        [JsonProperty("media_is_live")]
        public string MediaIsLive { get; set; }
        public bool IsLive => MediaIsLive == "1";
        [JsonProperty("media_views")]
        public string Views { get; set; }
    }

    public class TwitchResponse
    {
        public string Error { get; set; } = null;
        public bool IsLive => Stream != null;
        public StreamInfo Stream { get; set; }

        public class StreamInfo
        {
            public int Viewers { get; set; }
        }
    }

    public class BeamResponse
    {
        public string Error { get; set; } = null;

        [JsonProperty("online")]
        public bool IsLive { get; set; }
        public int ViewersCurrent { get; set; }
    }
}
