namespace NadekoBot.Core.Services.Database.Models
{
    public class Donator : DbEntity
    {
        public ulong UserId { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; } = 0;
    }
}
