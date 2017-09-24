using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Modules.Administration.Common.Migration
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

    /// <summary>
    /// Holds a permission list
    /// </summary>
    public class Permissions
    {
        /// <summary>
        /// Name of the parent object whose permissions these are
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Module name with allowed/disallowed
        /// </summary>
        public ConcurrentDictionary<string, bool> Modules { get; set; }
        /// <summary>
        /// Command name with allowed/disallowed
        /// </summary>
        public ConcurrentDictionary<string, bool> Commands { get; set; }
        /// <summary>
        /// Should the bot filter invites to other discord servers (and ref links in the future)
        /// </summary>
        public bool FilterInvites { get; set; }
        /// <summary>
        /// Should the bot filter words which are specified in the Words hashset
        /// </summary>
        public bool FilterWords { get; set; }

        public Permissions(string name)
        {
            Name = name;
            Modules = new ConcurrentDictionary<string, bool>();
            Commands = new ConcurrentDictionary<string, bool>();
            FilterInvites = false;
            FilterWords = false;
        }

        public void CopyFrom(Permissions other)
        {
            Modules.Clear();
            foreach (var mp in other.Modules)
                Modules.AddOrUpdate(mp.Key, mp.Value, (s, b) => mp.Value);
            Commands.Clear();
            foreach (var cp in other.Commands)
                Commands.AddOrUpdate(cp.Key, cp.Value, (s, b) => cp.Value);
            FilterInvites = other.FilterInvites;
            FilterWords = other.FilterWords;
        }

        public override string ToString()
        {
            var toReturn = "";
            var bannedModules = Modules.Where(kvp => kvp.Value == false);
            var bannedModulesArray = bannedModules as KeyValuePair<string, bool>[] ?? bannedModules.ToArray();
            if (bannedModulesArray.Any())
            {
                toReturn += "`Banned Modules:`\n";
                toReturn = bannedModulesArray.Aggregate(toReturn, (current, m) => current + $"\t`[x]  {m.Key}`\n");
            }
            var bannedCommands = Commands.Where(kvp => kvp.Value == false);
            var bannedCommandsArr = bannedCommands as KeyValuePair<string, bool>[] ?? bannedCommands.ToArray();
            if (bannedCommandsArr.Any())
            {
                toReturn += "`Banned Commands:`\n";
                toReturn = bannedCommandsArr.Aggregate(toReturn, (current, c) => current + $"\t`[x]  {c.Key}`\n");
            }
            return toReturn;
        }
    }

    public class ServerPermissions0_9
    {
        /// <summary>
        /// The guy who can edit the permissions
        /// </summary>
        public string PermissionsControllerRole { get; set; }
        /// <summary>
        /// Does it print the error when a restriction occurs
        /// </summary>
        public bool Verbose { get; set; }
        /// <summary>
        /// The id of the thing (user/server/channel)
        /// </summary>
        public ulong Id { get; set; } //a string because of the role name.
        /// <summary>
        /// Permission object bound to the id of something/role name
        /// </summary>
        public Permissions Permissions { get; set; }
        /// <summary>
        /// Banned words, usually profanities, like word "java"
        /// </summary>
        public HashSet<string> Words { get; set; }

        public Dictionary<ulong, Permissions> UserPermissions { get; set; }
        public Dictionary<ulong, Permissions> ChannelPermissions { get; set; }
        public Dictionary<ulong, Permissions> RolePermissions { get; set; }
        /// <summary>
        /// Dictionary of command names with their respective cooldowns
        /// </summary>
        public ConcurrentDictionary<string, int> CommandCooldowns { get; set; }

        public ServerPermissions0_9(ulong id, string name)
        {
            Id = id;
            PermissionsControllerRole = "Nadeko";
            Verbose = true;

            Permissions = new Permissions(name);
            Permissions.Modules.TryAdd("NSFW", false);
            UserPermissions = new Dictionary<ulong, Permissions>();
            ChannelPermissions = new Dictionary<ulong, Permissions>();
            RolePermissions = new Dictionary<ulong, Permissions>();
            CommandCooldowns = new ConcurrentDictionary<string, int>();
            Words = new HashSet<string>();
        }
    }

    public class ServerSpecificConfig
    {
        public bool VoicePlusTextEnabled { get; set; }
        public bool SendPrivateMessageOnMention { get; set; }
        public ulong? LogChannel { get; set; } = null;
        public ulong? LogPresenceChannel { get; set; } = null;
        public HashSet<ulong> LogserverIgnoreChannels { get; set; }
        public ConcurrentDictionary<ulong, ulong> VoiceChannelLog { get; set; }
        public HashSet<ulong> ListOfSelfAssignableRoles { get; set; }
        public ulong AutoAssignedRole { get; set; }
        public ConcurrentDictionary<ulong, int> GenerateCurrencyChannels { get; set; }
        public bool AutoDeleteMessagesOnCommand { get; set; }
        public bool ExclusiveSelfAssignedRoles { get; set; }
        public float DefaultMusicVolume { get; set; }
        public HashSet<StreamNotificationConfig0_9> ObservingStreams { get; set; }
    }

    public class StreamNotificationConfig0_9
    {
        public string Username { get; set; }
        public StreamType Type { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public bool LastStatus { get; set; }

        public enum StreamType
        {
            Twitch,
            Beam,
            Hitbox,
            YoutubeGaming
        }
    }
}