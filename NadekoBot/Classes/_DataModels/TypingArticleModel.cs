using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes._DataModels {
    class TypingArticle : IDataModel {
        public string Text { get; set; }
        [JsonProperty("createdAt")]
        public DateTime DateAdded { get; set; }
    }
}
