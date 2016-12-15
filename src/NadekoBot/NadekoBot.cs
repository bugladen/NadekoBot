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
using System.Collections.Generic;
using NadekoBot.Modules.Permissions;
using Module = Discord.Commands.Module;
using NadekoBot.TypeReaders;
using System.Collections.Concurrent;
using NadekoBot.Modules.Music;
using NadekoBot.Services.Database.Models;
using NadekoBot.Modules.Games.Commands.Hangman;

namespace NadekoBot
{
    public class NadekoBot
    {
        private Logger _log;
        
        public static uint OkColor { get; } = 0x71cd40;
        public static uint ErrorColor { get; } = 0xee281f;

        public static CommandService CommandService { get; private set; }
        public static CommandHandler CommandHandler { get; private set; }
        public static ShardedDiscordClient  Client { get; private set; }
        public static BotCredentials Credentials { get; private set; }

        public static GoogleApiService Google { get; private set; }
        public static StatsService Stats { get; private set; }

        public static ConcurrentDictionary<string, string> ModulePrefixes { get; private set; }
        public static bool Ready { get; private set; }

        public static IEnumerable<GuildConfig> AllGuildConfigs { get; }

        static NadekoBot()
        {
            SetupLogger();
            Credentials = new BotCredentials();

            using (var uow = DbHandler.UnitOfWork())
            {
                AllGuildConfigs = uow.GuildConfigs.GetAll();
            }
        }

        public async Task RunAsync(params string[] args)
        {
            _log = LogManager.GetCurrentClassLogger();

            _log.Info("Starting NadekoBot v" + StatsService.BotVersion);

            //create client
            Client = new ShardedDiscordClient(new DiscordSocketConfig
            {
                AudioMode = Discord.Audio.AudioMode.Outgoing,
                MessageCacheSize = 10,
                LogLevel = LogSeverity.Warning,
                TotalShards = Credentials.TotalShards,
                ConnectionTimeout = int.MaxValue
            });

            //initialize Services
            CommandService = new CommandService();
            Google = new GoogleApiService();
            CommandHandler = new CommandHandler(Client, CommandService);
            Stats = new StatsService(Client, CommandHandler);

            ////setup DI
            //var depMap = new DependencyMap();
            //depMap.Add<ILocalization>(Localizer);
            //depMap.Add<ShardedDiscordClient>(Client);
            //depMap.Add<CommandService>(CommandService);
            //depMap.Add<IGoogleApiService>(Google);


            //setup typereaders
            CommandService.AddTypeReader<PermissionAction>(new PermissionActionTypeReader());
            CommandService.AddTypeReader<Command>(new CommandTypeReader());
            CommandService.AddTypeReader<Module>(new ModuleTypeReader());
            CommandService.AddTypeReader<IGuild>(new GuildTypeReader());

            //connect
            await Client.LoginAsync(TokenType.Bot, Credentials.Token).ConfigureAwait(false);
            await Client.ConnectAsync().ConfigureAwait(false);
            await Client.DownloadAllUsersAsync().ConfigureAwait(false);

            _log.Info("Connected");

            //load commands and prefixes
            using (var uow = DbHandler.UnitOfWork())
            {
                ModulePrefixes = new ConcurrentDictionary<string, string>(uow.BotConfig.GetOrCreate().ModulePrefixes.ToDictionary(m => m.ModuleName, m => m.Prefix));
            }
            // start handling messages received in commandhandler
            await CommandHandler.StartHandling().ConfigureAwait(false);

            await CommandService.LoadAssembly(this.GetType().GetTypeInfo().Assembly).ConfigureAwait(false);
#if !GLOBAL_NADEKO
            await CommandService.Load(new Music()).ConfigureAwait(false);
#endif
            Ready = true;
            Console.WriteLine(await Stats.Print().ConfigureAwait(false));
        }

        public async Task RunAndBlockAsync(params string[] args)
        {
            await RunAsync(args).ConfigureAwait(false);
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static void SetupLogger()
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
