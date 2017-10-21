namespace NadekoBot.Core.Services.Database.Models
{
    public class Currency : DbEntity
    {
        public ulong UserId { get; set; }
        public long Amount { get; set; }
    }
}
