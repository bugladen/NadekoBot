using Discord;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NadekoBot.Services
{
    public interface IBotCredentials
    {
        ulong ClientId { get; }

        string Token { get; }
        string GoogleApiKey { get; }
        ImmutableArray<ulong> OwnerIds { get; }
        string MashapeKey { get; }
        string LoLApiKey { get; }
        string PatreonAccessToken { get; }

        DBConfig Db { get; }

        bool IsOwner(IUser u);
    }

    public class DBConfig
    {
        public DBConfig(string type, string connString)
        {
            this.Type = type;
            this.ConnectionString = connString;
        }
        public string Type { get; }
        public string ConnectionString { get; }
    }
}
