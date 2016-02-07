using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes._DataModels {
    class Command : IDataModel {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public long ServerId { get; set; }
        public string ServerName { get; set; }
        public long ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string CommandName { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
