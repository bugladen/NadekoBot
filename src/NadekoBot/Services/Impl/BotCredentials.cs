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

        public string ClientId { get; }

        public string GoogleApiKey { get; }

        public string MashapeKey { get; }

        public string Token { get; }

        public ulong[] OwnerIds { get; }

        public string LoLApiKey { get; }
        public string OsuApiKey { get; }
        public string SoundCloudClientId { get; }

        public DB Db { get; }
        public int TotalShards { get; }

        public BotCredentials()
        {
            _log = LogManager.GetCurrentClassLogger();
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
                SoundCloudClientId = cm.SoundCloudClientId;
                if (cm.Db == null)
                    Db = new DB("sqlite", "");
                else
                    Db = new DB(cm.Db.Type, cm.Db.ConnectionString);
            }
            else
            {
                File.WriteAllText("./credentials_example.json", JsonConvert.SerializeObject(new CredentialsModel(), Formatting.Indented));
                _log.Fatal($"credentials.json is missing. Failed to start. Example written to {Path.GetFullPath("./credentials_example.json")}");
                throw new FileNotFoundException();
            }
        }

        private class CredentialsModel
        {
            public string Token { get; set; }
            public ulong[] OwnerIds { get; set; }
            public string LoLApiKey { get; set; }
            public string GoogleApiKey { get; set; }
            public string MashapeKey { get; set; }
            public string OsuApiKey { get; set; }
            public string SoundCloudClientId { get; set; }
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
