using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SQLite;
using NadekoBot.Classes._DataModels;

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
            }
        }

        internal void InsertData<T>(T o) where T : IDataModel {
            using (var _conn = new SQLiteConnection(_filePath)) {
                _conn.Insert(o, typeof(T));
            }
        }

        internal void UpdateData<T>(T o) where T : IDataModel {
            using (var _conn = new SQLiteConnection(_filePath)) {
                _conn.Update(o, typeof(T));
            }
        }

        internal List<T> GetAllRows<T>() where T : IDataModel, new() {
            using (var _conn = new SQLiteConnection(_filePath)) {
                return _conn.Table<T>().Where(t => true).ToList();
            }
        }

        internal T Delete<T>(int Id) where T : IDataModel, new() {
            using (var _conn = new SQLiteConnection(_filePath)) {
                var found = _conn.Table<T>().Where(t => t.Id == Id).FirstOrDefault();
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
                var found = _conn.Table<T>().Where(t => t.Id == o.Id).FirstOrDefault();
                if (found == null)
                    _conn.Insert(o, typeof(T));
                else
                    _conn.Update(o, typeof(T));
            }
        }
    }
}
