using Discord;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace NadekoBot.Classes.JSONModels
{
    public class Configuration
    {
        [JsonIgnore]
        public static readonly Dictionary<string, List<string>> DefaultCustomReactions = new Dictionary<string, List<string>>
        {
            {@"\o\", new List<string>()
            { "/o/" } },
            {"/o/", new List<string>()
            { @"\o\" } },
            {"moveto", new List<string>() {
                @"(👉 ͡° ͜ʖ ͡°)👉 %target%" } },
            {"comeatmebro", new List<string>() {
                "%target% (ง’̀-‘́)ง" } },
            {"e", new List<string>() {
                "%user% did it 😒 🔫",
                "%target% did it 😒 🔫" } },
            {"%mention% insult", new List<string>() {
                "%target% You are a poop.",
                "%target% You're a jerk.",
                "%target% I will eat you when I get my powers back."
                 } },
            {"%mention% praise", new List<string>()
            {
                "%target% You are cool.",
                "%target% You are nice!",
                "%target% You did a good job.",
                "%target% You did something nice.",
                "%target% is awesome!",
                "%target% Wow."
            } },
            {"%mention% pat", new List<string>() {
                "http://i.imgur.com/IiQwK12.gif",
                "http://i.imgur.com/JCXj8yD.gif",
                "http://i.imgur.com/qqBl2bm.gif",
                "http://i.imgur.com/eOJlnwP.gif",
                "https://45.media.tumblr.com/229ec0458891c4dcd847545c81e760a5/tumblr_mpfy232F4j1rxrpjzo1_r2_500.gif",
                "https://media.giphy.com/media/KZQlfylo73AMU/giphy.gif",
                "https://media.giphy.com/media/12hvLuZ7uzvCvK/giphy.gif",
                "http://gallery1.anivide.com/_full/65030_1382582341.gif",
                "https://49.media.tumblr.com/8e8a099c4eba22abd3ec0f70fd087cce/tumblr_nxovj9oY861ur1mffo1_500.gif ",
            } },
            {"%mention% cry", new List<string>()
            {
                "http://i.imgur.com/Xg3i1Qy.gif",
                "http://i.imgur.com/3K8DRrU.gif",
                "http://i.imgur.com/k58BcAv.gif",
                "http://i.imgur.com/I2fLXwo.gif"
            } },
            {"%mention% are you real?", new List<string>()
            {
                "%user%, I will be soon."
            } },
            {"%mention% are you there?", new List<string>()
            {
                "Yes. :)"
            } },
            {"%mention% draw", new List<string>() {
                "Sorry, I don't gamble, type $draw for that function."
            } },
            {"%mention% bb", new List<string>()
            {
                "Bye %target%"
            } },
            {"%mention% call", new List<string>() {
                "Calling %target%"
            } },
            {"%mention% disguise", new List<string>() {
                "https://cdn.discordapp.com/attachments/140007341880901632/156721710458994690/Cc5mixjUYAADgBs.jpg",
                "https://cdn.discordapp.com/attachments/140007341880901632/156721715831898113/hqdefault.jpg",
                "https://cdn.discordapp.com/attachments/140007341880901632/156721724430352385/okawari_01_haruka_weird_mask.jpg",
                "https://cdn.discordapp.com/attachments/140007341880901632/156721728763068417/mustache-best-girl.png"

            } }
        };

        public bool DontJoinServers { get; set; } = false;
        public bool ForwardMessages { get; set; } = true;
        public bool ForwardToAllOwners { get; set; } = false;
        public bool IsRotatingStatus { get; set; } = false;
        public int BufferSize { get; set; } = 4.MiB();

        public List<Quote> Quotes { get; set; } = new List<Quote>();

        [JsonIgnore]
        public List<PokemonType> PokemonTypes { get; set; } = new List<PokemonType>();

        public string RemindMessageFormat { get; set; } = "❗⏰**I've been told to remind you to '%message%' now by %user%.**⏰❗";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, List<string>> CustomReactions { get; set; }

        public List<string> RotatingStatuses { get; set; } = new List<string>();
        public CommandPrefixesModel CommandPrefixes { get; set; } = new CommandPrefixesModel();
        public HashSet<ulong> ServerBlacklist { get; set; } = new HashSet<ulong>();
        public HashSet<ulong> ChannelBlacklist { get; set; } = new HashSet<ulong>();

        public HashSet<ulong> UserBlacklist { get; set; } = new HashSet<ulong>() {
            105309315895693312,
            119174277298782216,
            143515953525817344
        };

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (CustomReactions == null)
            {
                CustomReactions = DefaultCustomReactions;
            }
        }
        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            if (CustomReactions == null)
            {
                CustomReactions = DefaultCustomReactions;
            }
        }

        public string[] _8BallResponses { get; set; } =
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

        public string CurrencySign { get; set; } = "🌸";
        public string CurrencyName { get; set; } = "NadekoFlower";
        public string DMHelpString { get; set; } = "Type `-h` for help.";
        public string HelpString { get; set; } = @"You can use `{0}modules` command to see a list of all modules.
You can use `{0}commands ModuleName`
(for example `{0}commands Administration`) to see a list of all of the commands in that module.
For a specific command help, use `{0}h ""Command name""` (for example `-h ""!m q""`)


**LIST OF COMMANDS CAN BE FOUND ON THIS LINK**
<https://github.com/Kwoth/NadekoBot/blob/master/commandlist.md>


Nadeko Support Server: <https://discord.gg/0ehQwTK2RBjAxzEY>";
    }

    public class CommandPrefixesModel
    {
        public string Administration { get; set; } = ".";
        public string Searches { get; set; } = "~";
        public string NSFW { get; set; } = "~";
        public string Conversations { get; set; } = "<@{0}>";
        public string ClashOfClans { get; set; } = ",";
        public string Help { get; set; } = "-";
        public string Music { get; set; } = "!m";
        public string Trello { get; set; } = "trello ";
        public string Games { get; set; } = ">";
        public string Gambling { get; set; } = "$";
        public string Permissions { get; set; } = ";";
        public string Programming { get; set; } = "%";
        public string Pokemon { get; set; } = ">";
        public string Utility { get; set; } = ".";
    }

    public static class ConfigHandler
    {
        private static readonly object configLock = new object();
        public static void SaveConfig()
        {
            lock (configLock)
            {
                File.WriteAllText("data/config.json", JsonConvert.SerializeObject(NadekoBot.Config, Formatting.Indented));
            }
        }

        public static bool IsBlackListed(MessageEventArgs evArgs) => IsUserBlacklisted(evArgs.User.Id) ||
                                                                      (!evArgs.Channel.IsPrivate &&
                                                                       (IsChannelBlacklisted(evArgs.Channel.Id) || IsServerBlacklisted(evArgs.Server.Id)));

        public static bool IsServerBlacklisted(ulong id) => NadekoBot.Config.ServerBlacklist.Contains(id);

        public static bool IsChannelBlacklisted(ulong id) => NadekoBot.Config.ChannelBlacklist.Contains(id);

        public static bool IsUserBlacklisted(ulong id) => NadekoBot.Config.UserBlacklist.Contains(id);
    }

    public class Quote
    {
        public string Author { get; set; }
        public string Text { get; set; }

        public override string ToString() =>
            $"{Text}\n\t*-{Author}*";
    }

}
