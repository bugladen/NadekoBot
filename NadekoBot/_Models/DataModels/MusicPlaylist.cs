namespace NadekoBot.DataModels
{
    internal class MusicPlaylist : IDataModel
    {
        public string Name { get; set; }
        public long CreatorId { get; set; }
        public string CreatorName { get; set; }
    }
}
