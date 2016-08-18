using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace NadekoBot.Services.Impl
{
    //todo load creds
    public class BotCredentials : IBotCredentials
    {
        public string ClientId { get; }

        public string GoogleApiKey { get; }

        public IEnumerable<string> MashapeKey { get; }

        public string Token { get; }


        public BotCredentials()
        {
            var cm = JsonConvert.DeserializeObject<CredentialsModel>(File.ReadAllText("./credentials.json"));
            Token = cm.Token;
        }

        private class CredentialsModel {
            public string Token { get; set; }
        }
    }
}
