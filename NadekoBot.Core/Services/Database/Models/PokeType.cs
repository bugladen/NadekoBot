namespace NadekoBot.Core.Services.Database.Models
{
    public class UserPokeTypes : DbEntity
    {
        public ulong UserId { get; set; }
        public string type { get; set; }
    }
}
