using SQLite;

namespace NadekoBot.DataModels
{
    internal class SongInfo : IDataModel
    {
        public string Provider { get; internal set; }
        public int ProviderType { get; internal set; }
        public string Title { get; internal set; }
        public string Uri { get; internal set; }
        [Unique]
        public string Query { get; internal set; }
    }
}
