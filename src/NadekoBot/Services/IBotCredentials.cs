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
        string MashapeKey { get; }
        string LoLApiKey { get; }

        DB Db { get; }

        bool IsOwner(IUser u);
    }

    public class DB
    {
        public DB(string type, string connString)
        {
            this.Type = type;
            this.ConnectionString = connString;
        }
        public string Type { get; }
        public string ConnectionString { get; }
    }
}
