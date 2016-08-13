using Newtonsoft.Json;

namespace NadekoBot.DataModels
{
    internal class Announcement : IDataModel
    {
        public long ServerId { get; set; } = 0;
        public bool Greet { get; set; } = false;
        public bool GreetPM { get; set; } = false;
        [JsonProperty("greetChannel")]
        public long GreetChannelId { get; set; } = 0;
        public string GreetText { get; set; } = "Welcome %user%!";
        public bool Bye { get; set; } = false;
        public bool ByePM { get; set; } = false;
        [JsonProperty("byeChannel")]
        public long ByeChannelId { get; set; } = 0;
        public string ByeText { get; set; } = "%user% has left the server.";
        public bool DeleteGreetMessages { get; set; } = true;
    }
}
