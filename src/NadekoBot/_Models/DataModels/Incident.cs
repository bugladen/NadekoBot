namespace NadekoBot.DataModels
{
    class Incident : IDataModel
    {
        public long ServerId { get; set; }
        public long ChannelId { get; set; }
        public string Text { get; set; }
        public bool Read { get; set; } = false;
    }
}
