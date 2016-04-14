namespace NadekoBot.DataModels {
    internal class Command : IDataModel {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public long ServerId { get; set; }
        public string ServerName { get; set; }
        public long ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string CommandName { get; set; }
    }
}
