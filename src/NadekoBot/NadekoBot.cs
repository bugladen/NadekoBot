using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Impl;
using NLog;
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
using System.IO;
using NadekoBot.Services.Pokemon;
using NadekoBot.DataStructures.ShardCom;

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

        public ImmutableArray<GuildConfig> AllGuildConfigs { get; private set; }
        public BotConfig BotConfig { get; }
        public DbService Db { get; }
        public CommandService CommandService { get; }
        public CommandHandler CommandHandler { get; private set; }
        public Localization Localization { get; private set; }
        public NadekoStrings Strings { get; private set; }
        public StatsService Stats { get; private set; }
        public ImagesService Images { get; }
        public CurrencyService Currency { get; }
        public GoogleApiService GoogleApi { get; }

        public DiscordSocketClient Client { get; }
        public bool Ready { get; private set; }

        public INServiceProvider Services { get; private set; }
        public BotCredentials Credentials { get; }

        private const string _mutexName = @"Global\nadeko_shards_lock";
        private readonly Semaphore sem = new Semaphore(1, 1, _mutexName);
        public int ShardId { get; }
        public ShardsCoordinator ShardCoord { get; private set; }

        private readonly ShardComClient _comClient;

        public NadekoBot(int shardId, int parentProcessId, int? port = null)
        {
            if (shardId < 0)
                throw new ArgumentOutOfRangeException(nameof(shardId));

            ShardId = shardId;

            LogSetup.SetupLogger();
            _log = LogManager.GetCurrentClassLogger();
            TerribleElevatedPermissionCheck();

            Credentials = new BotCredentials();

            port = port ?? Credentials.ShardRunPort;
            _comClient = new ShardComClient(port.Value);

            Db = new DbService(Credentials);

            using (var uow = Db.UnitOfWork)
            {
                BotConfig = uow.BotConfig.GetOrCreate();
                OkColor = new Color(Convert.ToUInt32(BotConfig.OkColor, 16));
                ErrorColor = new Color(Convert.ToUInt32(BotConfig.ErrorColor, 16));
            }
            
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 10,
                LogLevel = LogSeverity.Warning,
                ConnectionTimeout = int.MaxValue,
                TotalShards = Credentials.TotalShards,
                ShardId = shardId,
                AlwaysDownloadUsers = false,
            });

            CommandService = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync,
            });
            
            Images = new ImagesService();
            Currency = new CurrencyService(BotConfig, Db);
            GoogleApi = new GoogleApiService(Credentials);

            SetupShard(shardId, parentProcessId, port.Value);

#if GLOBAL_NADEKO
            Client.Log += Client_Log;
#endif
        }

        private void StartSendingData()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await _comClient.Send(new ShardComMessage()
                    {
                        ConnectionState = Client.ConnectionState,
                        Guilds = Client.ConnectionState == ConnectionState.Connected ? Client.Guilds.Count : 0,
                        ShardId = Client.ShardId,
                    });
                    await Task.Delay(1000);
                }
            });
        }

        private void AddServices()
        {
            var startingGuildIdList = Client.Guilds.Select(x => (long)x.Id).ToList();

            //this unit of work will be used for initialization of all modules too, to prevent multiple queries from running
            using (var uow = Db.UnitOfWork)
            {
                AllGuildConfigs = uow.GuildConfigs.GetAllGuildConfigs(startingGuildIdList).ToImmutableArray();
                
                Localization = new Localization(BotConfig.Locale, AllGuildConfigs.ToDictionary(x => x.GuildId, x => x.Locale), Db);
                Strings = new NadekoStrings(Localization);
                CommandHandler = new CommandHandler(Client, Db, BotConfig, AllGuildConfigs, CommandService, Credentials, this);
                Stats = new StatsService(Client, CommandHandler, Credentials, ShardCoord);

                var soundcloudApiService = new SoundCloudApiService(Credentials);

                #region help
                var helpService = new HelpService(BotConfig, CommandHandler, Strings);
                #endregion

                //module services
                //todo 90 - autodiscover, DI, and add instead of manual like this
                #region utility
                var remindService = new RemindService(Client, BotConfig, Db, startingGuildIdList, uow);
                var repeaterService = new MessageRepeaterService(this, Client, AllGuildConfigs);
                //var converterService = new ConverterService(Db);
                var commandMapService = new CommandMapService(AllGuildConfigs);
                var patreonRewardsService = new PatreonRewardsService(Credentials, Db, Currency, Client);
                var verboseErrorsService = new VerboseErrorsService(AllGuildConfigs, Db, CommandHandler, helpService);
                var pruneService = new PruneService();
                #endregion

                #region permissions
                var permissionsService = new PermissionService(Client, Db, BotConfig, CommandHandler, Strings);
                var blacklistService = new BlacklistService(BotConfig);
                var cmdcdsService = new CmdCdService(AllGuildConfigs);
                var filterService = new FilterService(Client, AllGuildConfigs);
                var globalPermsService = new GlobalPermissionService(BotConfig);
                #endregion

                #region Searches
                var searchesService = new SearchesService(Client, GoogleApi, Db);
                var streamNotificationService = new StreamNotificationService(Db, Client, Strings);
                var animeSearchService = new AnimeSearchService();
                #endregion

                var clashService = new ClashOfClansService(Client, Db, Localization, Strings, uow, startingGuildIdList);
                var musicService = new MusicService(GoogleApi, Strings, Localization, Db, soundcloudApiService, Credentials, AllGuildConfigs);
                var crService = new CustomReactionsService(permissionsService, Db, Strings, Client, CommandHandler, BotConfig, uow);

                #region Games
                var gamesService = new GamesService(Client, BotConfig, AllGuildConfigs, Strings, Images, CommandHandler);
                var chatterBotService = new ChatterBotService(Client, permissionsService, AllGuildConfigs, CommandHandler, Strings);
                var pollService = new PollService(Client, Strings);
                #endregion

                #region administration
                var administrationService = new AdministrationService(AllGuildConfigs, CommandHandler);
                var greetSettingsService = new GreetSettingsService(Client, AllGuildConfigs, Db);
                var selfService = new SelfService(Client, this, CommandHandler, Db, BotConfig, Localization, Strings, Credentials);
                var vcRoleService = new VcRoleService(Client, AllGuildConfigs, Db);
                var vPlusTService = new VplusTService(Client, AllGuildConfigs, Strings, Db);
                var muteService = new MuteService(Client, AllGuildConfigs, Db);
                var ratelimitService = new SlowmodeService(Client, AllGuildConfigs);
                var protectionService = new ProtectionService(Client, AllGuildConfigs, muteService);
                var playingRotateService = new PlayingRotateService(Client, BotConfig, musicService);
                var gameVcService = new GameVoiceChannelService(Client, Db, AllGuildConfigs);
                var autoAssignRoleService = new AutoAssignRoleService(Client, AllGuildConfigs);
                var logCommandService = new LogCommandService(Client, Strings, AllGuildConfigs, Db, muteService, protectionService);
                var guildTimezoneService = new GuildTimezoneService(AllGuildConfigs, Db);
                #endregion

                #region pokemon 
                var pokemonService = new PokemonService();
                #endregion



                //initialize Services
                Services = new NServiceProvider.ServiceProviderBuilder()
                    .Add<ILocalization>(Localization)
                    .Add<IStatsService>(Stats)
                    .Add<IImagesService>(Images)
                    .Add<IGoogleApiService>(GoogleApi)
                    .Add<IStatsService>(Stats)
                    .Add<IBotCredentials>(Credentials)
                    .Add<CommandService>(CommandService)
                    .Add<NadekoStrings>(Strings)
                    .Add<DiscordSocketClient>(Client)
                    .Add<BotConfig>(BotConfig)
                    .Add<CurrencyService>(Currency)
                    .Add<CommandHandler>(CommandHandler)
                    .Add<DbService>(Db)
                        //modules
                        .Add(commandMapService)
                        .Add(remindService)
                        .Add(repeaterService)
                        //.Add(converterService)
                        .Add(verboseErrorsService)
                        .Add(patreonRewardsService)
                        .Add(pruneService)
                    .Add<SearchesService>(searchesService)
                        .Add(streamNotificationService)
                        .Add(animeSearchService)
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
                        .Add(logCommandService)
                        .Add(guildTimezoneService)
                    .Add<PermissionService>(permissionsService)
                        .Add(blacklistService)
                        .Add(cmdcdsService)
                        .Add(filterService)
                        .Add(globalPermsService)
                    .Add<PokemonService>(pokemonService)
                    .Add<NadekoBot>(this)
                    .Build();


                CommandHandler.AddServices(Services);

                //setup typereaders
                CommandService.AddTypeReader<PermissionAction>(new PermissionActionTypeReader());
                CommandService.AddTypeReader<CommandInfo>(new CommandTypeReader(CommandService, CommandHandler));
                CommandService.AddTypeReader<CommandOrCrInfo>(new CommandOrCrTypeReader(crService, CommandService, CommandHandler));
                CommandService.AddTypeReader<ModuleInfo>(new ModuleTypeReader(CommandService));
                CommandService.AddTypeReader<ModuleOrCrInfo>(new ModuleOrCrTypeReader(CommandService));
                CommandService.AddTypeReader<IGuild>(new GuildTypeReader(Client));
                CommandService.AddTypeReader<GuildDateTime>(new GuildDateTimeTypeReader(guildTimezoneService));

            }
        }

        private async Task LoginAsync(string token)
        {
            var clientReady = new TaskCompletionSource<bool>();

            Task SetClientReady()
            {
                var _ = Task.Run(async () =>
                {
                    clientReady.TrySetResult(true);
                    try
                    {
                        foreach (var chan in (await Client.GetDMChannelsAsync()))
                        {
                            await chan.CloseAsync().ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    finally
                    {
                        
                    }
                });
                return Task.CompletedTask;
            }

            //connect
            try { sem.WaitOne(); } catch (AbandonedMutexException) { }

            _log.Info("Shard {0} logging in ...", ShardId);

            try
            {
                await Client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
                await Client.StartAsync().ConfigureAwait(false);
                Client.Ready += SetClientReady;
                await clientReady.Task.ConfigureAwait(false);
                Client.Ready -= SetClientReady;
            }
            finally
            {
                _log.Info("Shard {0} logged in.", ShardId);
                sem.Release();
            }
        }

        public async Task RunAsync(params string[] args)
        {
            if(ShardId == 0)
            _log.Info("Starting NadekoBot v" + StatsService.BotVersion);

            var sw = Stopwatch.StartNew();

            await LoginAsync(Credentials.Token).ConfigureAwait(false);

            _log.Info($"Shard {ShardId} loading services...");
            AddServices();

            sw.Stop();
            _log.Info($"Shard {ShardId} connected in {sw.Elapsed.TotalSeconds:F2}s");

            var stats = Services.GetService<IStatsService>();
            stats.Initialize();
            var commandHandler = Services.GetService<CommandHandler>();
            var CommandService = Services.GetService<CommandService>();

            // start handling messages received in commandhandler
            await commandHandler.StartHandling().ConfigureAwait(false);

            var _ = await CommandService.AddModulesAsync(this.GetType().GetTypeInfo().Assembly);


            //Console.WriteLine(string.Join(", ", CommandService.Commands
            //    .Distinct(x => x.Name + x.Module.Name)
            //    .SelectMany(x => x.Aliases)
            //    .GroupBy(x => x)
            //    .Where(x => x.Count() > 1)
            //    .Select(x => x.Key + $"({x.Count()})")));

//unload modules which are not available on the public bot
#if GLOBAL_NADEKO
            CommandService
                .Modules
                .ToArray()
                .Where(x => x.Preconditions.Any(y => y.GetType() == typeof(NoPublicBot)))
                .ForEach(x => CommandService.RemoveModuleAsync(x));
#endif

            Ready = true;
            _log.Info($"Shard {ShardId} ready.");
            //_log.Info(await stats.Print().ConfigureAwait(false));
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
            StartSendingData();
            if (ShardCoord != null)
                await ShardCoord.RunAndBlockAsync();
            else
            {
                await Task.Delay(-1).ConfigureAwait(false);
            }
        }

        private void TerribleElevatedPermissionCheck()
        {
            try
            {
                File.WriteAllText("test", "test");
                File.Delete("test");
            }
            catch
            {
                _log.Error("You must run the application as an ADMINISTRATOR.");
                Console.ReadKey();
                Environment.Exit(2);
            }
        }

        private void SetupShard(int shardId, int parentProcessId, int port)
        {
            if (shardId != 0)
            {
                new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        var p = Process.GetProcessById(parentProcessId);
                        if (p == null)
                            return;
                        p.WaitForExit();
                    }
                    finally
                    {
                        Environment.Exit(10);
                    }
                })).Start();
            }
            else
            {
                ShardCoord = new ShardsCoordinator(port);
            }
        }
    }
}
