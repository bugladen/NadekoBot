using SQLite;
using System.Collections.Generic;

namespace NadekoBot.Classes._DataModels
{
    internal class MusicPlaylist : IDataModel
    {
        [Unique]
        public string Name { get; set; }
        public long CreatorId { get; set; }
        public string CreatorName { get; set; }
        public List<SongInfo> Songs { get; set; }
    }

    [System.Serializable]
    internal class SongInfo
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public SongType Type { get; set; }
    }

    internal enum SongType
    {
        Local,
        Radio,
        Query
    }
}
