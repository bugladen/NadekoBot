using Discord;
using System;
using System.IO;
using Newtonsoft.Json;
using Discord.Commands;
using Discord.Modules;
using System.Text.RegularExpressions;
using Parse;
using NadekoBot.Modules;
using System.Timers;

namespace NadekoBot
{
    class NadekoBot
    {
        public static DiscordClient client;
        public static StatsCollector sc;
        public static string botMention;
        public static string GoogleAPIKey;
        public static long OwnerID;

        static void Main(string[] args)
        {
            //load credentials from credentials.json
            Credentials c;
            try
            {
                c = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("credentials.json"));
                botMention = c.BotMention;
                GoogleAPIKey = c.GoogleAPIKey;
                OwnerID = c.OwnerID;
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to load stuff from credentials.json, RTFM");
                Console.ReadKey();
                return;
            }

            //init parse
            if (c.ParseKey != null && c.ParseID != null)
                ParseClient.Initialize(c.ParseID,c.ParseKey);

            //create new discord client
            client = new DiscordClient(new DiscordClientConfig() {
                VoiceMode = DiscordVoiceMode.Outgoing,
            });

            //create a command service
            var commandService = new CommandService(new CommandServiceConfig
            {
                CommandChar = null,
                HelpMode = HelpMode.Disable
            });

            //monitor commands for logging
            sc = new StatsCollector(commandService);

            //add command service
            var commands = client.AddService(commandService);
            
            //create module service
            var modules = client.AddService(new ModuleService());

            //install modules
            modules.Install(new Administration(), "Administration", FilterType.Unrestricted);
            modules.Install(new Conversations(), "Conversation", FilterType.Unrestricted);
            modules.Install(new Gambling(), "Gambling", FilterType.Unrestricted);
            modules.Install(new Games(), "Games", FilterType.Unrestricted);
            modules.Install(new Music(), "Music", FilterType.Unrestricted);
            modules.Install(new Searches(), "Searches", FilterType.Unrestricted);

            commands.CommandError += Commands_CommandError;

            //run the bot
            client.Run(async () =>
            {
                Console.WriteLine("Trying to connect...");
                try
                {
                    await client.Connect(c.Username, c.Password);
                    Console.WriteLine("Connected!");
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }

        private static void Commands_CommandError(object sender, CommandErrorEventArgs e)
        {
            if (e.Command != null)
                client.SendMessage(e.Channel, Mention.User(e.User) + " Command failed. See help (-h).");
        }

        private static async void TryJoin(string code)
        {
            try
            {
                await NadekoBot.client.AcceptInvite(await NadekoBot.client.GetInvite(code));
                File.AppendAllText("invites.txt", code + "\n");
            }
            catch (Exception)
            {
                StatsCollector.DEBUG_LOG("Failed to join " + code);
            }
        }
    }
}