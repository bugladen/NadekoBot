namespace NadekoBot.Core.Services.Database.Models
{
    public class CommandCost : DbEntity
    {
        public int Cost { get; set; }
        public string CommandName { get; set; }

        public override int GetHashCode() =>
            CommandName.GetHashCode();

        public override bool Equals(object obj)
        {
            var instance = obj as CommandCost;

            if (instance == null)
                return false;

            return instance.CommandName == CommandName;
        }
    }
}
