using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes._DataModels {
    class UserQuote : IDataModel {
        public string UserName { get; set; }
        public string Keyword { get; set; }
        public string Text { get; set; }
    }
}
