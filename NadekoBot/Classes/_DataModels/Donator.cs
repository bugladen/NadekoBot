using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes._DataModels {
    class Donator : IDataModel {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public int Amount { get; set; }
    }
}
