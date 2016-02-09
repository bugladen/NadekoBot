using SQLite;
using System;

namespace NadekoBot.Classes._DataModels {
    abstract class IDataModel {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Newtonsoft.Json.JsonProperty("createdAt")]
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public IDataModel() { }
    }
}
