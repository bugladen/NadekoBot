using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class BotCredentials : IBotCredentials
    {
        public string GoogleApiKey {
            get {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<string> MashapeKey { get; internal set; }

        public string Token {
            get {
                throw new NotImplementedException();
            }
        }
    }
}
