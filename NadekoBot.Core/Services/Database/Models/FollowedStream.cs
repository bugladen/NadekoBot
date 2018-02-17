namespace NadekoBot.Core.Services.Database.Models
{
    public class FollowedStream : DbEntity
    {
        public ulong ChannelId { get; set; }
        public string Username { get; set; }
        public FType Type { get; set; }
        public ulong GuildId { get; set; }

        public enum FType
        {
            Twitch, Smashcast, Mixer,
            Picarto
        }

        public override int GetHashCode() => 
            ChannelId.GetHashCode() ^ 
            Username.ToLowerInvariant().GetHashCode() ^ 
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
