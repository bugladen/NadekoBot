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

        static DbHandler() { }
        public DbHandler()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                conn.CreateTable<Stats>();
                conn.CreateTable<Command>();
                conn.CreateTable<Announcement>();
                conn.CreateTable<Request>();
                conn.CreateTable<TypingArticle>();
                conn.CreateTable<CurrencyState>();
                conn.CreateTable<CurrencyTransaction>();
                conn.CreateTable<Donator>();
                conn.CreateTable<UserPokeTypes>();
                conn.CreateTable<UserQuote>();
                conn.CreateTable<Reminder>();
                conn.CreateTable<SongInfo>();
                conn.CreateTable<PlaylistSongInfo>();
                conn.CreateTable<MusicPlaylist>();
                conn.Execute(Queries.TransactionTriggerQuery);
            }
        }

        internal T FindOne<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                return conn.Table<T>().Where(p).FirstOrDefault();
            }
        }

        internal IList<T> FindAll<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                return conn.Table<T>().Where(p).ToList();
            }
        }

        internal void DeleteAll<T>() where T : IDataModel
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                conn.DeleteAll<T>();
            }
        }

        internal void DeleteWhere<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                var id = conn.Table<T>().Where(p).FirstOrDefault()?.Id;
                if (id.HasValue)
                    conn.Delete<T>(id);
            }
        }

        internal void InsertData<T>(T o) where T : IDataModel
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                conn.Insert(o, typeof(T));
            }
        }

        internal void InsertMany<T>(T objects) where T : IEnumerable<IDataModel>
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                conn.InsertAll(objects);
            }
        }

        internal void UpdateData<T>(T o) where T : IDataModel
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                conn.Update(o, typeof(T));
            }
        }

        internal HashSet<T> GetAllRows<T>() where T : IDataModel, new()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                return new HashSet<T>(conn.Table<T>());
            }
        }

        internal CurrencyState GetStateByUserId(long id)
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                return conn.Table<CurrencyState>().Where(x => x.UserId == id).FirstOrDefault();
            }
        }

        internal T Delete<T>(int id) where T : IDataModel, new()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                var found = conn.Find<T>(id);
                if (found != null)
                    conn.Delete<T>(found.Id);
                return found;
            }
        }

        /// <summary>
        /// Updates an existing object or creates a new one
        /// </summary>
        internal void Save<T>(T o) where T : IDataModel, new()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                var found = conn.Find<T>(o.Id);
                if (found == null)
                    conn.Insert(o, typeof(T));
                else
                    conn.Update(o, typeof(T));
            }
        }

        /// <summary>
        /// Updates an existing object or creates a new one
        /// </summary>
        internal void SaveAll<T>(IEnumerable<T> ocol) where T : IDataModel, new()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                foreach (var o in ocol)
                {
                    conn.InsertOrReplace(o, typeof(T));
                }
            }
        }

        internal T GetRandom<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                var r = new Random();
                return conn.Table<T>().Where(p).ToList().OrderBy(x => r.Next()).FirstOrDefault();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num">Page number (0+)</param>
        /// <returns></returns>
        internal List<PlaylistData> GetPlaylistData(int num)
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                return conn.Query<PlaylistData>(
@"SELECT mp.Name as 'Name',mp.Id as 'Id', mp.CreatorName as 'Creator', Count(*) as 'SongCnt' FROM MusicPlaylist as mp
INNER JOIN PlaylistSongInfo as psi
ON mp.Id = psi.PlaylistId
Group BY mp.Name
Order By mp.DateAdded desc
Limit 20 OFFSET ?", num * 20);
            }
        }

        internal IEnumerable<CurrencyState> GetTopRichest(int n = 10)
        {
            using (var conn = new SQLiteConnection(FilePath))
            {
                return conn.Table<CurrencyState>().Take(n).ToList().OrderBy(cs => -cs.Value);
            }
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
    public static string TransactionTriggerQuery = @"
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
}
