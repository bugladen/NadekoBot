namespace NadekoBot.Core.Services.Database.Models
{
    public class CommandPrice : DbEntity
    {
        public int Price { get; set; }
        //this is unique
        public string CommandName { get; set; }
    }
}
