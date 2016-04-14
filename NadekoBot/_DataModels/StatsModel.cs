using System;

namespace NadekoBot._DataModels {
    internal class Stats : IDataModel {
        public int ConnectedServers { get; set; }
        public int OnlineUsers { get; set; }
        public TimeSpan Uptime { get; set; }
        public int RealOnlineUsers { get; set; }
    }
}
