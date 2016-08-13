namespace NadekoBot.DataModels {
    internal class CurrencyTransaction : IDataModel {
        public string Reason { get; set; }
        public int Value { get; set; }
        public long UserId { get; set; }
    }
}
