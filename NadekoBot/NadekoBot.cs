using Discord;
using System;
using System.IO;
using Newtonsoft.Json;
using Discord.Commands;
using NadekoBot.Modules;
using Discord.Modules;
using Discord.Audio;
using NadekoBot.Extensions;
using System.Timers;
using System.Linq;
using NadekoBot.Classes;

namespace NadekoBot {
    public class NadekoBot {
        public static DiscordClient Client;
        public static string botMention;
        public static string GoogleAPIKey = null;
        public static Channel OwnerPrivateChannel = null;
        public static string TrelloAppKey;
        public static bool ForwardMessages = false;
        public static Credentials Creds { get; set; }

        static void Main() {
            //load credentials from credentials.json
            bool loadTrello = false;
            try {
                Creds = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("credentials.json"));
                botMention = Creds.BotMention;
                if (string.IsNullOrWhiteSpace(Creds.GoogleAPIKey)) {
                    Console.WriteLine("No google api key found. You will not be able to use music and links won't be shortened.");
                }
                else {
                    Console.WriteLine("Google API key provided.");
                    GoogleAPIKey = Creds.GoogleAPIKey;
                }
                if (string.IsNullOrWhiteSpace(Creds.TrelloAppKey)) {
                    Console.WriteLine("No trello appkey found. You will not be able to use trello commands.");
                }
                else {
                    Console.WriteLine("Trello app key provided.");
                    TrelloAppKey = Creds.TrelloAppKey;
                    loadTrello = true;
                }
                if (Creds.ForwardMessages != true)
                    Console.WriteLine("Not forwarding messages.");
                else {
                    ForwardMessages = true;
                    Console.WriteLine("Forwarding messages.");
                }
                if (string.IsNullOrWhiteSpace(Creds.SoundCloudClientID))
                    Console.WriteLine("No soundcloud Client ID found. Soundcloud streaming is disabled.");
                else
                    Console.WriteLine("SoundCloud streaming enabled.");
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed to load stuff from credentials.json, RTFM\n{ex.Message}");
                Console.ReadKey();
                return;
            }

            //create new discord client
            Client = new DiscordClient(new DiscordConfigBuilder() {
                MessageCacheSize = 20,
                LogLevel = LogSeverity.Warning,
                LogHandler = (s, e) => {
                    try {
                        Console.WriteLine($"Severity: {e.Severity}\nMessage: {e.Message}\nExceptionMessage: {e.Exception?.Message ?? "-"}");//\nException: {(e.Exception?.ToString() ?? "-")}");
                    }
                    catch { }
                }
            });

            //create a command service
            var commandService = new CommandService(new CommandServiceConfigBuilder {
                AllowMentionPrefix = false,
                CustomPrefixHandler = m => 0,
                HelpMode = HelpMode.Disabled,
                ErrorHandler = async (s, e) => {
                    try {
                        if (e.ErrorType != CommandErrorType.BadPermissions)
                            return;
                        if (string.IsNullOrWhiteSpace(e.Exception.Message))
                            return;
                        await e.Channel.SendMessage(e.Exception.Message);
                    }
                    catch { }
                }
            });

            //reply to personal messages and forward if enabled.
            Client.MessageReceived += Client_MessageReceived;

            //add command service
            var commands = Client.AddService<CommandService>(commandService);

            //create module service
            var modules = Client.AddService<ModuleService>(new ModuleService());

            //add audio service
            var audio = Client.AddService<AudioService>(new AudioService(new AudioServiceConfigBuilder() {
                Channels = 2,
                EnableEncryption = false,
                EnableMultiserver = true,
                Bitrate = 128,
            }));

            //install modules
            modules.Add(new Administration(), "Administration", ModuleFilter.None);
            modules.Add(new Help(), "Help", ModuleFilter.None);
            modules.Add(new PermissionModule(), "Permissions", ModuleFilter.None);
            modules.Add(new Conversations(), "Conversations", ModuleFilter.None);
            modules.Add(new Gambling(), "Gambling", ModuleFilter.None);
            modules.Add(new Games(), "Games", ModuleFilter.None);
            modules.Add(new Music(), "Music", ModuleFilter.None);
            modules.Add(new Searches(), "Searches", ModuleFilter.None);
            if (loadTrello)
                modules.Add(new Trello(), "Trello", ModuleFilter.None);
            modules.Add(new NSFW(), "NSFW", ModuleFilter.None);

            //run the bot
            Client.ExecuteAndWait(async () => {
                try {
                    await Client.Connect(Creds.Username, Creds.Password);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Probably wrong EMAIL or PASSWORD.\n{ex.Message}");
                    Console.ReadKey();
                    Console.WriteLine(ex);
                    Console.ReadKey();
                    return;
                }
                Console.WriteLine("-----------------");
                Console.WriteLine(await NadekoStats.Instance.GetStats());
                Console.WriteLine("-----------------");

                try {
                    OwnerPrivateChannel = await Client.CreatePrivateChannel(OwnerId);
                }
                catch {
                    Console.WriteLine("Failed creating private channel with the owner");
                }

                Classes.Permissions.PermissionsHandler.Initialize();
                
                Client.ClientAPI.SendingRequest += (s, e) => {

                    try {
                        var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                        if (request != null) {
                            //@everyοne
                            request.Content = request.Content?.Replace("@everyone", "@everryone") ?? "_error_";
                            if (string.IsNullOrWhiteSpace(request.Content))
                                e.Cancel = true;
                            //else
                            //    Console.WriteLine("Sending request");
                        }
                    }
                    catch {
                        Console.WriteLine("SENDING REQUEST ERRORED!!!!");
                    }
                };

                //client.ClientAPI.SentRequest += (s, e) => {
                //    try {
                //        var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                //        if (request != null) {
                //            Console.WriteLine("Sent.");
                //        }
                //    }
                //    catch { Console.WriteLine("SENT REQUEST ERRORED!!!"); }
                //};
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }

        static bool repliedRecently = false;
        private static async void Client_MessageReceived(object sender, MessageEventArgs e) {
            try {
                if (e.Server != null || e.User.Id == Client.CurrentUser.Id) return;
                if (PollCommand.ActivePolls.SelectMany(kvp => kvp.Key.Users.Select(u => u.Id)).Contains(e.User.Id)) return;
                // just ban this trash AutoModerator
                // and cancer christmass spirit
                // and crappy shotaslave
                if (e.User.Id == 105309315895693312 ||
                    e.User.Id == 119174277298782216 ||
                    e.User.Id == 143515953525817344)
                    return; // FU

                if (!NadekoBot.Creds.DontJoinServers) {
                    try {
                        await (await Client.GetInvite(e.Message.Text)).Accept();
                        await e.Channel.SendMessage("I got in!");
                        return;
                    }
                    catch {
                        if (e.User.Id == 109338686889476096) { //carbonitex invite
                            await e.Channel.SendMessage("Failed to join the server.");
                            return;
                        }
                    }
                }

                if (ForwardMessages && OwnerPrivateChannel != null)
                    await OwnerPrivateChannel.SendMessage(e.User + ": ```\n" + e.Message.Text + "\n```");

                if (!repliedRecently) {
                    repliedRecently = true;
                    await e.Channel.SendMessage("**FULL LIST OF COMMANDS**:\n❤ <https://gist.github.com/Kwoth/1ab3a38424f208802b74> ❤\n\n⚠**COMMANDS DO NOT WORK IN PERSONAL MESSAGES**\n\n\n**Bot Creator's server:** <https://discord.gg/0ehQwTK2RBjAxzEY>");
                    Timer t = new Timer();
                    t.Interval = 2000;
                    t.Start();
                    t.Elapsed += (s, ev) => {
                        try {
                            repliedRecently = false;
                            t.Stop();
                            t.Dispose();
                        }
                        catch { }
                    };
                }
            }
            catch { }
        }
    }
}

//95520984584429568 meany