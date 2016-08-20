using Discord;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Services
{
    public interface IBotCredentials
    {
        string ClientId { get; }
        string Token { get; }
        string GoogleApiKey { get; }
        ulong[] OwnerIds { get; }
        IEnumerable<string> MashapeKey { get; }
        string LoLApiKey { get; }

        bool IsOwner(IUser u);
    }
}
