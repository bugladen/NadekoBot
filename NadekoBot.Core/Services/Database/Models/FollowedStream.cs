namespace NadekoBot.Core.Services.Database.Models
{
    public class FollowedStream : DbEntity
    {
        public ulong ChannelId { get; set; }
        public string Username { get; set; }
        public FType Type { get; set; }
        public ulong GuildId { get; set; }
        public string Message { get; set; }

        public enum FType
        {
            Twitch, Smashcast, Mixer,
            Picarto
        }

        public override int GetHashCode() => 
            ChannelId.GetHashCode() ^ 
            Username.ToUpperInvariant().GetHashCode(System.StringComparison.InvariantCulture) ^ 
            Type.GetHashCode();

        public override bool Equals(object obj)
        {
            if (!(obj is FollowedStream fs))
                return false;

            return fs.ChannelId == ChannelId && 
                   fs.Username.ToUpperInvariant().Trim() == Username.ToUpperInvariant().Trim() &&
                   fs.Type == Type;
        }
    }
}
