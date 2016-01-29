using Discord;
using System;
using System.IO;
using Newtonsoft.Json;
using Parse;
using Discord.Commands;
using NadekoBot.Modules;
using Discord.Modules;
using Discord.Audio;
using NadekoBot.Extensions;
using System.Timers;
using System.Linq;
using System.Diagnostics;

namespace NadekoBot {
    class NadekoBot {
        public static DiscordClient client;
        public static StatsCollector stats_collector;
        public static string botMention;
        public static string GoogleAPIKey = null;
        public static ulong OwnerID;
        public static User OwnerUser = null;
        public static string password;
        public static string TrelloAppKey;
        public static bool ForwardMessages = false;
        public static string BotVersion = "0.8-beta2";
        public static int commandsRan = 0;

        static void Main() {
            //load credentials from credentials.json
            Credentials c;
            bool loadTrello = false;
            try {
                c = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("credentials.json"));
                botMention = c.BotMention;
                if (c.GoogleAPIKey == null || c.GoogleAPIKey == "") {
                    Console.WriteLine("No google api key found. You will not be able to use music and links won't be shortened.");
                } else {
                    Console.WriteLine("Google API key provided.");
                    GoogleAPIKey = c.GoogleAPIKey;
                }
                if (c.TrelloAppKey == null || c.TrelloAppKey == "") {
                    Console.WriteLine("No trello appkey found. You will not be able to use trello commands.");
                } else {
                    Console.WriteLine("Trello app key provided.");
                    TrelloAppKey = c.TrelloAppKey;
                    loadTrello = true;
                }
                if (c.ForwardMessages != true)
                    Console.WriteLine("Not forwarding messages.");
                else {
                    ForwardMessages = true;
                    Console.WriteLine("Forwarding messages.");
                }

                OwnerID = c.OwnerID;
                password = c.Password;
            } catch (Exception ex) {
                Console.WriteLine("Failed to load stuff from credentials.json, RTFM");
                Console.ReadKey();
                return;
            }

            //create new discord client
            client = new DiscordClient();

            //create a command service
            var commandService = new CommandService(new CommandServiceConfig {
                CommandChar = null,
                HelpMode = HelpMode.Disable
            });

            //init parse
            if (c.ParseKey != null && c.ParseID != null && c.ParseID != "" && c.ParseKey != "") {
                ParseClient.Initialize(c.ParseID, c.ParseKey);

                //monitor commands for logging
                stats_collector = new StatsCollector(commandService);
            } else {
                Console.WriteLine("Parse key and/or ID not found. Logging disabled.");
            }

            //reply to personal messages and forward if enabled.
            client.MessageReceived += Client_MessageReceived;

            //add command service
            var commands = client.Services.Add<CommandService>(commandService);

            //count commands ran
            client.Commands().CommandExecuted += (s, e) => commandsRan++;

            //create module service
            var modules = client.Services.Add<ModuleService>(new ModuleService());

            //add audio service
            var audio = client.Services.Add<AudioService>(new AudioService(new AudioServiceConfig() {
                Channels = 2,
                EnableEncryption = false,
                EnableMultiserver = true,
                Bitrate = 128,
            }));

            //install modules
            modules.Add(new Administration(), "Administration", ModuleFilter.None);
            modules.Add(new Conversations(), "Conversations", ModuleFilter.None);
            modules.Add(new Gambling(), "Gambling", ModuleFilter.None);
            modules.Add(new Games(), "Games", ModuleFilter.None);
            modules.Add(new Music(), "Music", ModuleFilter.None);
            modules.Add(new Searches(), "Searches", ModuleFilter.None);
            if (loadTrello)
                modules.Add(new Trello(), "Trello", ModuleFilter.None);

            //start the timer for stats
            _statsSW.Start();
            //run the bot
            client.ExecuteAndWait(async () => {
                await client.Connect(c.Username, c.Password);

                LoadStats();
                Console.WriteLine("-----------------");
                Console.WriteLine(GetStats());
                Console.WriteLine("-----------------");

                foreach (var serv in client.Servers) {
                    if ((OwnerUser = serv.GetUser(OwnerID)) != null)
                        return;
                }

            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }
        private static string _statsCache = "";
        private static Stopwatch _statsSW = new Stopwatch();
        public static string GetStats() {
            if (_statsSW.ElapsedTicks > 5) {
                LoadStats();
                _statsSW.Restart();
            }
            return _statsCache;
        }
        private static void LoadStats() {
            _statsCache =
            "Author: Kwoth" +
            $"\nDiscord.Net version: {DiscordConfig.LibVersion}" +
            $"\nRuntime: {client.GetRuntime()}" +
            $"\nBot Version: {BotVersion}" +
            $"\nLogged in as: {client.CurrentUser.Name}" +
            $"\nBot id: {client.CurrentUser.Id}" +
            $"\nUptime: {GetUptimeString()}" +
            $"\nServers: {client.Servers.Count()}" +
            $"\nChannels: {client.Servers.Sum(s => s.AllChannels.Count())}" +
            $"\nUsers: {client.Servers.SelectMany(x => x.Users.Select(y => y.Id)).Count()} ({client.Servers.SelectMany(x => x.Users.Select(y => y.Id)).Distinct().Count()} unique) ({client.Servers.SelectMany(x => x.Users.Where(y => y.Status != UserStatus.Offline).Select(y => y.Id)).Distinct().Count()} online)" +
            $"\nHeap: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString()}MB" +
            $"\nCommands Ran this session: {commandsRan}";
        }

        public static string GetUptimeString() {
            var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
            return time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";
        }

        static bool repliedRecently = false;
        private static async void Client_MessageReceived(object sender, MessageEventArgs e) {
            if (e.Server != null || e.User.Id == client.CurrentUser.Id) return;

            //just ban this trash AutoModerator
            if (e.User.Id == 105309315895693312)
                return; // FU

            try {
                await (await client.GetInvite(e.Message.Text)).Accept();
                await e.Send("I got in!");
                return;
            } catch (Exception) {
                if (e.User.Id == 109338686889476096) { //carbonitex invite
                    await e.Send("Failed to join the server.");
                    return;
                }
            }

            if (ForwardMessages && OwnerUser != null)
                await OwnerUser.SendMessage(e.User + ": ```\n" + e.Message.Text + "\n```");

            if (repliedRecently = !repliedRecently) {
                await e.Send("You can type `-h` or `-help` or `@MyName help` in any of the channels I am in and I will send you a message with my commands.\n Or you can find out what i do here: https://github.com/Kwoth/NadekoBot\nYou can also just send me an invite link to a server and I will join it.\nIf you don't want me on your server, you can simply ban me ;(\nBot Creator's server: https://discord.gg/0ehQwTK2RBhxEi0X");
                Timer t = new Timer();
                t.Interval = 2000;
                t.Start();
                t.Elapsed += (s, ev) => {
                    repliedRecently = !repliedRecently;
                    t.Stop();
                    t.Dispose();
                };
            }
        }
    }
}