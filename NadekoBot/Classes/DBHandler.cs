using System.Collections.Generic;
using System.Linq;
using SQLite;
using NadekoBot.Classes._DataModels;
using System;
using System.Linq.Expressions;

namespace NadekoBot.Classes {
    internal class DbHandler {
        public static DbHandler Instance { get; } = new DbHandler();

        private string FilePath { get; } = "data/nadekobot.sqlite";

        static DbHandler() { }
        public DbHandler() {
            using (var conn = new SQLiteConnection(FilePath)) {
                conn.CreateTable<Stats>();
                conn.CreateTable<Command>();
                conn.CreateTable<Announcement>();
                conn.CreateTable<Request>();
                conn.CreateTable<TypingArticle>();
                conn.CreateTable<CurrencyState>();
                conn.CreateTable<CurrencyTransaction>();
                conn.CreateTable<Donator>();
                conn.CreateTable<UserQuote>();
                conn.Execute(Queries.TransactionTriggerQuery);
            }
        }

        internal void InsertData<T>(T o) where T : IDataModel {
            using (var conn = new SQLiteConnection(FilePath)) {
                conn.Insert(o, typeof(T));
            }
        }

        internal void InsertMany<T>(T objects) where T : IEnumerable<IDataModel> {
            using (var conn = new SQLiteConnection(FilePath)) {
                conn.InsertAll(objects);
            }
        }

        internal void UpdateData<T>(T o) where T : IDataModel {
            using (var conn = new SQLiteConnection(FilePath)) {
                conn.Update(o, typeof(T));
            }
        }

        internal HashSet<T> GetAllRows<T>() where T : IDataModel, new() {
            using (var conn = new SQLiteConnection(FilePath)) {
                return new HashSet<T>(conn.Table<T>());
            }
        }

        internal CurrencyState GetStateByUserId(long id) {
            using (var conn = new SQLiteConnection(FilePath)) {
                return conn.Table<CurrencyState>().Where(x => x.UserId == id).FirstOrDefault();
            }
        }

        internal T Delete<T>(int id) where T : IDataModel, new() {
            using (var conn = new SQLiteConnection(FilePath)) {
                var found = conn.Find<T>(id);
                if (found != null)
                    conn.Delete<T>(found.Id);
                return found;
            }
        }

        /// <summary>
        /// Updates an existing object or creates a new one
        /// </summary>
        internal void Save<T>(T o) where T : IDataModel, new() {
            using (var conn = new SQLiteConnection(FilePath)) {
                var found = conn.Find<T>(o.Id);
                if (found == null)
                    conn.Insert(o, typeof(T));
                else
                    conn.Update(o, typeof(T));
            }
        }

        internal T GetRandom<T>(Expression<Func<T, bool>> p) where T : IDataModel, new() {
            using (var conn = new SQLiteConnection(FilePath)) {
                var r = new Random();
                return conn.Table<T>().Where(p).ToList().OrderBy(x => r.Next()).FirstOrDefault();
            }
        }
    }
}

public static class Queries {
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
