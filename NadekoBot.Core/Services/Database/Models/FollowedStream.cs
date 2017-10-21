namespace NadekoBot.Core.Services.Database.Models
{
    public class FollowedStream : DbEntity
    {
        public ulong ChannelId { get; set; }
        public string Username { get; set; }
        public FollowedStreamType Type { get; set; }
        public ulong GuildId { get; set; }

        public enum FollowedStreamType
        {
            Twitch, Smashcast, Mixer
        }

        public override int GetHashCode() => 
            ChannelId.GetHashCode() ^ 
            Username.GetHashCode() ^ 
            Type.GetHashCode();

        public override bool Equals(object obj)
        {
            var fs = obj as FollowedStream;
            if (fs == null)
                return false;

            return fs.ChannelId == ChannelId && 
                   fs.Username.ToLowerInvariant().Trim() == Username.ToLowerInvariant().Trim() &&
                   fs.Type == Type;
        }
    }
}
