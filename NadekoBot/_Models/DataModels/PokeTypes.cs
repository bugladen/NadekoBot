using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.DataModels
{
    class UserPokeTypes : IDataModel
    {
        public long UserId { get; set; }
        public string type { get; set; }
    }
}
