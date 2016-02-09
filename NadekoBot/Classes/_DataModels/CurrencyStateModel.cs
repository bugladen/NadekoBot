namespace NadekoBot.Classes._DataModels {
    class CurrencyState : IDataModel {
        public long Value { get; set; }
        [SQLite.Unique]
        public long UserId { get; set; }
    }
}
