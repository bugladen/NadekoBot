using System.Collections.Generic;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class BotConfiguration : IBotConfiguration
    {
        internal Task _8BallResponses;

        public HashSet<ulong> BlacklistedChannels { get; set; } = new HashSet<ulong>();

        public HashSet<ulong> BlacklistedServers { get; set; } = new HashSet<ulong>();

        public HashSet<ulong> BlacklistedUsers { get; set; } = new HashSet<ulong>();

        public ulong BufferSize { get; set; } = 4000000;

        public bool DontJoinServers { get; set; } = false;

        public bool ForwardMessages { get; set; } = true;

        public bool ForwardToAllOwners { get; set; } = true;

        public ModulePrefixes ModulePrefixes { get; set; } = new ModulePrefixes
        {
            Administration = ".",
            ClashOfClans = ",",
            Conversations = "<@{0}>",
            Gambling = "$",
            Games = ">",
            Pokemon = ">",
            Help = "-",
            Music = "!!",
            NSFW = "~",
            Permissions = ";",
            Programming = "%",
            Searches = "~",
            Trello = "trello",
            Utility = "."
        };

        public List<string> PlayingStatuses { get; set; } = new List<string>();

        public string RemindMessageFormat { get; set; } = "❗⏰**I've been told to remind you to '%message%' now by %user%.**⏰❗";

        public bool RotatePlayingStatus { get; set; } = false;

        public Currency Currency { get; set; } = new Currency
        {
            Name = "Nadeko Flower",
            PluralName = "Nadeko Flowers",
            Sign = "🌸",
        };

        public List<string> EightBallResponses { get; set; } = new List<string>
        {
            "Most definitely yes",
            "For sure",
            "As I see it, yes",
            "My sources say yes",
            "Yes",
            "Most likely",
            "Perhaps",
            "Maybe",
            "Not sure",
            "It is uncertain",
            "Ask me again later",
            "Don't count on it",
            "Probably not",
            "Very doubtful",
            "Most likely no",
            "Nope",
            "No",
            "My sources say no",
            "Dont even think about it",
            "Definitely no",
            "NO - It may cause disease contraction"
        };

        public List<string> RaceAnimals { get; set; } = new List<string>
        {
            "🐼",
            "🐻",
            "🐧",
            "🐨",
            "🐬",
            "🐞",
            "🦀",
            "🦄"
        };
    }
}
