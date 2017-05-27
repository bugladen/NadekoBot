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
using NadekoBot.Modules.Permissions;
using NadekoBot.TypeReaders;
using System.Collections.Immutable;
using System.Diagnostics;
using NadekoBot.Services.Database.Models;
using System.Threading;
using NadekoBot.Modules.Utility;
using NadekoBot.Services.Searches;
using NadekoBot.Services.ClashOfClans;
using NadekoBot.Services.Music;
using NadekoBot.Services.CustomReactions;
using NadekoBot.Services.Games;
using NadekoBot.Services.Administration;

namespace NadekoBot
{
    public class NadekoBot
    {
        private Logger _log;
        
        /* I don't know how to make this not be static
         * and keep the convenience of .WithOkColor
         * and .WithErrorColor extensions methods.
         * I don't want to pass botconfig every time I 
         * want to send a confirm or error message, so
         * I'll keep this for now */
        public static Color OkColor { get; private set; }
        public static Color ErrorColor { get; private set; }
        
        //todo placeholder, will be guild-based
        public static string Prefix { get; } = ".";

        public ImmutableArray<GuildConfig> AllGuildConfigs { get; }
        public BotConfig BotConfig { get; }

        public DiscordShardedClient Client { get; }
        public bool Ready { get; private set; }

        public INServiceProvider Services { get; }

        public NadekoBot()
        {
            SetupLogger();
            _log = LogManager.GetCurrentClassLogger();

            var credentials = new BotCredentials();
            var db = new DbHandler(credentials);
            using (var uow = db.UnitOfWork)
            {
                AllGuildConfigs = uow.GuildConfigs.GetAllGuildConfigs().ToImmutableArray();
                BotConfig = uow.BotConfig.GetOrCreate();
                OkColor = new Color(Convert.ToUInt32(BotConfig.OkColor, 16));
                ErrorColor = new Color(Convert.ToUInt32(BotConfig.ErrorColor, 16));
            }
            
            Client = new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 10,
                LogLevel = LogSeverity.Warning,
                TotalShards = credentials.TotalShards,
                ConnectionTimeout = int.MaxValue,
                AlwaysDownloadUsers = true,
            });

            var google = new GoogleApiService(credentials);
            var localization = new Localization(BotConfig.Locale, AllGuildConfigs.ToDictionary(x => x.GuildId, x => x.Locale), db);
            var strings = new NadekoStrings(localization);

            var greetSettingsService = new GreetSettingsService(Client, AllGuildConfigs, db);

            var commandService = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync,
            });

            var commandHandler = new CommandHandler(Client, commandService, credentials, this);

            var stats = new StatsService(Client, commandHandler, credentials);

            var images = new ImagesService();

            var currencyHandler = new CurrencyHandler(BotConfig, db);

            var soundcloud = new SoundCloudApiService(credentials);

            //module services
            var utilityService = new UtilityService(AllGuildConfigs, Client, BotConfig, db);
            var searchesService = new SearchesService(Client, google, db);
            var clashService = new ClashOfClansService(Client, db, localization, strings);
            var musicService = new MusicService(google, strings, localization, db, soundcloud, credentials);
            var crService = new CustomReactionsService(db, Client);
            var gamesService = new GamesService(Client, BotConfig, AllGuildConfigs, strings, images);
#region administration
            var administrationService = new AdministrationService(AllGuildConfigs, commandHandler);
            var selfService = new SelfService(this, commandHandler, db, BotConfig);
            var vcRoleService = new VcRoleService(Client, AllGuildConfigs);
            var vPlusTService = new VplusTService(Client, AllGuildConfigs, strings, db);
#endregion


            //initialize Services
            Services = new NServiceProvider.ServiceProviderBuilder() //todo all Adds should be interfaces
                .Add<ILocalization>(localization)
                .Add<IStatsService>(stats)
                .Add<IImagesService>(images)
                .Add<IGoogleApiService>(google)
                .Add<IStatsService>(stats)
                .Add<IBotCredentials>(credentials)
                .Add<CommandService>(commandService)
                .Add<NadekoStrings>(strings)
                .Add<DiscordShardedClient>(Client)
                .Add<BotConfig>(BotConfig)
                .Add<CurrencyHandler>(currencyHandler)
                .Add<CommandHandler>(commandHandler)
                .Add<DbHandler>(db)
                //modules
                .Add<UtilityService>(utilityService)
                .Add<SearchesService>(searchesService)
                .Add<ClashOfClansService>(clashService)
                .Add<MusicService>(musicService)
                .Add<GreetSettingsService>(greetSettingsService)
                .Add<CustomReactionsService>(crService)
                .Add<GamesService>(gamesService)
                .Add(selfService)
                .Add(vcRoleService)
                .Add(vPlusTService)
                .Build();

            commandHandler.AddServices(Services);

            //setup typereaders
            commandService.AddTypeReader<PermissionAction>(new PermissionActionTypeReader());
            commandService.AddTypeReader<CommandInfo>(new CommandTypeReader(commandService));
            //commandService.AddTypeReader<CommandOrCrInfo>(new CommandOrCrTypeReader());
            commandService.AddTypeReader<ModuleInfo>(new ModuleTypeReader(commandService));
            commandService.AddTypeReader<ModuleOrCrInfo>(new ModuleOrCrTypeReader(commandService));
            commandService.AddTypeReader<IGuild>(new GuildTypeReader(Client));

#if GLOBAL_NADEKO
            Client.Log += Client_Log;
#endif
        }

        public async Task RunAsync(params string[] args)
        {
            var creds = Services.GetService<IBotCredentials>();
            var stats = Services.GetService<IStatsService>();
            var commandHandler = Services.GetService<CommandHandler>();
            var commandService = Services.GetService<CommandService>();

            _log.Info("Starting NadekoBot v" + StatsService.BotVersion);

            var sw = Stopwatch.StartNew();
            //connect
            await Client.LoginAsync(TokenType.Bot, creds.Token).ConfigureAwait(false);
            await Client.StartAsync().ConfigureAwait(false);
            
            // wait for all shards to be ready
            int readyCount = 0;
            foreach (var s in Client.Shards)
                s.Ready += () => Task.FromResult(Interlocked.Increment(ref readyCount));

            while (readyCount < Client.Shards.Count)
                await Task.Delay(100).ConfigureAwait(false);
            
            stats.Initialize();

            sw.Stop();
            _log.Info("Connected in " + sw.Elapsed.TotalSeconds.ToString("F2"));

            // start handling messages received in commandhandler
            await commandHandler.StartHandling().ConfigureAwait(false);

            var _ = await Task.Run(() => commandService.AddModulesAsync(this.GetType().GetTypeInfo().Assembly)).ConfigureAwait(false);
#if !GLOBAL_NADEKO
            //todo uncomment this
            //await commandService.AddModuleAsync<Music>().ConfigureAwait(false);
#endif
            Ready = true;
            _log.Info(await stats.Print().ConfigureAwait(false));
        }

        private Task Client_Log(LogMessage arg)
        {
            _log.Warn(arg.Source + " | " + arg.Message);
            if (arg.Exception != null)
                _log.Warn(arg.Exception);

            return Task.CompletedTask;
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
                var consoleTarget = new ColoredConsoleTarget()
                {
                    Layout = @"${date:format=HH\:mm\:ss} ${logger} | ${message}"
                };
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
