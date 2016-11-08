using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using System.Linq;
using NLog;

namespace NadekoBot.Services.Impl
{
    public class BotCredentials : IBotCredentials
    {
        private Logger _log;

        public ulong ClientId { get; }
        public ulong BotId { get; }

        public string GoogleApiKey { get; }

        public string MashapeKey { get; }

        public string Token { get; }

        public ulong[] OwnerIds { get; }

        public string LoLApiKey { get; }
        public string OsuApiKey { get; }
        public string SoundCloudClientId { get; }

        public DB Db { get; }
        public int TotalShards { get; }
        public string CarbonKey { get; }

        public BotCredentials()
        {
            _log = LogManager.GetCurrentClassLogger();

            try { File.WriteAllText("./credentials_example.json", JsonConvert.SerializeObject(new CredentialsModel(), Formatting.Indented)); } catch { }
            if (File.Exists("./credentials.json"))
            {
                var cm = JsonConvert.DeserializeObject<CredentialsModel>(File.ReadAllText("./credentials.json"));
                Token = cm.Token;
                OwnerIds = cm.OwnerIds;
                LoLApiKey = cm.LoLApiKey;
                GoogleApiKey = cm.GoogleApiKey;
                MashapeKey = cm.MashapeKey;
                OsuApiKey = cm.OsuApiKey;
                TotalShards = cm.TotalShards < 1 ? 1 : cm.TotalShards;
                BotId = cm.BotId ?? cm.ClientId;
                ClientId = cm.ClientId;
                SoundCloudClientId = cm.SoundCloudClientId;
                CarbonKey = cm.CarbonKey;
                if (cm.Db == null)
                    Db = new DB("sqlite", "");
                else
                    Db = new DB(cm.Db.Type, cm.Db.ConnectionString);
            }
            else
            {
                _log.Fatal($"credentials.json is missing. Failed to start. Example is in {Path.GetFullPath("./credentials_example.json")}");
                throw new FileNotFoundException();
            }
            
        }

        private class CredentialsModel
        {
            public ulong ClientId { get; set; } = 123123123;
            public ulong? BotId { get; set; }
            public string Token { get; set; } = "";
            public ulong[] OwnerIds { get; set; } = new ulong[1];
            public string LoLApiKey { get; set; } = "";
            public string GoogleApiKey { get; set; } = "";
            public string MashapeKey { get; set; } = "";
            public string OsuApiKey { get; set; } = "";
            public string SoundCloudClientId { get; set; } = "";
            public string CarbonKey { get; set; } = "";
            public DB Db { get; set; }
            public int TotalShards { get; set; } = 1;
        }

        private class DbModel
        {
            public string Type { get; set; }
            public string ConnectionString { get; set; }
        }

        public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
    }
}
