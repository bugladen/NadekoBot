using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Common
{
    public interface IStreamResponse
    {
        int Viewers { get; }
        string Title { get; }
        bool Live { get; }
        string Game { get; }
        int FollowerCount { get; }
        string Url { get; }
        string Icon { get; }
    }

    public class SmashcastResponse : IStreamResponse
    {
        public bool Success { get; set; } = true;
        public int Followers { get; set; }
        [JsonProperty("user_logo")]
        public string UserLogo { get; set; }
        [JsonProperty("is_live")]
        public string IsLive { get; set; }

        public int Viewers => 0;
        public string Title => "";
        public bool Live => IsLive == "1";
        public string Game => "";
        public int FollowerCount => Followers;
        public string Icon => !string.IsNullOrWhiteSpace(UserLogo)
            ? "https://edge.sf.hitbox.tv" + UserLogo
            : "";

        public string Url { get; set; }
    }

    public class TwitchResponse : IStreamResponse
    {
        public string Error { get; set; } = null;
        public bool IsLive => Stream != null;
        public StreamInfo Stream { get; set; }

        public class StreamInfo
        {
            public int Viewers { get; set; }
            public string Game { get; set; }
            public ChannelInfo Channel { get; set; }

            public class ChannelInfo
            {
                public string Status { get; set; }
                public string Logo { get; set; }
                public int Followers { get; set; }
            }
        }

        public int Viewers => Stream?.Viewers ?? 0;
        public string Title => Stream?.Channel?.Status;
        public bool Live => IsLive;
        public string Game => Stream?.Game;
        public int FollowerCount => Stream?.Channel?.Followers ?? 0;
        public string Url { get; set; }
        public string Icon => Stream?.Channel?.Logo;
    }

    public class MixerResponse : IStreamResponse
    {
        public class MixerType
        {
            public string Parent { get; set; }
            public string Name { get; set; }
        }
        public class MixerThumbnail
        {
            public string Url { get; set; }
        }
        public string Url { get; set; }
        public string Error { get; set; } = null;

        [JsonProperty("online")]
        public bool IsLive { get; set; }
        public int ViewersCurrent { get; set; }
        public string Name { get; set; }
        public int NumFollowers { get; set; }
        public MixerType Type { get; set; }
        public MixerThumbnail Thumbnail { get; set; }

        public int Viewers => ViewersCurrent;
        public string Title => Name;
        public bool Live => IsLive;
        public string Game => Type?.Name ?? "";
        public int FollowerCount => NumFollowers;
        public string Icon => Thumbnail?.Url;
    }
}
