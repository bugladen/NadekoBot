using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Impl;
using NLog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Diagnostics;
using NadekoBot.Core.Services.Database.Models;
using System.Threading;
using System.IO;
using NadekoBot.Extensions;
using System.Collections.Generic;
using NadekoBot.Common;
using NadekoBot.Common.ShardCom;
using NadekoBot.Core.Services.Database;
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
        public static Color OkColor { get; set; }
        public static Color ErrorColor { get; set; }

        public TaskCompletionSource<bool> Ready { get; private set; } = new TaskCompletionSource<bool>();

        public INServiceProvider Services { get; private set; }

        private readonly BotConfig _botConfig;
        public IDataCache Cache { get; private set; }

        public int GuildCount =>
            Cache.Redis.GetDatabase()
                .ListRange(Credentials.RedisKey() + "_shardstats")
                .Select(x => JsonConvert.DeserializeObject<ShardComMessage>(x))
                .Sum(x => x.Guilds);

        public int[] ShardGuildCounts =>
            Cache.Redis.GetDatabase()
                .ListRange(Credentials.RedisKey() + "_shardstats")
                .Select(x => JsonConvert.DeserializeObject<ShardComMessage>(x))
                .OrderBy(x => x.ShardId)
                .Select(x => x.Guilds)
                .ToArray();

        public event Func<GuildConfig, Task> JoinedGuild = delegate { return Task.CompletedTask; };

        public NadekoBot(int shardId, int parentProcessId)
        {
            if (shardId < 0)
                throw new ArgumentOutOfRangeException(nameof(shardId));

            LogSetup.SetupLogger(shardId);
            _log = LogManager.GetCurrentClassLogger();
            TerribleElevatedPermissionCheck();

            Credentials = new BotCredentials();
            Cache = new RedisCache(Credentials);
            _db = new DbService(Credentials);
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 10,
                LogLevel = LogSeverity.Info,
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

            using (var uow = _db.UnitOfWork)
            {
                _botConfig = uow.BotConfig.GetOrCreate();
                OkColor = new Color(Convert.ToUInt32(_botConfig.OkColor, 16));
                ErrorColor = new Color(Convert.ToUInt32(_botConfig.ErrorColor, 16));
                uow.Complete();
            }

            SetupShard(parentProcessId);

#if GLOBAL_NADEKO || DEBUG
            Client.Log += Client_Log;
#endif
        }

        private void StartSendingData()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var data = new ShardComMessage()
                    {
                        ConnectionState = Client.ConnectionState,
                        Guilds = Client.ConnectionState == ConnectionState.Connected ? Client.Guilds.Count : 0,
                        ShardId = Client.ShardId,
                        Time = DateTime.UtcNow,
                    };

                    var sub = Cache.Redis.GetSubscriber();
                    var msg = JsonConvert.SerializeObject(data);

                    await sub.PublishAsync(Credentials.RedisKey() + "_shardcoord_send", msg).ConfigureAwait(false);
                    await Task.Delay(7500);
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

                IBotConfigProvider botConfigProvider = new BotConfigProvider(_db, _botConfig, Cache);

                //initialize Services
                Services = new NServiceProvider()
                    .AddManual<IBotCredentials>(Credentials)
                    .AddManual(_db)
                    .AddManual(Client)
                    .AddManual(CommandService)
                    .AddManual(botConfigProvider)
                    .AddManual<NadekoBot>(this)
                    .AddManual<IUnitOfWork>(uow)
                    .AddManual<IDataCache>(Cache);

                Services.LoadFrom(Assembly.GetAssembly(typeof(CommandHandler)));

                var commandHandler = Services.GetService<CommandHandler>();
                commandHandler.AddServices(Services);

                LoadTypeReaders(typeof(NadekoBot).Assembly);
            }
            Services.Unload(typeof(IUnitOfWork)); // unload it after the startup
        }

        private IEnumerable<object> LoadTypeReaders(Assembly assembly)
        {
            Type[] allTypes;
            try
            {
                allTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine(ex.LoaderExceptions[0]);
                return Enumerable.Empty<object>();
            }
            var filteredTypes = allTypes
                .Where(x => x.IsSubclassOf(typeof(TypeReader))
                    && x.BaseType.GetGenericArguments().Length > 0
                    && !x.IsAbstract);

            var toReturn = new List<object>();
            foreach (var ft in filteredTypes)
            {
                var x = (TypeReader)Activator.CreateInstance(ft, Client, CommandService);
                var baseType = ft.BaseType;
                var typeArgs = baseType.GetGenericArguments();
                try
                {
                    CommandService.AddTypeReader(typeArgs[0], x);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    throw;
                }
                toReturn.Add(x);
            }

            return toReturn;
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
            var _ = Task.Run(async () =>
            {
                GuildConfig gc;
                using (var uow = _db.UnitOfWork)
                {
                    gc = uow.GuildConfigs.For(arg.Id);
                }
                await JoinedGuild.Invoke(gc);
            });
            return Task.CompletedTask;
        }

        public async Task RunAsync(params string[] args)
        {
            var sw = Stopwatch.StartNew();

            await LoginAsync(Credentials.Token).ConfigureAwait(false);

            _log.Info($"Shard {Client.ShardId} loading services...");
            try
            {
                AddServices();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }

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

            if (isPublicNadeko)
                CommandService
                    .Modules
                    .ToArray()
                    .Where(x => x.Preconditions.Any(y => y.GetType() == typeof(NoPublicBot)))
                    .ForEach(x => CommandService.RemoveModuleAsync(x));

            HandleStatusChanges();
            StartSendingData();
            Ready.TrySetResult(true);
            _log.Info($"Shard {Client.ShardId} ready.");
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

        private void SetupShard(int parentProcessId)
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

        private void HandleStatusChanges()
        {
            var sub = Services.GetService<IDataCache>().Redis.GetSubscriber();
            sub.Subscribe(Client.CurrentUser.Id + "_status.game_set", async (ch, game) =>
            {
                try
                {
                    var obj = new { Name = default(string), Activity = ActivityType.Playing };
                    obj = JsonConvert.DeserializeAnonymousType(game, obj);
                    await Client.SetGameAsync(obj.Name, type: obj.Activity).ConfigureAwait(false);
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
                    await Client.SetGameAsync(obj.Name, obj.Url, ActivityType.Streaming).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }, CommandFlags.FireAndForget);
        }

        public Task SetGameAsync(string game, ActivityType type)
        {
            var obj = new { Name = game, Activity = type };
            var sub = Services.GetService<IDataCache>().Redis.GetSubscriber();
            return sub.PublishAsync(Client.CurrentUser.Id + "_status.game_set", JsonConvert.SerializeObject(obj));
        }

        public Task SetStreamAsync(string name, string url)
        {
            var obj = new { Name = name, Url = url };
            var sub = Services.GetService<IDataCache>().Redis.GetSubscriber();
            return sub.PublishAsync(Client.CurrentUser.Id + "_status.stream_set", JsonConvert.SerializeObject(obj));
        }

        //private readonly Dictionary<string, (IEnumerable<ModuleInfo> Modules, IEnumerable<Type> Types)> _loadedPackages = new Dictionary<string, (IEnumerable<ModuleInfo>, IEnumerable<Type>)>();
        //private readonly SemaphoreSlim _packageLocker = new SemaphoreSlim(1, 1);
        //public IEnumerable<string> LoadedPackages => _loadedPackages.Keys;

        ///// <summary>
        ///// Unloads a package
        ///// </summary>
        ///// <param name="name">Package name. Case sensitive.</param>
        ///// <returns>Whether the unload is successful.</returns>
        //public async Task<bool> UnloadPackage(string name)
        //{
        //    await _packageLocker.WaitAsync().ConfigureAwait(false);
        //    try
        //    {
        //        if (!_loadedPackages.Remove(name, out var data))
        //            return false;

        //        var modules = data.Modules;
        //        var types = data.Types;

        //        var i = 0;
        //        foreach (var m in modules)
        //        {
        //            await CommandService.RemoveModuleAsync(m).ConfigureAwait(false);
        //            i++;
        //        }
        //        _log.Info("Unloaded {0} modules.", i);

        //        if (types != null && types.Any())
        //        {
        //            i = 0;
        //            foreach (var t in types)
        //            {
        //                var obj = Services.Unload(t);
        //                if (obj is IUnloadableService s)
        //                    await s.Unload().ConfigureAwait(false);
        //                i++;
        //            }

        //            _log.Info("Unloaded {0} types.", i);
        //        }
        //        using (var uow = _db.UnitOfWork)
        //        {
        //            uow.BotConfig.GetOrCreate().LoadedPackages.Remove(new LoadedPackage
        //            {
        //                Name = name,
        //            });
        //        }
        //        return true;
        //    }
        //    finally
        //    {
        //        _packageLocker.Release();
        //    }
        //}
        ///// <summary>
        ///// Loads a package
        ///// </summary>
        ///// <param name="name">Name of the package to load. Case sensitive.</param>
        ///// <returns>Whether the load is successful.</returns>
        //public async Task<bool> LoadPackage(string name)
        //{
        //    await _packageLocker.WaitAsync().ConfigureAwait(false);
        //    try
        //    {
        //        if (_loadedPackages.ContainsKey(name))
        //            return false;

        //        var startingGuildIdList = Client.Guilds.Select(x => (long)x.Id).ToList();
        //        using (var uow = _db.UnitOfWork)
        //        {
        //            AllGuildConfigs = uow.GuildConfigs.GetAllGuildConfigs(startingGuildIdList).ToImmutableArray();
        //        }

        //        var domain = new Context();
        //        var package = domain.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory,
        //                                        "modules",
        //                                        $"NadekoBot.Modules.{name}",
        //                                        $"NadekoBot.Modules.{name}.dll"));
        //        //var package = Assembly.LoadFile(Path.Combine(AppContext.BaseDirectory,
        //        //                                "modules",
        //        //                                $"NadekoBot.Modules.{name}",
        //        //                                $"NadekoBot.Modules.{name}.dll"));
        //        var types = Services.LoadFrom(package);
        //        var added = await CommandService.AddModulesAsync(package).ConfigureAwait(false);
        //        var trs = LoadTypeReaders(package); 
        //        /* i don't have to unload typereaders
        //         * (and there's no api for it)
        //         * because they get overwritten anyway, and since 
        //         * the only time I'd unload typereaders, is when unloading a module
        //         * which means they won't have a chance to be used
        //         * */
        //        _log.Info("Loaded {0} modules and {1} types.", added.Count(), types.Count());
        //        _loadedPackages.Add(name, (added, types));
        //        using (var uow = _db.UnitOfWork)
        //        {
        //            uow.BotConfig.GetOrCreate().LoadedPackages.Add(new LoadedPackage
        //            {
        //                Name = name,
        //            });
        //        }
        //        return true;
        //    }
        //    finally
        //    {
        //        _packageLocker.Release();
        //    }
        //}
    }
}
