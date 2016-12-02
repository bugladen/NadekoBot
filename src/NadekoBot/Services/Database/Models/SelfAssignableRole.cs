namespace NadekoBot.Services.Database.Models
{
    public class SelfAssignedRole : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
    }
}
