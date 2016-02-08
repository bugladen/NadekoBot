namespace NadekoBot.Classes._DataModels {
    class CurrencyTransaction : IDataModel {
        public string Reason { get; set; }
        public int Value { get; set; }
    }
}
