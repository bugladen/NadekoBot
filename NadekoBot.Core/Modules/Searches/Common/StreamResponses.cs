using NadekoBot.Core.Services.Database.Models;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Common
{
    public interface IStreamResponse
    {
        FollowedStream.FType StreamType { get; }
        string Name { get; }
        int Viewers { get; }
        string Title { get; }
        bool Live { get; }
        string Game { get; }
        int Followers { get; }
        string ApiUrl { get; set; }
        string Icon { get; }
        string Thumbnail { get; }
    }

    public class StreamResponse : IStreamResponse
    {
        public FollowedStream.FType StreamType { get; set; }
        public string Name { get; set; }
        public int Viewers { get; set; }
        public string Title { get; set; }
        public bool Live { get; set; }
        public string Game { get; set; }
        public int Followers { get; set; }
        public string ApiUrl { get; set; }
        public string Icon { get; set; }
        public string Thumbnail { get; set; }
    }

    public class SmashcastResponse : IStreamResponse
    {
        [JsonProperty("user_name")]
        public string Name { get; set; }
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
        public string Icon => !string.IsNullOrWhiteSpace(UserLogo)
            ? "https://edge.sf.hitbox.tv" + UserLogo
            : "";

        public string ApiUrl { get; set; }

        public FollowedStream.FType StreamType => FollowedStream.FType.Smashcast;

        public string Thumbnail => null;
    }

    public class PicartoResponse : IStreamResponse
    {
        public class PThumbnail
        {
            public string Web { get; set; }
        }

        public string Name { get; set; }
        public int Viewers { get; set; }

        public string Title { get; set; }

        [JsonProperty("online")]
        public bool Live { get; set; }
        [JsonProperty("category")]
        public string Game { get; set; }

        public int Followers { get; set; }

        public string ApiUrl { get; set; }
        [JsonProperty("thumbnail")]
        public string Icon { get; set; }

        public FollowedStream.FType StreamType => FollowedStream.FType.Picarto;

        public PThumbnail Thumbnails { get; set; }
        public string Thumbnail => Thumbnails?.Web;
    }

    public class TwitchResponse : IStreamResponse
    {
        public string Error { get; set; } = null;
        public bool IsLive => Stream != null;
        public StreamInfo Stream { get; set; }
        public string Name => Stream?.Channel.Name;

        public class StreamInfo
        {
            public int Viewers { get; set; }
            public string Game { get; set; }
            public ChannelInfo Channel { get; set; }

            public class ChannelInfo
            {
                public string Name { get; set; }
                public string Status { get; set; }
                public string Logo { get; set; }
                public int Followers { get; set; }
            }
        }

        public int Viewers => Stream?.Viewers ?? 0;
        public string Title => Stream?.Channel?.Status;
        public bool Live => IsLive;
        public string Game => Stream?.Game;
        public int Followers => Stream?.Channel?.Followers ?? 0;
        public string ApiUrl { get; set; }
        public string Icon => Stream?.Channel?.Logo;

        public FollowedStream.FType StreamType => FollowedStream.FType.Twitch;

        public string Thumbnail => null;
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
        public string ApiUrl { get; set; }
        public string Error { get; set; } = null;

        [JsonProperty("online")]
        public bool IsLive { get; set; }
        public int ViewersCurrent { get; set; }
        public string Name { get; set; }
        public int NumFollowers { get; set; }
        public MixerType Type { get; set; }
        [JsonProperty("thumbnail")]
        public MixerThumbnail IconData { get; set; }

        public int Viewers => ViewersCurrent;
        public string Title => Name;
        public bool Live => IsLive;
        public string Game => Type?.Name ?? "";
        public int Followers => NumFollowers;
        public string Icon => IconData?.Url;

        public FollowedStream.FType StreamType => FollowedStream.FType.Mixer;
        public string Thumbnail => null;
    }
}
