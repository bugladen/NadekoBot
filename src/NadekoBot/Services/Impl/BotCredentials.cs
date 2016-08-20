using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using System.Linq;

namespace NadekoBot.Services.Impl
{
    //todo load creds
    public class BotCredentials : IBotCredentials
    {
        public string ClientId { get; }

        public string GoogleApiKey { get; }

        public IEnumerable<string> MashapeKey { get; }

        public string Token { get; }

        public ulong[] OwnerIds { get; }

        public BotCredentials()
        {
            var cm = JsonConvert.DeserializeObject<CredentialsModel>(File.ReadAllText("./credentials.json"));
            Token = cm.Token;
            OwnerIds = cm.OwnerIds;
        }

        private class CredentialsModel {
            public string Token { get; set; }
            public ulong[] OwnerIds { get; set; }
        }

        public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
    }
}
