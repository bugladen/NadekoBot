using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
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
