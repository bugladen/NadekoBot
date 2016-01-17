using Discord;
using System;
using System.IO;
using Newtonsoft.Json;
using Parse;
using Discord.Commands;
using NadekoBot.Modules;
using Discord.Modules;
using Discord.Audio;
using System.Threading.Tasks;
using System.Timers;

namespace NadekoBot
{
    class NadekoBot
    {
        public static DiscordClient client;
        public static StatsCollector stats_collector;
        public static string botMention;
        public static string GoogleAPIKey = null;
        public static ulong OwnerID;
        public static string password;
        public static string TrelloAppKey;

        static void Main()
        {
            //load credentials from credentials.json
            Credentials c;
            bool trelloLoaded = false;
            try
            {
                c = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("credentials.json"));
                botMention = c.BotMention;
                if (c.GoogleAPIKey == null || c.GoogleAPIKey == "") {
                    Console.WriteLine("No google api key found. You will not be able to use music and links won't be shortened.");
                } else {
                    GoogleAPIKey = c.GoogleAPIKey;
                }
                if (c.TrelloAppKey == null || c.TrelloAppKey == "") {
                    Console.WriteLine("No trello appkey found. You will not be able to use trello commands.");
                } else {
                    TrelloAppKey = c.TrelloAppKey;
                    trelloLoaded = true;
                }
                OwnerID = c.OwnerID;
                password = c.Password;
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to load stuff from credentials.json, RTFM");
                Console.ReadKey();
                return;
            }

            //create new discord client
            client = new DiscordClient();

            //create a command service
            var commandService = new CommandService(new CommandServiceConfig
            {
                CommandChar = null,
                HelpMode = HelpMode.Disable
            });

            //init parse
            if (c.ParseKey != null && c.ParseID != null && c.ParseID != "" && c.ParseKey != "") {
                ParseClient.Initialize(c.ParseID, c.ParseKey);

                //monitor commands for logging
                stats_collector = new StatsCollector(commandService);
            } else {
                Console.WriteLine("Parse key and/or ID not found. Bot will not log.");
            }

            //reply to personal messages
            client.MessageReceived += Client_MessageReceived;

            //add command service
            var commands = client.Services.Add<CommandService>(commandService);
            
            //create module service
            var modules = client.Services.Add<ModuleService>(new ModuleService());

            //add audio service
            var audio = client.Services.Add<AudioService>(new AudioService(new AudioServiceConfig() {
                Channels = 2,
                EnableEncryption = false
            }));

            //install modules
            modules.Install(new Administration(), "Administration", FilterType.Unrestricted);
            modules.Install(new Conversations(), "Conversations", FilterType.Unrestricted);
            modules.Install(new Gambling(), "Gambling", FilterType.Unrestricted);
            modules.Install(new Games(), "Games", FilterType.Unrestricted);
            modules.Install(new Music(), "Music", FilterType.Unrestricted);
            modules.Install(new Searches(), "Searches", FilterType.Unrestricted);
            if(trelloLoaded)
                modules.Install(new Trello(), "Trello", FilterType.Unrestricted);

            //run the bot
            client.Run(async () =>
            {
                await client.Connect(c.Username, c.Password);
                Console.WriteLine("Connected!");
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }
        static bool repliedRecently = false;

        private static async void Client_MessageReceived(object sender, MessageEventArgs e) {
            if (e.Server != null) return;
            try {
                (await client.GetInvite(e.Message.Text))?.Accept();
            } catch (Exception) { }

            if (repliedRecently = !repliedRecently) {
                await e.Send("You can type `-h` or `-help` or `@MyName help` in any of the channels I am in and I will send you a message with my commands.\n Or you can find out what i do here: https://github.com/Kwoth/NadekoBot\nYou can also just send me an invite link to a server and I will join it.\nIf you don't want me on your server, you can simply ban me ;(");
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