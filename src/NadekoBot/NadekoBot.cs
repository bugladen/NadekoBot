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
using NadekoBot.Services.Searches;
using NadekoBot.Services.ClashOfClans;
using NadekoBot.Services.Music;
using NadekoBot.Services.CustomReactions;
using NadekoBot.Services.Games;
using NadekoBot.Services.Administration;
using NadekoBot.Services.Permissions;
using NadekoBot.Services.Utility;
using NadekoBot.Services.Help;

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
        public DbService Db { get; }
        public CommandService CommandService { get; }

        public DiscordShardedClient Client { get; }
        public bool Ready { get; private set; }

        public INServiceProvider Services { get; private set; }
        public BotCredentials Credentials { get; }

        public NadekoBot()
        {
            SetupLogger();
            _log = LogManager.GetCurrentClassLogger();

            Credentials = new BotCredentials();
            Db = new DbService(Credentials);

            using (var uow = Db.UnitOfWork)
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
                TotalShards = Credentials.TotalShards,
                ConnectionTimeout = int.MaxValue,
                AlwaysDownloadUsers = true,
            });

            CommandService = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync,
            });

#if GLOBAL_NADEKO
            Client.Log += Client_Log;
#endif
        }

        private void AddServices()
        {
            var googleApiService = new GoogleApiService(Credentials);
            var soundcloudApiService = new SoundCloudApiService(Credentials);
            var localization = new Localization(BotConfig.Locale, AllGuildConfigs.ToDictionary(x => x.GuildId, x => x.Locale), Db);
            var strings = new NadekoStrings(localization);
            var commandHandler = new CommandHandler(Client, CommandService, Credentials, this);
            var stats = new StatsService(Client, commandHandler, Credentials);
            var images = new ImagesService();
            var currencyHandler = new CurrencyService(BotConfig, Db);

            //module services
            //todo 90 - autodiscover, DI, and add instead of manual like this
            #region utility
            var crossServerTextService = new CrossServerTextService(AllGuildConfigs, Client);
            var remindService = new RemindService(Client, BotConfig, Db);
            var repeaterService = new MessageRepeaterService(Client, AllGuildConfigs);
            var converterService = new ConverterService(Db);
            var commandMapService = new CommandMapService(AllGuildConfigs);
            #endregion

            #region Searches
            var searchesService = new SearchesService(Client, googleApiService, Db);
            var streamNotificationService = new StreamNotificationService(Db, Client, strings);
            #endregion

            var clashService = new ClashOfClansService(Client, Db, localization, strings);
            var musicService = new MusicService(googleApiService, strings, localization, Db, soundcloudApiService, Credentials, AllGuildConfigs);
            var crService = new CustomReactionsService(Db, Client);
            var helpService = new HelpService(BotConfig);

            #region Games
            var gamesService = new GamesService(Client, BotConfig, AllGuildConfigs, strings, images);
            var chatterBotService = new ChatterBotService(Client, AllGuildConfigs);
            var pollService = new PollService(Client, strings);
            #endregion

            #region administration
            var administrationService = new AdministrationService(AllGuildConfigs, commandHandler);
            var greetSettingsService = new GreetSettingsService(Client, AllGuildConfigs, Db);
            var selfService = new SelfService(Client, this, commandHandler, Db, BotConfig, localization, strings, Credentials);
            var vcRoleService = new VcRoleService(Client, AllGuildConfigs, Db);
            var vPlusTService = new VplusTService(Client, AllGuildConfigs, strings, Db);
            var muteService = new MuteService(Client, AllGuildConfigs, Db);
            var ratelimitService = new SlowmodeService(AllGuildConfigs);
            var protectionService = new ProtectionService(Client, AllGuildConfigs, muteService);
            var playingRotateService = new PlayingRotateService(Client, BotConfig, musicService);
            var gameVcService = new GameVoiceChannelService(Client, Db, AllGuildConfigs);
            var autoAssignRoleService = new AutoAssignRoleService(Client, AllGuildConfigs);
            var permissionsService = new PermissionsService(Db, BotConfig);
            var blacklistService = new BlacklistService(BotConfig);
            var cmdcdsService = new CmdCdService(AllGuildConfigs);
            var filterService = new FilterService(Client, AllGuildConfigs);
            var globalPermsService = new GlobalPermissionService(BotConfig);
            #endregion

            //initialize Services
            Services = new NServiceProvider.ServiceProviderBuilder()
                .Add<ILocalization>(localization)
                .Add<IStatsService>(stats)
                .Add<IImagesService>(images)
                .Add<IGoogleApiService>(googleApiService)
                .Add<IStatsService>(stats)
                .Add<IBotCredentials>(Credentials)
                .Add<CommandService>(CommandService)
                .Add<NadekoStrings>(strings)
                .Add<DiscordShardedClient>(Client)
                .Add<BotConfig>(BotConfig)
                .Add<CurrencyService>(currencyHandler)
                .Add<CommandHandler>(commandHandler)
                .Add<DbService>(Db)
                //modules
                    .Add(crossServerTextService)
                    .Add(commandMapService)
                    .Add(remindService)
                    .Add(repeaterService)
                    .Add(converterService)
                .Add<SearchesService>(searchesService)
                    .Add(streamNotificationService)
                .Add<ClashOfClansService>(clashService)
                .Add<MusicService>(musicService)
                .Add<GreetSettingsService>(greetSettingsService)
                .Add<CustomReactionsService>(crService)
                .Add<HelpService>(helpService)
                .Add<GamesService>(gamesService)
                    .Add(chatterBotService)
                    .Add(pollService)
                .Add<AdministrationService>(administrationService)
                    .Add(selfService)
                    .Add(vcRoleService)
                    .Add(vPlusTService)
                    .Add(muteService)
                    .Add(ratelimitService)
                    .Add(playingRotateService)
                    .Add(gameVcService)
                    .Add(autoAssignRoleService)
                    .Add(protectionService)
                .Add<PermissionsService>(permissionsService)
                    .Add(blacklistService)
                    .Add(cmdcdsService)
                    .Add(filterService)
                    .Add(globalPermsService)
                .Build();

            commandHandler.AddServices(Services);

            //setup typereaders
            CommandService.AddTypeReader<PermissionAction>(new PermissionActionTypeReader());
            CommandService.AddTypeReader<CommandInfo>(new CommandTypeReader(CommandService));
            CommandService.AddTypeReader<CommandOrCrInfo>(new CommandOrCrTypeReader(crService, CommandService));
            CommandService.AddTypeReader<ModuleInfo>(new ModuleTypeReader(CommandService));
            CommandService.AddTypeReader<ModuleOrCrInfo>(new ModuleOrCrTypeReader(CommandService));
            CommandService.AddTypeReader<IGuild>(new GuildTypeReader(Client));
        }

        private async Task LoginAsync(string token)
        {
            //connect
            await Client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            await Client.StartAsync().ConfigureAwait(false);

            // wait for all shards to be ready
            int readyCount = 0;
            foreach (var s in Client.Shards)
                s.Ready += () => Task.FromResult(Interlocked.Increment(ref readyCount));

            while (readyCount < Client.Shards.Count)
                await Task.Delay(100).ConfigureAwait(false);
        }

        public async Task RunAsync(params string[] args)
        {
            _log.Info("Starting NadekoBot v" + StatsService.BotVersion);

            var sw = Stopwatch.StartNew();

            await LoginAsync(Credentials.Token).ConfigureAwait(false);

            AddServices();

            sw.Stop();
            _log.Info("Connected in " + sw.Elapsed.TotalSeconds.ToString("F2"));

            var stats = Services.GetService<IStatsService>();
            stats.Initialize();
            var commandHandler = Services.GetService<CommandHandler>();
            var CommandService = Services.GetService<CommandService>();

            // start handling messages received in commandhandler
            await commandHandler.StartHandling().ConfigureAwait(false);

            var _ = await CommandService.AddModulesAsync(this.GetType().GetTypeInfo().Assembly);
#if GLOBAL_NADEKO
            //unload modules which are not available on the public bot
            CommandService
                .Modules
                .ToArray()
                .Where(x => x.Preconditions.Any(y => y.GetType() == typeof(NoPublicBot)))
                .ForEach(x => CommandService.RemoveModuleAsync(x));
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
