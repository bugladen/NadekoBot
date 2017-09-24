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
using System.Collections.Immutable;
using System.Diagnostics;
using NadekoBot.Services.Database.Models;
using System.Threading;
using System.IO;
using NadekoBot.Extensions;
using System.Collections.Generic;
using NadekoBot.Common;
using NadekoBot.Common.ShardCom;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Services.Database;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace NadekoBot
{
    public class NadekoBot
    {
        private Logger _log;

        public BotCredentials Credentials { get; }

        public DiscordSocketClient Client { get; }
        public CommandService CommandService { get; }

        private readonly DbService _db;
        public ImmutableArray<GuildConfig> AllGuildConfigs { get; private set; }

        /* I don't know how to make this not be static
         * and keep the convenience of .WithOkColor
         * and .WithErrorColor extensions methods.
         * I don't want to pass botconfig every time I 
         * want to send a confirm or error message, so
         * I'll keep this for now */
        public static Color OkColor { get; private set; }
        public static Color ErrorColor { get; private set; }

        public TaskCompletionSource<bool> Ready { get; private set; } = new TaskCompletionSource<bool>();

        public INServiceProvider Services { get; private set; }

        public ShardsCoordinator ShardCoord { get; private set; }

        private readonly ShardComClient _comClient;

        private readonly BotConfig _botConfig;

        public NadekoBot(int shardId, int parentProcessId, int? port = null)
        {
            if (shardId < 0)
                throw new ArgumentOutOfRangeException(nameof(shardId));

            //var obj = JsonConvert.DeserializeObject<Dictionary<string, CommandData2>>(File.ReadAllText("./data/command_strings.json"))
            //    .ToDictionary(x => x.Key, x => new CommandData2
            //    {
            //        Cmd = x.Value.Cmd,
            //        Desc = x.Value.Desc,
            //        Usage = x.Value.Usage.Select(y => y.Substring(1, y.Length - 2)).ToArray(),
            //    });

            //File.WriteAllText("./data/command_strings.json", JsonConvert.SerializeObject(obj, Formatting.Indented));
            

            LogSetup.SetupLogger();
            _log = LogManager.GetCurrentClassLogger();
            TerribleElevatedPermissionCheck();

            Credentials = new BotCredentials();
            _db = new DbService(Credentials);
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

            port = port ?? Credentials.ShardRunPort;
            _comClient = new ShardComClient(port.Value);

            using (var uow = _db.UnitOfWork)
            {
                _botConfig = uow.BotConfig.GetOrCreate();
                OkColor = new Color(Convert.ToUInt32(_botConfig.OkColor, 16));
                ErrorColor = new Color(Convert.ToUInt32(_botConfig.ErrorColor, 16));
            }

            SetupShard(parentProcessId, port.Value);

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
                        Time = DateTime.UtcNow,
                    });
                    await Task.Delay(5000);
                }
            });
        }

        private void AddServices()
        {
            var startingGuildIdList = Client.Guilds.Select(x => (long)x.Id).ToList();

            //this unit of work will be used for initialization of all modules too, to prevent multiple queries from running
            using (var uow = _db.UnitOfWork)
            {
                AllGuildConfigs = uow.GuildConfigs.GetAllGuildConfigs(startingGuildIdList).ToImmutableArray();

                IBotConfigProvider botConfigProvider = new BotConfigProvider(_db, _botConfig);

                //var localization = new Localization(_botConfig.Locale, AllGuildConfigs.ToDictionary(x => x.GuildId, x => x.Locale), Db);

                //initialize Services
                Services = new NServiceProvider.ServiceProviderBuilder()
                    .AddManual<IBotCredentials>(Credentials)
                    .AddManual(_db)
                    .AddManual(Client)
                    .AddManual(CommandService)
                    .AddManual(botConfigProvider)
                    //.AddManual<ILocalization>(localization)
                    .AddManual<IEnumerable<GuildConfig>>(AllGuildConfigs) //todo wrap this
                    .AddManual<NadekoBot>(this)
                    .AddManual<IUnitOfWork>(uow)
                    .AddManual<IDataCache>(new RedisCache(Client.CurrentUser.Id))
                    .LoadFrom(Assembly.GetEntryAssembly())
                    .Build();

                var commandHandler = Services.GetService<CommandHandler>();
                commandHandler.AddServices(Services);

                //setup typereaders
                CommandService.AddTypeReader<PermissionAction>(new PermissionActionTypeReader());
                CommandService.AddTypeReader<CommandInfo>(new CommandTypeReader());
                CommandService.AddTypeReader<CommandOrCrInfo>(new CommandOrCrTypeReader());
                CommandService.AddTypeReader<ModuleInfo>(new ModuleTypeReader(CommandService));
                CommandService.AddTypeReader<ModuleOrCrInfo>(new ModuleOrCrTypeReader(CommandService));
                CommandService.AddTypeReader<IGuild>(new GuildTypeReader(Client));
                CommandService.AddTypeReader<GuildDateTime>(new GuildDateTimeTypeReader());
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
            _log.Info("Shard {0} logging in ...", Client.ShardId);
            await Client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            await Client.StartAsync().ConfigureAwait(false);
            Client.Ready += SetClientReady;
            await clientReady.Task.ConfigureAwait(false);
            Client.Ready -= SetClientReady;
            Client.JoinedGuild += Client_JoinedGuild;
            Client.LeftGuild += Client_LeftGuild;
            _log.Info("Shard {0} logged in.", Client.ShardId);
        }

        private Task Client_LeftGuild(SocketGuild arg)
        {
            _log.Info("Left server: {0} [{1}]", arg?.Name, arg?.Id);
            return Task.CompletedTask;
        }

        private Task Client_JoinedGuild(SocketGuild arg)
        {
            _log.Info("Joined server: {0} [{1}]", arg?.Name, arg?.Id);
            return Task.CompletedTask;
        }

        public async Task RunAsync(params string[] args)
        {
            if(Client.ShardId == 0)
            _log.Info("Starting NadekoBot v" + StatsService.BotVersion);

            var sw = Stopwatch.StartNew();

            await LoginAsync(Credentials.Token).ConfigureAwait(false);

            _log.Info($"Shard {Client.ShardId} loading services...");
            AddServices();

            sw.Stop();
            _log.Info($"Shard {Client.ShardId} connected in {sw.Elapsed.TotalSeconds:F2}s");

            var stats = Services.GetService<IStatsService>();
            stats.Initialize();
            var commandHandler = Services.GetService<CommandHandler>();
            var CommandService = Services.GetService<CommandService>();

            // start handling messages received in commandhandler
            await commandHandler.StartHandling().ConfigureAwait(false);

            var _ = await CommandService.AddModulesAsync(this.GetType().GetTypeInfo().Assembly);


            bool isPublicNadeko = false;
#if GLOBAL_NADEKO
            isPublicNadeko = true;
#endif
            //unload modules which are not available on the public bot

            if(isPublicNadeko)
                CommandService
                    .Modules
                    .ToArray()
                    .Where(x => x.Preconditions.Any(y => y.GetType() == typeof(NoPublicBot)))
                    .ForEach(x => CommandService.RemoveModuleAsync(x));

            Ready.TrySetResult(true);
            HandleStatusChanges();
            _log.Info($"Shard {Client.ShardId} ready.");
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

        private void SetupShard(int parentProcessId, int port)
        {
            if (Client.ShardId == 0)
            {
                ShardCoord = new ShardsCoordinator(port);
                return;
            }
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

        private void HandleStatusChanges()
        {
            var sub = Services.GetService<IDataCache>().Redis.GetSubscriber();
            sub.Subscribe(Client.CurrentUser.Id + "_status.game_set", async (ch, game) =>
            {
                try
                {
                    var obj = new { Name = default(string) };
                    obj = JsonConvert.DeserializeAnonymousType(game, obj);
                    await Client.SetGameAsync(obj.Name).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }, CommandFlags.FireAndForget);

            sub.Subscribe(Client.CurrentUser.Id + "_status.stream_set", async (ch, streamData) =>
            {
                try
                {
                    var obj = new { Name = "", Url = "" };
                    obj = JsonConvert.DeserializeAnonymousType(streamData, obj);
                    await Client.SetGameAsync(obj.Name, obj.Url, StreamType.Twitch).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }, CommandFlags.FireAndForget);
        }

        public Task SetGameAsync(string game)
        {
            var obj = new { Name = game };
            var sub = Services.GetService<IDataCache>().Redis.GetSubscriber();
            return sub.PublishAsync(Client.CurrentUser.Id + "_status.game_set", JsonConvert.SerializeObject(obj));
        }

        public Task SetStreamAsync(string name, string url)
        {
            var obj = new { Name = name, Url = url };
            var sub = Services.GetService<IDataCache>().Redis.GetSubscriber();
            return sub.PublishAsync(Client.CurrentUser.Id + "_status.game_set", JsonConvert.SerializeObject(obj));
        }
    }
}
