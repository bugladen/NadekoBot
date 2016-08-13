using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface IBotConfiguration
    {
        bool DontJoinServers { get; set; }
        bool ForwardMessages { get; set; }
        bool ForwardToAllOwners { get; set; }

        bool RotatePlayingStatus { get; set; }
        List<string> PlayingStatuses { get; set; }

        ulong BufferSize { get; set; }
        List<string> RaceAnimals { get; set; }
        string RemindMessageFormat { get; set; }


        HashSet<ulong> BlacklistedServers { get; set; }
        HashSet<ulong> BlacklistedChannels { get; set; }
        HashSet<ulong> BlacklistedUsers { get; set; }

        List<string> EightBallResponses { get; set; }       
        Currency Currency { get; set; }

        ModulePrefixes ModulePrefixes { get; set; }
    }

    public class Currency {
        public string Sign { get; set; }
        public string Name { get; set; }
        public string PluralName { get; set; }
    }

    public class ModulePrefixes
    {
        public string Administration { get; set; } = ".";
        public string Searches { get; set; } = "~";
        public string NSFW { get; set; } = "~";
        public string Conversations { get; set; } = "<@{0}>";
        public string ClashOfClans { get; set; } = ",";
        public string Help { get; set; } = "-";
        public string Music { get; set; } = "!!";
        public string Trello { get; set; } = "trello ";
        public string Games { get; set; } = ">";
        public string Gambling { get; set; } = "$";
        public string Permissions { get; set; } = ";";
        public string Programming { get; set; } = "%";
        public string Pokemon { get; set; } = ">";
        public string Utility { get; set; } = ".";
    }
}
