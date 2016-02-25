using System.Collections.Generic;
using System.Linq;
using SQLite;
using NadekoBot.Classes._DataModels;
using System;
using System.Linq.Expressions;

namespace NadekoBot.Classes {
    class DBHandler {
        private static readonly DBHandler _instance = new DBHandler();
        public static DBHandler Instance => _instance;

        private string _filePath { get; } = "data/nadekobot.sqlite";

        static DBHandler() { }
        public DBHandler() {
            using (var _conn = new SQLiteConnection(_filePath)) {
                _conn.CreateTable<Stats>();
                _conn.CreateTable<Command>();
                _conn.CreateTable<Announcement>();
                _conn.CreateTable<Request>();
                _conn.CreateTable<TypingArticle>();
                _conn.CreateTable<CurrencyState>();
                _conn.CreateTable<CurrencyTransaction>();
                _conn.CreateTable<Donator>();
                _conn.CreateTable<UserQuote>();
                _conn.Execute(Queries.TransactionTriggerQuery);
            }
        }

        internal void InsertData<T>(T o) where T : IDataModel {
            using (var _conn = new SQLiteConnection(_filePath)) {
                _conn.Insert(o, typeof(T));
            }
        }

        internal void InsertMany<T>(T objects) where T : IEnumerable<IDataModel> {
            using (var _conn = new SQLiteConnection(_filePath)) {
                _conn.InsertAll(objects);
            }
        }

        internal void UpdateData<T>(T o) where T : IDataModel {
            using (var _conn = new SQLiteConnection(_filePath)) {
                _conn.Update(o, typeof(T));
            }
        }

        internal List<T> GetAllRows<T>() where T : IDataModel, new() {
            using (var _conn = new SQLiteConnection(_filePath)) {
                return _conn.Table<T>().ToList();
            }
        }

        internal CurrencyState GetStateByUserId(long Id) {
            using (var _conn = new SQLiteConnection(_filePath)) {
                return _conn.Table<CurrencyState>().Where(x => x.UserId == Id).FirstOrDefault();
            }
        }

        internal T Delete<T>(int Id) where T : IDataModel, new() {
            using (var _conn = new SQLiteConnection(_filePath)) {
                var found = _conn.Find<T>(Id);
                if (found != null)
                    _conn.Delete<T>(found.Id);
                return found;
            }
        }

        /// <summary>
        /// Updates an existing object or creates a new one
        /// </summary>
        internal void Save<T>(T o) where T : IDataModel, new() {
            using (var _conn = new SQLiteConnection(_filePath)) {
                var found = _conn.Find<T>(o.Id);
                if (found == null)
                    _conn.Insert(o, typeof(T));
                else
                    _conn.Update(o, typeof(T));
            }
        }

        internal T GetRandom<T>(Expression<Func<T, bool>> p) where T : IDataModel, new() {
            using (var _conn = new SQLiteConnection(_filePath)) {
                var r = new Random();
                return _conn.Table<T>().Where(p).ToList().OrderBy(x => r.Next()).FirstOrDefault();
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
