using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Common;
using NadekoBot.Common.ShardCom;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

        public IServiceProvider Services { get; private set; }

        private readonly BotConfig _botConfig;
        public IDataCache Cache { get; private set; }

        public int GuildCount =>
            Cache.Redis.GetDatabase()
                .ListRange(Credentials.RedisKey() + "_shardstats")
                .Select(x => JsonConvert.DeserializeObject<ShardComMessage>(x))
                .Sum(x => x.Guilds);

        public event Func<GuildConfig, Task> JoinedGuild = delegate { return Task.CompletedTask; };

        public NadekoBot(int shardId, int parentProcessId)
        {
            if (shardId < 0)
                throw new ArgumentOutOfRangeException(nameof(shardId));

            LogSetup.SetupLogger(shardId);
            _log = LogManager.GetCurrentClassLogger();
            TerribleElevatedPermissionCheck();

            Credentials = new BotCredentials();
            Cache = new RedisCache(Credentials, shardId);
            _db = new DbService(Credentials);

            if (shardId == 0)
            {
                _db.Setup();
            }

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
#if GLOBAL_NADEKO
                MessageCacheSize = 0,
#else
                MessageCacheSize = 50,
#endif
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

            using (var uow = _db.GetDbContext())
            {
                _botConfig = uow.BotConfig.GetOrCreate();
                OkColor = new Color(Convert.ToUInt32(_botConfig.OkColor, 16));
                ErrorColor = new Color(Convert.ToUInt32(_botConfig.ErrorColor, 16));
                uow.SaveChanges();
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
                    await Task.Delay(7500).ConfigureAwait(false);
                }
            });
        }

        private List<ulong> GetCurrentGuildIds()
        {
            return Client.Guilds.Select(x => x.Id).ToList();
        }

        public IEnumerable<GuildConfig> GetCurrentGuildConfigs()
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.GuildConfigs.GetAllGuildConfigs(GetCurrentGuildIds()).ToImmutableArray();
            }
        }

        private void AddServices()
        {
            var startingGuildIdList = GetCurrentGuildIds();

            //this unit of work will be used for initialization of all modules too, to prevent multiple queries from running
            using (var uow = _db.GetDbContext())
            {
                var sw = Stopwatch.StartNew();

                var _bot = Client.CurrentUser;

                uow.DiscordUsers.EnsureCreated(_bot.Id, _bot.Username, _bot.Discriminator, _bot.AvatarId);

                AllGuildConfigs = uow.GuildConfigs.GetAllGuildConfigs(startingGuildIdList).ToImmutableArray();

                IBotConfigProvider botConfigProvider = new BotConfigProvider(_db, _botConfig, Cache);

                var s = new ServiceCollection()
                    .AddSingleton<IBotCredentials>(Credentials)
                    .AddSingleton(_db)
                    .AddSingleton(Client)
                    .AddSingleton(CommandService)
                    .AddSingleton(botConfigProvider)
                    .AddSingleton(this)
                    .AddSingleton(uow)
                    .AddSingleton(Cache);

                s.AddHttpClient();
                s.AddHttpClient("memelist").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = false
                });

                s.LoadFrom(Assembly.GetAssembly(typeof(CommandHandler)));

                //initialize Services
                Services = s.BuildServiceProvider();
                var commandHandler = Services.GetService<CommandHandler>();
                //what the fluff
                commandHandler.AddServices(s);
                LoadTypeReaders(typeof(NadekoBot).Assembly);

                sw.Stop();
                _log.Info($"All services loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }
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
                _log.Warn(ex.LoaderExceptions[0]);
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
                        foreach (var chan in (await Client.GetDMChannelsAsync().ConfigureAwait(false)))
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
                using (var uow = _db.GetDbContext())
                {
                    gc = uow.GuildConfigs.ForId(arg.Id);
                }
                await JoinedGuild.Invoke(gc).ConfigureAwait(false);
            });
            return Task.CompletedTask;
        }

        public async Task RunAsync()
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

            var _ = await CommandService.AddModulesAsync(this.GetType().GetTypeInfo().Assembly, Services)
                .ConfigureAwait(false);

            bool isPublicNadeko = false;
#if GLOBAL_NADEKO
            isPublicNadeko = true;
#endif
            //unload modules which are not available on the public bot

            if (isPublicNadeko)
                CommandService
                    .Modules
                    .ToArray()
                    .Where(x => x.Preconditions.Any(y => y.GetType() == typeof(NoPublicBotAttribute)))
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

        public async Task RunAndBlockAsync()
        {
            await RunAsync().ConfigureAwait(false);
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private void TerribleElevatedPermissionCheck()
        {
            try
            {
                var rng = new NadekoRandom().Next(100000, 1000000);
                var str = rng.ToString();
                File.WriteAllText(str, str);
                File.Delete(str);
            }
            catch
            {
                _log.Error("You must run the application as an ADMINISTRATOR.");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(2);
            }
        }

        private static void SetupShard(int parentProcessId)
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

        public Task SetStreamAsync(string name, string link)
        {
            var obj = new { Name = name, Url = link };
            var sub = Services.GetService<IDataCache>().Redis.GetSubscriber();
            return sub.PublishAsync(Client.CurrentUser.Id + "_status.stream_set", JsonConvert.SerializeObject(obj));
        }
    }
}
