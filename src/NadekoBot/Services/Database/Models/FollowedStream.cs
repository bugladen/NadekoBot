namespace NadekoBot.Services.Database.Models
{
    public class FollowedStream : DbEntity
    {
        public ulong ChannelId { get; set; }
        public string Username { get; set; }
        public FollowedStreamType Type { get; set; }
        public ulong GuildId { get; set; }

        public enum FollowedStreamType
        {
            Twitch, Hitbox, Beam
        }
    }
}
