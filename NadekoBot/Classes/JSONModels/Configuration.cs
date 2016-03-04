using System;
using System.Collections.Generic;

namespace NadekoBot.Classes.JSONModels {
    public class Configuration {
        public bool DontJoinServers = false;
        public bool ForwardMessages = true;
        public HashSet<ulong> ServerBlacklist = new HashSet<ulong>();
        public HashSet<ulong> ChannelBlacklist = new HashSet<ulong>();
        public HashSet<ulong> UserBlacklist = new HashSet<ulong>() {
            105309315895693312,
            119174277298782216,
            143515953525817344
        };
    }
}
