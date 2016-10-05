using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration.Commands.Migration
{
    public class CommandPrefixes0_9
    {
        public string Administration { get; set; }
        public string Searches { get; set; }
        public string NSFW { get; set; }
        public string Conversations { get; set; }
        public string ClashOfClans { get; set; }
        public string Help { get; set; }
        public string Music { get; set; }
        public string Trello { get; set; }
        public string Games { get; set; }
        public string Gambling { get; set; }
        public string Permissions { get; set; }
        public string Programming { get; set; }
        public string Pokemon { get; set; }
        public string Utility { get; set; }
    }

    public class Config0_9
    {
        public bool DontJoinServers { get; set; }
        public bool ForwardMessages { get; set; }
        public bool ForwardToAllOwners { get; set; }
        public bool IsRotatingStatus { get; set; }
        public int BufferSize { get; set; }
        public List<string> RaceAnimals { get; set; }
        public string RemindMessageFormat { get; set; }
        public Dictionary<string, List<string>> CustomReactions { get; set; }
        public List<string> RotatingStatuses { get; set; }
        public CommandPrefixes0_9 CommandPrefixes { get; set; }
        public List<ulong> ServerBlacklist { get; set; }
        public List<ulong> ChannelBlacklist { get; set; }
        public List<ulong> UserBlacklist { get; set; }
        public List<string> _8BallResponses { get; set; }
        public string CurrencySign { get; set; }
        public string CurrencyName { get; set; }
        public string DMHelpString { get; set; }
        public string HelpString { get; set; }
    }
}