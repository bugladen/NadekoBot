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

            //help command
            commands.CreateCommand("-h")
                .Alias(new string[]{"-help",NadekoBot.botMention+" help", NadekoBot.botMention+" h"})
                .Description("Help command")
                .Do(async e =>
                {
                    string helpstr = "";
                    foreach (var com in client.Commands().AllCommands) {
                        helpstr += "&###**#" + com.Category + "#**\n";
                        helpstr += PrintCommandHelp(com);
                    }
                    while (helpstr.Length > 2000) {
                        var curstr = helpstr.Substring(0, 2000);
                        await client.SendPrivateMessage(e.User, curstr.Substring(0,curstr.LastIndexOf("&")));
                        helpstr = curstr.Substring(curstr.LastIndexOf("&")) + helpstr.Substring(2000);
                    }
                    await client.SendPrivateMessage(e.User, helpstr);
                });
            
            //create module service
            var modules = client.AddService(new ModuleService());

            //install modules
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

        private static string PrintCommandHelp(Command com)
        {
            var str = "`" + com.Text + "`\n";
            foreach (var a in com.Aliases)
                str += "`" + a + "`\n";
            str += "Description: " + com.Description + "\n";
            return str;
        }
        /* removed
        private static void Crawl()
        {
            Timer t = new Timer();
            t.Interval = 5000; // start crawling after 5 seconds
            t.Elapsed += (s, e) => {
                var wc = new WebCrawler.WebCrawler();
                WebCrawler.WebCrawler.OnFoundInvite += inv => { TryJoin(inv); };
                t.Stop();
            };
            t.Start();
        }
        */

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