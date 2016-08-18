using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Impl;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class NadekoBot
    {
        private Logger _log;

        public static CommandService Commands { get; private set; }
        public static DiscordSocketClient Client { get; private set; }
        public static BotConfiguration Config { get; private set; }
        public static Localization Localizer { get; private set; }
        public static BotCredentials Credentials { get; private set; }

        private static YoutubeService Youtube { get; set; }
        public static StatsService Stats { get; private set; }

        public async Task RunAsync(string[] args)
        {
            SetupLogger();

            //create client
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AudioMode = Discord.Audio.AudioMode.Incoming,
                LargeThreshold = 200,
                LogLevel = LogSeverity.Warning,
                MessageCacheSize = 10,
            });

            //initialize Services
            Credentials = new BotCredentials();
            Commands = new CommandService();
            Config = new BotConfiguration();
            Localizer = new Localization();
            Youtube = new YoutubeService();
            Stats = new StatsService(Client);
            _log = LogManager.GetCurrentClassLogger();

            //setup DI
            var depMap = new DependencyMap();
            depMap.Add<ILocalization>(Localizer);
            depMap.Add<IBotConfiguration>(Config);
            depMap.Add<IDiscordClient>(Client);
            depMap.Add<CommandService>(Commands);
            depMap.Add<IYoutubeService>(Youtube);

            //connect
            await Client.LoginAsync(TokenType.Bot, Credentials.Token);
            await Client.ConnectAsync();

            _log.Info("Connected");

            //load commands
            await Commands.LoadAssembly(Assembly.GetEntryAssembly(), depMap);
            Client.MessageReceived += Client_MessageReceived;

            Console.WriteLine(await Stats.Print());

            await Task.Delay(-1);
        }

        private void SetupLogger()
        {
            try
            {
                var logConfig = new LoggingConfiguration();
                var consoleTarget = new ColoredConsoleTarget();

                consoleTarget.Layout = @"${date:format=HH\:mm\:ss} ${logger} | ${message}";

                logConfig.AddTarget("Console", consoleTarget);

                logConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));

                LogManager.Configuration = logConfig;
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private Task Client_MessageReceived(IMessage imsg)
        {
            var throwaway = Task.Run(async () =>
            {
                var t = await Commands.Execute(imsg, imsg.Content);
                if (t.IsSuccess)
                {
                    _log.Info("Command Executed\n\tFull Message: {0}",imsg.Content);
                }
                else if (!t.IsSuccess && t.Error != CommandError.UnknownCommand)
                {
                    _log.Warn("Command errored!\n\tFull Message: {0}\n\tError:{1}", imsg.Content, t.Error);
                }
            });

            return Task.CompletedTask;
        }
    }
}
