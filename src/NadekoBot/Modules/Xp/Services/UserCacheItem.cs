namespace NadekoBot.Modules.Xp.Services
{
    public class UserCacheItem
    {
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }

        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is UserCacheItem uci && uci.UserId == UserId;
        }
    }
}
