using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes._DataModels
{
    class PokeMoves : IDataModel
    {
        public string move { get; set; }
        public int type { get; set; }
        public long UserId { get; internal set; }
    }
}
