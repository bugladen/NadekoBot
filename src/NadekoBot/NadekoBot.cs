using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Impl;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
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
        public static Localization Localizer { get; private set; }
        public static BotCredentials Credentials { get; private set; }

        public static GoogleApiService Google { get; set; }
        public static StatsService Stats { get; private set; }

        public async Task RunAsync(string[] args)
        {
            SetupLogger();

            //create client
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AudioMode = Discord.Audio.AudioMode.Outgoing,
                LargeThreshold = 200,
                LogLevel = LogSeverity.Warning,
            });

            //initialize Services
            Credentials = new BotCredentials();
            Commands = new CommandService();
            Localizer = new Localization();
            Google = new GoogleApiService();
            Stats = new StatsService(Client);
            _log = LogManager.GetCurrentClassLogger();

            //setup DI
            var depMap = new DependencyMap();
            depMap.Add<ILocalization>(Localizer);
            depMap.Add<DiscordSocketClient>(Client);
            depMap.Add<CommandService>(Commands);
            depMap.Add<IGoogleApiService>(Google);

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

        private Task Client_MessageReceived(IMessage umsg)
        {
            var usrMsg = umsg as IUserMessage;
            if (usrMsg == null)
                return Task.CompletedTask;
            var throwaway = Task.Run(async () =>
            {
                var sw = new Stopwatch();
                sw.Start();
                var t = await Commands.Execute(usrMsg, usrMsg.Content);
                sw.Stop();
                var channel = (umsg.Channel as ITextChannel);
                if (t.IsSuccess)
                {

                    _log.Info("Command Executed after {4}s\n\t" +
                              "User: {0}\n\t" +
                              "Server: {1}\n\t" +
                              "Channel: {2}\n\t" +
                              "Message: {3}",
                              umsg.Author + " [" + umsg.Author.Id + "]", // {0}
                              (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                              (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), //{2}
                              umsg.Content, // {3}
                              sw.Elapsed.TotalSeconds // {4}
                              );
                }
                else if (!t.IsSuccess && t.Error != CommandError.UnknownCommand)
                {
                    _log.Warn("Command Errored after {5}s\n\t" +
                              "User: {0}\n\t" +
                              "Server: {1}\n\t" +
                              "Channel: {2}\n\t" +
                              "Message: {3}\n\t" + 
                              "Error: {4}",
                              umsg.Author + " [" + umsg.Author.Id + "]", // {0}
                              (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                              (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), //{2}
                              umsg.Content,// {3}
                              t.ErrorReason, // {4}
                              sw.Elapsed.TotalSeconds //{5}
                              );
                }
            });

            return Task.CompletedTask;
        }
    }
}
