using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes._DataModels {
    class IDataModel {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public IDataModel() { }
    }
}
