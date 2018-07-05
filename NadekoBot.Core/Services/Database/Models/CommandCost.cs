using System;

namespace NadekoBot.Core.Services.Database.Models
{
    public class CommandCost : DbEntity
    {
        public int Cost { get; set; }
        public string CommandName { get; set; }

        public override int GetHashCode() =>
            CommandName.GetHashCode(StringComparison.InvariantCulture);

        public override bool Equals(object obj)
        {
            return obj is CommandCost cc
                ? cc.CommandName == CommandName
                : false;
        }
    }
}
