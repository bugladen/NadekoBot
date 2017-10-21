using Newtonsoft.Json;
using System;
using System.IO;
using Discord;
using System.Linq;
using NLog;
using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;
using NadekoBot.Common;

namespace NadekoBot.Core.Services.Impl
{
    public class BotCredentials : IBotCredentials
    {
        private Logger _log;

        public ulong ClientId { get; }
        public string GoogleApiKey { get; }
        public string MashapeKey { get; }
        public string Token { get; }

        public ImmutableArray<ulong> OwnerIds { get; }

        public string LoLApiKey { get; }
        public string OsuApiKey { get; }
        public string CleverbotApiKey { get; }
        public RestartConfig RestartCommand { get; }
        public DBConfig Db { get; }
        public int TotalShards { get; }
        public string CarbonKey { get; }

        private readonly string _credsFileName = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
        public string PatreonAccessToken { get; }
        public string ShardRunCommand { get; }
        public string ShardRunArguments { get; }
        public int ShardRunPort { get; }

        public string PatreonCampaignId { get; }

        public BotCredentials()
        {
            _log = LogManager.GetCurrentClassLogger();

            try { File.WriteAllText("./credentials_example.json", JsonConvert.SerializeObject(new CredentialsModel(), Formatting.Indented)); } catch { }
            if(!File.Exists(_credsFileName))
                _log.Warn($"credentials.json is missing. Attempting to load creds from environment variables prefixed with 'NadekoBot_'. Example is in {Path.GetFullPath("./credentials_example.json")}");
            try
            {
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddJsonFile(_credsFileName, true)
                    .AddEnvironmentVariables("NadekoBot_");

                var data = configBuilder.Build();

                Token = data[nameof(Token)];
                if (string.IsNullOrWhiteSpace(Token))
                {
                    _log.Error("Token is missing from credentials.json or Environment varibles. Add it and restart the program.");
                    Console.ReadKey();
                    Environment.Exit(3);
                }
                OwnerIds = data.GetSection("OwnerIds").GetChildren().Select(c => ulong.Parse(c.Value)).ToImmutableArray();
                LoLApiKey = data[nameof(LoLApiKey)];
                GoogleApiKey = data[nameof(GoogleApiKey)];
                MashapeKey = data[nameof(MashapeKey)];
                OsuApiKey = data[nameof(OsuApiKey)];
                PatreonAccessToken = data[nameof(PatreonAccessToken)];
                PatreonCampaignId = data[nameof(PatreonCampaignId)] ?? "334038";
                ShardRunCommand = data[nameof(ShardRunCommand)];
                ShardRunArguments = data[nameof(ShardRunArguments)];
                CleverbotApiKey = data[nameof(CleverbotApiKey)];

                var restartSection = data.GetSection(nameof(RestartCommand));
                var cmd = restartSection["cmd"];
                var args = restartSection["args"];
                if (!string.IsNullOrWhiteSpace(cmd))
                    RestartCommand = new RestartConfig(cmd, args);

                if (string.IsNullOrWhiteSpace(ShardRunCommand))
                    ShardRunCommand = "dotnet";
                if (string.IsNullOrWhiteSpace(ShardRunArguments))
                    ShardRunArguments = "run -c Release -- {0} {1} {2}";
                
                var portStr = data[nameof(ShardRunPort)];
                if (string.IsNullOrWhiteSpace(portStr))
                    ShardRunPort = new NadekoRandom().Next(5000, 6000);
                else
                    ShardRunPort = int.Parse(portStr);

                int ts = 1;
                int.TryParse(data[nameof(TotalShards)], out ts);
                TotalShards = ts < 1 ? 1 : ts;

                ulong.TryParse(data[nameof(ClientId)], out ulong clId);
                ClientId = clId;

                CarbonKey = data[nameof(CarbonKey)];
                var dbSection = data.GetSection("db");
                Db = new DBConfig(string.IsNullOrWhiteSpace(dbSection["Type"]) 
                                ? "sqlite" 
                                : dbSection["Type"], 
                            string.IsNullOrWhiteSpace(dbSection["ConnectionString"]) 
                                ? "Data Source=data/NadekoBot.db"
                                : dbSection["ConnectionString"]);
            }
            catch (Exception ex)
            {
                _log.Fatal(ex.Message);
                _log.Fatal(ex);
                throw;
            }
            
        }

        private class CredentialsModel
        {
            public ulong ClientId { get; set; } = 123123123;
            public string Token { get; set; } = "";
            public ulong[] OwnerIds { get; set; } = new ulong[1];
            public string LoLApiKey { get; set; } = "";
            public string GoogleApiKey { get; set; } = "";
            public string MashapeKey { get; set; } = "";
            public string OsuApiKey { get; set; } = "";
            public string SoundCloudClientId { get; set; } = "";
            public string CleverbotApiKey { get; } = "";
            public string CarbonKey { get; set; } = "";
            public DBConfig Db { get; set; } = new DBConfig("sqlite", "Data Source=data/NadekoBot.db");
            public int TotalShards { get; set; } = 1;
            public string PatreonAccessToken { get; set; } = "";
            public string PatreonCampaignId { get; set; } = "334038";
            public string RestartCommand { get; set; } = null;

            public string ShardRunCommand { get; set; } = "";
            public string ShardRunArguments { get; set; } = "";
            public int? ShardRunPort { get; set; } = null;
        }

        private class DbModel
        {
            public string Type { get; set; }
            public string ConnectionString { get; set; }
        }

        public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
    }
}
