using NadekoBot.DataModels;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NadekoBot.Classes
{
    internal class DbHandler
    {
        public static DbHandler Instance { get; } = new DbHandler();

        private string FilePath { get; } = "data/nadekobot.sqlite";

        public SQLiteAsyncConnection Connection { get; set; }

        static DbHandler() { }
        public DbHandler()
        {
            DbHandlerAsync().GetAwaiter().GetResult();
        }

        private async Task DbHandlerAsync()
        {
            Connection = new SQLiteAsyncConnection(FilePath);
            await Connection.CreateTablesAsync<Stats, Command, Announcement, Request, TypingArticle>();
            await Connection.CreateTablesAsync<CurrencyState, CurrencyTransaction, Donator, UserPokeTypes, UserQuote>();
            await Connection.CreateTablesAsync<Reminder, SongInfo, PlaylistSongInfo, MusicPlaylist, Incident>();
            await Connection.ExecuteAsync(Queries.TransactionTriggerQuery);
            try
            {
                await Connection.ExecuteAsync(Queries.DeletePlaylistTriggerQuery);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal async Task DeleteWhere<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            var item = await Connection.Table<T>().Where(p).FirstOrDefaultAsync();
            if (item != null)
                await Connection.DeleteAsync(item);
        }

        internal async Task<HashSet<T>> GetAllRows<T>() where T : IDataModel, new() => new HashSet<T>(await Connection.Table<T>().ToListAsync());

        internal Task<CurrencyState> GetStateByUserId(long id) => Connection.Table<CurrencyState>().Where(x => x.UserId == id).FirstOrDefaultAsync();

        internal async Task<T> Delete<T>(int id) where T : IDataModel, new()
        {
            var found = await Connection.FindAsync<T>(id);
            if (found != null)
                await Connection.DeleteAsync(found);
            return found;
        }

        /// <summary>
        /// Updates an existing object or creates a new one
        /// </summary>
        internal Task<int> Save<T>(T o) where T : IDataModel, new() => Connection.InsertOrReplaceAsync(o);

        /// <summary>
        /// Updates an existing object or creates a new one
        /// </summary>
        internal async Task SaveAll<T>(IEnumerable<T> ocol) where T : IDataModel, new()
        {
            foreach (var o in ocol)
                await Connection.InsertOrReplaceAsync(o);
        }

        internal async Task<T> GetRandom<T>(Expression<Func<T, bool>> p) where T : IDataModel, new()
        {
            var r = new Random();
            return (await Connection.Table<T>().Where(p).ToListAsync()).OrderBy(x => r.Next()).FirstOrDefault();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num">Page number (0+)</param>
        /// <returns></returns>
        internal Task<List<PlaylistData>> GetPlaylistData(int num) => Connection.QueryAsync<PlaylistData>(
@"SELECT mp.Name as 'Name',mp.Id as 'Id', mp.CreatorName as 'Creator', Count(*) as 'SongCnt' FROM MusicPlaylist as mp
INNER JOIN PlaylistSongInfo as psi
ON mp.Id = psi.PlaylistId
Group BY mp.Name
Order By mp.DateAdded desc
Limit 20 OFFSET ?", num * 20);

        internal async Task<IEnumerable<CurrencyState>> GetTopRichest(int n = 10) => (await Connection.Table<CurrencyState>().OrderByDescending(cs => cs.Value).Take(n).ToListAsync());
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
