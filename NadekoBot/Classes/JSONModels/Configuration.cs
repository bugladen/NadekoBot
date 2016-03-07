using System;
using System.Collections.Generic;

namespace NadekoBot.Classes.JSONModels {
    public class Configuration {
        public bool DontJoinServers { get; set; } = false;
        public bool ForwardMessages { get; set; } = true;
        public bool IsRotatingStatus { get; set; } = false;
        public bool SendPrivateMessageOnMention { get; set; } = false;
        public List<string> RotatingStatuses { get; set; } = new List<string>();
        public HashSet<ulong> ServerBlacklist { get; set; } = new HashSet<ulong>();
        public HashSet<ulong> ChannelBlacklist { get; set; } = new HashSet<ulong>();
        public HashSet<ulong> UserBlacklist { get; set; } = new HashSet<ulong>() {
            105309315895693312,
            119174277298782216,
            143515953525817344
        };

        public string[] CryResponses { get; } = {
            "http://i.imgur.com/Xg3i1Qy.gif",
            "http://i.imgur.com/3K8DRrU.gif",
            "http://i.imgur.com/k58BcAv.gif",
            "http://i.imgur.com/I2fLXwo.gif"
        };

        public string[] PatResponses { get; } = {
            "http://i.imgur.com/IiQwK12.gif",
            "http://i.imgur.com/JCXj8yD.gif",
            "http://i.imgur.com/qqBl2bm.gif",
            "http://i.imgur.com/eOJlnwP.gif",
            "https://45.media.tumblr.com/229ec0458891c4dcd847545c81e760a5/tumblr_mpfy232F4j1rxrpjzo1_r2_500.gif",
            "https://media.giphy.com/media/KZQlfylo73AMU/giphy.gif",
            "https://media.giphy.com/media/12hvLuZ7uzvCvK/giphy.gif",
            "http://gallery1.anivide.com/_full/65030_1382582341.gif",
            "https://49.media.tumblr.com/8e8a099c4eba22abd3ec0f70fd087cce/tumblr_nxovj9oY861ur1mffo1_500.gif ",
        };
    }
}
