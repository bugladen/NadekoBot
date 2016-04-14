namespace NadekoBot._DataModels {
    internal class UserQuote : IDataModel {
        public string UserName { get; set; }
        public string Keyword { get; set; }
        public string Text { get; set; }
    }
}
