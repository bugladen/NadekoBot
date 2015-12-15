using Discord;
using System;
using System.IO;
using Newtonsoft.Json;
using Parse;
using Discord.Commands;
using NadekoBot.Modules;
using Discord.Modules;

namespace NadekoBot
{
    class NadekoBot
    {
        public static DiscordClient client;
       // public static StatsCollector stats_collector;
        public static string botMention;
        public static string GoogleAPIKey;
        public static ulong OwnerID;

        static void Main()
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

            client = new DiscordClient();
            
            //init parse
            if (c.ParseKey != null && c.ParseID != null)
                ParseClient.Initialize(c.ParseID,c.ParseKey);

            //create new discord client
            

            //create a command service
            var commandService = new CommandService(new CommandServiceConfig
            {
                CommandChar = null,
                HelpMode = HelpMode.Disable
            });

            //monitor commands for logging
            //stats_collector = new StatsCollector(commandService);

            //add command service
            var commands = client.AddService(commandService);
            
            //create module service
            var modules = client.AddService(new ModuleService());

            //install modules
            modules.Install(new Administration(), "Administration", FilterType.Unrestricted);
            modules.Install(new Conversations(), "Conversations", FilterType.Unrestricted);
            modules.Install(new Gambling(), "Gambling", FilterType.Unrestricted);
            modules.Install(new Games(), "Games", FilterType.Unrestricted);
            //modules.Install(new Music(), "Music", FilterType.Unrestricted);
            modules.Install(new Searches(), "Searches", FilterType.Unrestricted);

            //run the bot
            client.Run(async () =>
            {
                await client.Connect(c.Username, c.Password);
                Console.WriteLine("Connected!");
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }
    }
}