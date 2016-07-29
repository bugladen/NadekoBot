using NadekoBot.DataModels;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NadekoBot.Classes
{
    internal class DbHandler
    {
        public static DbHandler Instance { get; } = new DbHandler();

        private string FilePath { get; } = "data/nadekobot.sqlite";

        public SQLiteConnection Connection { get; set; }

        static DbHandler() { }
        public DbHandler()
        {
            Connection = new SQLiteConnection(FilePath);
            Connection.CreateTable<Stats>();
            Connection.CreateTable<Command>();
            Connection.CreateTable<Announcement>();
            Connection.CreateTable<Request>();
            Connection.CreateTable<TypingArticle>();
            Connection.CreateTable<CurrencyState>();
            Connection.CreateTable<CurrencyTransaction>();
            Connection.CreateTable<Donator>();
            Connection.CreateTable<UserPokeTypes>();
            Connection.CreateTable<UserQuote>();
            Connection.CreateTable<Reminder>();
            Connection.CreateTable<SongInfo>();
            Connection.CreateTable<PlaylistSongInfo>();
            Connection.CreateTable<MusicPlaylist>();
            Connection.CreateTable<Incident>();
            Connection.Execute(Queries.TransactionTriggerQuery);
            try
            {
                Connection.Execute(Queries.DeletePlaylistTriggerQuery);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal T FindOne<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            return Connection.Table<T>().Where(p).FirstOrDefault();

        }

        internal IList<T> FindAll<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {

            return Connection.Table<T>().Where(p).ToList();

        }

        internal void DeleteWhere<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            var id = Connection.Table<T>().Where(p).FirstOrDefault()?.Id;
            if (id.HasValue)
                Connection.Delete<T>(id);
        }

        internal HashSet<T> GetAllRows<T>() where T : IDataModel, new()
        {
            return new HashSet<T>(Connection.Table<T>());
        }

        internal CurrencyState GetStateByUserId(long id)
        {
            return Connection.Table<CurrencyState>().Where(x => x.UserId == id).FirstOrDefault();
        }

        internal T Delete<T>(int id) where T : IDataModel, new()
        {
            var found = Connection.Find<T>(id);
            if (found != null)
                Connection.Delete<T>(found.Id);
            return found;
        }

        /// <summary>
        /// Updates an existing object or creates a new one
        /// </summary>
        internal void Save<T>(T o) where T : IDataModel, new()
        {
            var found = Connection.Find<T>(o.Id);
            if (found == null)
                Connection.Insert(o, typeof(T));
            else
                Connection.Update(o, typeof(T));
        }

        /// <summary>
        /// Updates an existing object or creates a new one
        /// </summary>
        internal void SaveAll<T>(IEnumerable<T> ocol) where T : IDataModel, new()
        {
            foreach (var o in ocol)
                Connection.InsertOrReplace(o);
        }

        internal T GetRandom<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            var r = new Random();
            return Connection.Table<T>().Where(p).ToList().OrderBy(x => r.Next()).FirstOrDefault();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num">Page number (0+)</param>
        /// <returns></returns>
        internal List<PlaylistData> GetPlaylistData(int num)
        {
            return Connection.Query<PlaylistData>(
@"SELECT mp.Name as 'Name',mp.Id as 'Id', mp.CreatorName as 'Creator', Count(*) as 'SongCnt' FROM MusicPlaylist as mp
INNER JOIN PlaylistSongInfo as psi
ON mp.Id = psi.PlaylistId
Group BY mp.Name
Order By mp.DateAdded desc
Limit 20 OFFSET ?", num * 20);

        }

        internal IEnumerable<CurrencyState> GetTopRichest(int n = 10)
        {
            return Connection.Table<CurrencyState>().OrderByDescending(cs => cs.Value).Take(n).ToList();
        }
    }
}

public class PlaylistData
{
    public string Name { get; set; }
    public int Id { get; set; }
    public string Creator { get; set; }
    public int SongCnt { get; set; }
}

public static class Queries
{
    public const string TransactionTriggerQuery = @"
CREATE TRIGGER IF NOT EXISTS OnTransactionAdded
AFTER INSERT ON CurrencyTransaction
BEGIN
INSERT OR REPLACE INTO CurrencyState (Id, UserId, Value, DateAdded) 
    VALUES (COALESCE((SELECT Id from CurrencyState where UserId = NEW.UserId),(SELECT COALESCE(MAX(Id),0)+1 from CurrencyState)),
            NEW.UserId, 
            COALESCE((SELECT Value+New.Value FROM CurrencyState Where UserId = NEW.UserId),NEW.Value),  
            NEW.DateAdded);
END
";
    public const string DeletePlaylistTriggerQuery = @"
CREATE TRIGGER IF NOT EXISTS music_playlist
AFTER DELETE ON MusicPlaylist
FOR EACH ROW
BEGIN
    DELETE FROM PlaylistSongInfo WHERE PlaylistId = OLD.Id;
END";
}
