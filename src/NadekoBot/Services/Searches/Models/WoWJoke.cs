namespace NadekoBot.Services.Searches
{
    public class WoWJoke
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public override string ToString() => $"`{Question}`\n\n**{Answer}**";
    }
}
