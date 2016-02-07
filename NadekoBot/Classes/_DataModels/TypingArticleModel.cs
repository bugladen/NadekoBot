using System;

namespace NadekoBot.Classes._DataModels {
    class TypingArticle : IDataModel {
        public string Text { get; set; }
        [Newtonsoft.Json.JsonProperty("createdAt")]
        public DateTime DateAdded { get; set; }
    }
}
