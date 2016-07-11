using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes.Help.Commands;
using NadekoBot.Classes.JSONModels;
using NadekoBot.Modules.Administration;
using NadekoBot.Modules.ClashOfClans;
using NadekoBot.Modules.Conversations;
using NadekoBot.Modules.CustomReactions;
using NadekoBot.Modules.Gambling;
using NadekoBot.Modules.Games;
using NadekoBot.Modules.Games.Commands;
using NadekoBot.Modules.Help;
#if !NADEKO_RELEASE
using NadekoBot.Modules.Music;
#endif
using NadekoBot.Modules.NSFW;
using NadekoBot.Modules.Permissions;
using NadekoBot.Modules.Permissions.Classes;
using NadekoBot.Modules.Pokemon;
using NadekoBot.Modules.Searches;
using NadekoBot.Modules.Translator;
using NadekoBot.Modules.Trello;
using NadekoBot.Modules.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class NadekoBot
    {
        public static DiscordClient Client { get; private set; }
        public static Credentials Creds { get; set; }
        public static Configuration Config { get; set; }
        public static LocalizedStrings Locale { get; set; } = new LocalizedStrings();
        public static string BotMention { get; set; } = "";
        public static bool Ready { get; set; } = false;

        private static List<Channel> OwnerPrivateChannels { get; set; }

        private static void Main()
        {
            Console.OutputEncoding = Encoding.Unicode;

            try
            {
                File.WriteAllText("data/config_example.json", JsonConvert.SerializeObject(new Configuration(), Formatting.Indented));
                if (!File.Exists("data/config.json"))
                    File.Copy("data/config_example.json", "data/config.json");
                File.WriteAllText("credentials_example.json", JsonConvert.SerializeObject(new Credentials(), Formatting.Indented));

            }
            catch
            {
                Console.WriteLine("Failed writing credentials_example.json or data/config_example.json");
            }

            try
            {
                Config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("data/config.json"));
                Config.Quotes = JsonConvert.DeserializeObject<List<Quote>>(File.ReadAllText("data/quotes.json"));
                Config.PokemonTypes = JsonConvert.DeserializeObject<List<PokemonType>>(File.ReadAllText("data/PokemonTypes.json"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed loading configuration.");
                Console.WriteLine(ex);
                Console.ReadKey();
                return;
            }

            try
            {
                //load credentials from credentials.json
                Creds = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("credentials.json"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load stuff from credentials.json, RTFM\n{ex.Message}");
                Console.ReadKey();
                return;
            }

            //if password is not entered, prompt for password
            if (string.IsNullOrWhiteSpace(Creds.Token))
            {
                Console.WriteLine("Token blank. Please enter your bot's token:\n");
                Creds.Token = Console.ReadLine();
            }

            Console.WriteLine(string.IsNullOrWhiteSpace(Creds.GoogleAPIKey)
                ? "No google api key found. You will not be able to use music and links won't be shortened."
                : "Google API key provided.");
            Console.WriteLine(string.IsNullOrWhiteSpace(Creds.TrelloAppKey)
                ? "No trello appkey found. You will not be able to use trello commands."
                : "Trello app key provided.");
            Console.WriteLine(Config.ForwardMessages != true
                ? "Not forwarding messages."
                : "Forwarding private messages to owner.");
            Console.WriteLine(string.IsNullOrWhiteSpace(Creds.SoundCloudClientID)
                ? "No soundcloud Client ID found. Soundcloud streaming is disabled."
                : "SoundCloud streaming enabled.");
            Console.WriteLine(string.IsNullOrWhiteSpace(Creds.OsuAPIKey)
                ? "No osu! api key found. Song & top score lookups will not work. User lookups still available."
                : "osu! API key provided.");

            BotMention = $"<@{Creds.BotId}>";

            //create new discord client and log
            Client = new DiscordClient(new DiscordConfigBuilder()
            {
                MessageCacheSize = 10,
                ConnectionTimeout = 120000,
                LogLevel = LogSeverity.Warning,
                LogHandler = (s, e) =>
                    Console.WriteLine($"Severity: {e.Severity}" +
                                      $"Message: {e.Message}" +
                                      $"ExceptionMessage: {e.Exception?.Message ?? "-"}"),
            });

            //create a command service
            var commandService = new CommandService(new CommandServiceConfigBuilder
            {
                AllowMentionPrefix = false,
                CustomPrefixHandler = m => 0,
                HelpMode = HelpMode.Disabled,
                ErrorHandler = async (s, e) =>
                {
                    if (e.ErrorType != CommandErrorType.BadPermissions)
                        return;
                    if (string.IsNullOrWhiteSpace(e.Exception?.Message))
                        return;
                    try
                    {
                        await e.Channel.SendMessage(e.Exception.Message).ConfigureAwait(false);
                    }
                    catch { }
                }
            });

            //reply to personal messages and forward if enabled.
            Client.MessageReceived += Client_MessageReceived;

            //add command service
            Client.AddService<CommandService>(commandService);

            //create module service
            var modules = Client.AddService<ModuleService>(new ModuleService());

            //add audio service
            Client.AddService<AudioService>(new AudioService(new AudioServiceConfigBuilder()
            {
                Channels = 2,
                EnableEncryption = false,
                Bitrate = 128,
            }));

            //install modules
            modules.Add(new HelpModule(), "Help", ModuleFilter.None);
            modules.Add(new AdministrationModule(), "Administration", ModuleFilter.None);
            modules.Add(new UtilityModule(), "Utility", ModuleFilter.None);
            modules.Add(new PermissionModule(), "Permissions", ModuleFilter.None);
            modules.Add(new Conversations(), "Conversations", ModuleFilter.None);
            modules.Add(new GamblingModule(), "Gambling", ModuleFilter.None);
            modules.Add(new GamesModule(), "Games", ModuleFilter.None);
#if !NADEKO_RELEASE
            modules.Add(new MusicModule(), "Music", ModuleFilter.None);
#endif
            modules.Add(new SearchesModule(), "Searches", ModuleFilter.None);
            modules.Add(new NSFWModule(), "NSFW", ModuleFilter.None);
            modules.Add(new ClashOfClansModule(), "ClashOfClans", ModuleFilter.None);
            modules.Add(new PokemonModule(), "Pokegame", ModuleFilter.None);
            modules.Add(new TranslatorModule(), "Translator", ModuleFilter.None);
            modules.Add(new CustomReactionsModule(), "Customreactions", ModuleFilter.None);
            if (!string.IsNullOrWhiteSpace(Creds.TrelloAppKey))
                modules.Add(new TrelloModule(), "Trello", ModuleFilter.None);

            //run the bot
            Client.ExecuteAndWait(async () =>
            {
                try
                {
                    await Client.Connect(Creds.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Token is wrong. Don't set a token if you don't have an official BOT account.");
                    Console.WriteLine(ex);
                    Console.ReadKey();
                    return;
                }
#if NADEKO_RELEASE
                await Task.Delay(120000).ConfigureAwait(false);
#else
                await Task.Delay(1000).ConfigureAwait(false);
#endif

                Console.WriteLine("-----------------");
                Console.WriteLine(await NadekoStats.Instance.GetStats().ConfigureAwait(false));
                Console.WriteLine("-----------------");


                OwnerPrivateChannels = new List<Channel>(Creds.OwnerIds.Length);
                foreach (var id in Creds.OwnerIds)
                {
                    try
                    {
                        OwnerPrivateChannels.Add(await Client.CreatePrivateChannel(id).ConfigureAwait(false));
                    }
                    catch
                    {
                        Console.WriteLine($"Failed creating private channel with the owner {id} listed in credentials.json");
                    }
                }
                Client.ClientAPI.SendingRequest += (s, e) =>
                {
                    var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                    if (request == null) return;
                    // meew0 is magic
                    request.Content = request.Content?.Replace("@everyone", "@everyοne").Replace("@here", "@һere") ?? "_error_";
                    if (string.IsNullOrWhiteSpace(request.Content))
                        e.Cancel = true;
                };
                PermissionsHandler.Initialize();
                NadekoBot.Ready = true;
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }

        public static bool IsOwner(ulong id) => Creds.OwnerIds.Contains(id);

        public static async Task SendMessageToOwner(string message)
        {
            if (Config.ForwardMessages && OwnerPrivateChannels.Any())
                if (Config.ForwardToAllOwners)
                    OwnerPrivateChannels.ForEach(async c =>
                    {
                        try { await c.SendMessage(message).ConfigureAwait(false); } catch { }
                    });
                else
                {
                    var c = OwnerPrivateChannels.FirstOrDefault();
                    if (c != null)
                        await c.SendMessage(message).ConfigureAwait(false);
                }
        }

        private static bool repliedRecently = false;
        private static async void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Server != null || e.User.Id == Client.CurrentUser.Id) return;
                if (PollCommand.ActivePolls.SelectMany(kvp => kvp.Key.Users.Select(u => u.Id)).Contains(e.User.Id)) return;
                if (ConfigHandler.IsBlackListed(e))
                    return;

                if (Config.ForwardMessages && !NadekoBot.Creds.OwnerIds.Contains(e.User.Id) && OwnerPrivateChannels.Any())
                    await SendMessageToOwner(e.User + ": ```\n" + e.Message.Text + "\n```").ConfigureAwait(false);

                if (repliedRecently) return;

                repliedRecently = true;
                if (e.Message.RawText != NadekoBot.Config.CommandPrefixes.Help + "h")
                    await e.Channel.SendMessage(HelpCommand.DMHelpString).ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false);
                repliedRecently = false;
            }
            catch { }
        }
    }
}

//95520984584429568 meany
