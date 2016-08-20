using Discord;
using System.Linq;

namespace NadekoBot.Services
{
    public interface IBotCredentials
    {
        string ClientId { get; }
        string Token { get; }
        string GoogleApiKey { get; }
        ulong[] OwnerIds { get; }


        bool IsOwner(IUser u);
    }
}
