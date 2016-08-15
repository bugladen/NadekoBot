using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface IBotCredentials
    {
        string Token { get; }
        string GoogleApiKey { get; }
    }
}
