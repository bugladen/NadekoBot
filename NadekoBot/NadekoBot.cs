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

namespace NadekoBot {
    class NadekoBot {
        public static DiscordClient client;
        public static string botMention;
        public static string GoogleAPIKey = null;
        public static ulong OwnerID;
        public static User OwnerUser = null;
        public static string password;
        public static string TrelloAppKey;
        public static bool ForwardMessages = false;
        public static Credentials creds;

        static void Main() {
            //load credentials from credentials.json
            bool loadTrello = false;
            try {
                creds = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("credentials.json"));
                botMention = creds.BotMention;
                if (string.IsNullOrWhiteSpace(creds.GoogleAPIKey)) {
                    Console.WriteLine("No google api key found. You will not be able to use music and links won't be shortened.");
                } else {
                    Console.WriteLine("Google API key provided.");
                    GoogleAPIKey = creds.GoogleAPIKey;
                }
                if (string.IsNullOrWhiteSpace(creds.TrelloAppKey)) {
                    Console.WriteLine("No trello appkey found. You will not be able to use trello commands.");
                } else {
                    Console.WriteLine("Trello app key provided.");
                    TrelloAppKey = creds.TrelloAppKey;
                    loadTrello = true;
                }
                if (creds.ForwardMessages != true)
                    Console.WriteLine("Not forwarding messages.");
                else {
                    ForwardMessages = true;
                    Console.WriteLine("Forwarding messages.");
                }
                if (string.IsNullOrWhiteSpace(creds.ParseID) || string.IsNullOrWhiteSpace(creds.ParseKey)) {
                    Console.WriteLine("Parse key and/or ID not found. Those are mandatory.");
                    Console.ReadKey();
                    return;
                }
                if(string.IsNullOrWhiteSpace(creds.OsuApiKey))
                    Console.WriteLine("No osu API key found. Osu functionality is disabled.");
                else
                    Console.WriteLine("Osu enabled.");
                if(string.IsNullOrWhiteSpace(creds.SoundCloudClientID))
                    Console.WriteLine("No soundcloud Client ID found. Soundcloud streaming is disabled.");
                else
                    Console.WriteLine("SoundCloud streaming enabled.");

                //init parse
                ParseClient.Initialize(creds.ParseID, creds.ParseKey);

                OwnerID = creds.OwnerID;
                password = creds.Password;
            } catch (Exception ex) {
                Console.WriteLine($"Failed to load stuff from credentials.json, RTFM\n{ex.Message}");
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
            
            //reply to personal messages and forward if enabled.
            client.MessageReceived += Client_MessageReceived;

            //add command service
            var commands = client.Services.Add<CommandService>(commandService);

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

            //run the bot
            client.ExecuteAndWait(async () => {
                await client.Connect(creds.Username, creds.Password);
                Console.WriteLine("-----------------");
                Console.WriteLine(NadekoStats.Instance.GetStats());
                Console.WriteLine("-----------------");

                foreach (var serv in client.Servers) {
                    if ((OwnerUser = serv.GetUser(OwnerID)) != null)
                        return;
                }

                client.ClientAPI.SendingRequest += (s, e) =>
                {
                    var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                    if (request != null) {
                        if (string.IsNullOrWhiteSpace(request.Content))
                            e.Cancel = true;
                    }
                };
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
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
                await e.Send("**COMMANDS DO NOT WORK IN PERSONAL MESSAGES**\nYou can type `-h` or `-help` or `@MyName help` in any of the channels I am in and I will send you a message with my commands.\n Or you can find out what i do here: https://github.com/Kwoth/NadekoBot\nYou can also just send me an invite link to a server and I will join it.\nIf you don't want me on your server, you can simply ban me ;(\nBot Creator's server: https://discord.gg/0ehQwTK2RBhxEi0X");
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