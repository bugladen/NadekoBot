namespace NadekoBot.DataModels {
    internal class Donator : IDataModel {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public int Amount { get; set; }
    }
}
