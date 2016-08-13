namespace NadekoBot.DataModels {
    internal class Request : IDataModel {
        public string UserName { get; set; }
        public long UserId { get; set; }
        public string ServerName { get; set; }
        public long ServerId { get; set; }
        [Newtonsoft.Json.JsonProperty("Request")]
        public string RequestText { get; set; }
    }
}
