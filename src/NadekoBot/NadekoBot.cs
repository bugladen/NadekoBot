using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Impl;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NadekoBot.Modules.Permissions;
using Module = Discord.Commands.Module;
using NadekoBot.TypeReaders;

namespace NadekoBot
{
    public class NadekoBot
    {
        private Logger _log;

        public static CommandService CommandService { get; private set; }
        public static CommandHandler CommandHandler { get; private set; }
        public static DiscordSocketClient Client { get; private set; }
        public static Localization Localizer { get; private set; }
        public static BotCredentials Credentials { get; private set; }

        public static GoogleApiService Google { get; private set; }
        public static StatsService Stats { get; private set; }

        public static IReadOnlyDictionary<string, string> ModulePrefixes { get; private set; }

        public async Task RunAsync(string[] args)
        {
            SetupLogger();
            _log = LogManager.GetCurrentClassLogger();

            _log.Info("Starting NadekoBot v" + typeof(NadekoBot).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

            //create client
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AudioMode = Discord.Audio.AudioMode.Outgoing,
                MessageCacheSize = 10,
                LogLevel = LogSeverity.Warning,
            });

            //initialize Services
            Credentials = new BotCredentials();
            CommandService = new CommandService();
            Localizer = new Localization();
            Google = new GoogleApiService();
            CommandHandler = new CommandHandler(Client, CommandService);
            Stats = new StatsService(Client, CommandHandler);

            //setup DI
            var depMap = new DependencyMap();
            depMap.Add<ILocalization>(Localizer);
            depMap.Add<DiscordSocketClient>(Client);
            depMap.Add<CommandService>(CommandService);
            depMap.Add<IGoogleApiService>(Google);


            //setup typereaders
            CommandService.AddTypeReader<PermissionAction>(new PermissionActionTypeReader());
            CommandService.AddTypeReader<Command>(new CommandTypeReader());
            CommandService.AddTypeReader<Module>(new ModuleTypeReader());

            //connect
            await Client.LoginAsync(TokenType.Bot, Credentials.Token).ConfigureAwait(false);
            await Client.ConnectAsync().ConfigureAwait(false);
            await Client.DownloadAllUsersAsync().ConfigureAwait(false);

            _log.Info("Connected");

            //load commands and prefixes
            using (var uow = DbHandler.UnitOfWork())
            {
                ModulePrefixes = new ReadOnlyDictionary<string, string>(uow.BotConfig.GetOrCreate().ModulePrefixes.ToDictionary(m => m.ModuleName, m => m.Prefix));
            }
            await CommandService.LoadAssembly(Assembly.GetEntryAssembly(), depMap).ConfigureAwait(false);

            Console.WriteLine(await Stats.Print().ConfigureAwait(false));

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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
