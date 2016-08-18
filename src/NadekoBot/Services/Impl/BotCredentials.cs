using System;
using System.Collections.Generic;

namespace NadekoBot.Services.Impl
{
    //todo load creds
    public class BotCredentials : IBotCredentials
    {
        public string ClientId { get; }

        public string GoogleApiKey {
            get {
                return "";
            }
        }

        public IEnumerable<string> MashapeKey { get; }

        public string Token {
            get {
                return "";
            }
        }
    }
}
