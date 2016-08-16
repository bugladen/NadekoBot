using System;
using System.Collections.Generic;

namespace NadekoBot.Services.Impl
{
    public class BotCredentials : IBotCredentials
    {
        public string ClientId { get; }

        public string GoogleApiKey {
            get {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<string> MashapeKey { get; }

        public string Token {
            get {
                throw new NotImplementedException();
            }
        }
    }
}
