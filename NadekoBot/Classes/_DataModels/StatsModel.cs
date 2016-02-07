using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes._DataModels {
    class Stats : IDataModel {
        public int ConnectedServers { get; set; }
        public int OnlineUsers { get; set; }
        public TimeSpan Uptime { get; set; }
        public int RealOnlineUsers { get; set; }
        [Newtonsoft.Json.JsonProperty("createdAt")]
        public DateTime DateAdded { get; set; }
    }
}
