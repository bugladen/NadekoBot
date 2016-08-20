using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using System.Linq;
using NLog;

namespace NadekoBot.Services.Impl
{
    //todo load creds
    public class BotCredentials : IBotCredentials
    {
        private Logger _log;

        public string ClientId { get; }

        public string GoogleApiKey { get; }

        public IEnumerable<string> MashapeKey { get; }

        public string Token { get; }

        public ulong[] OwnerIds { get; }

        public string LoLApiKey { get; }

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
            }
            else
                _log.Fatal("credentials.json is missing. Failed to start.");
        }

        private class CredentialsModel {
            public string Token { get; set; }
            public ulong[] OwnerIds { get; set; }
            public string LoLApiKey { get; set; }
            public string GoogleApiKey { get; set; }
            public IEnumerable<string> MashapeKey { get; set; }
        }

        public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
    }
}
