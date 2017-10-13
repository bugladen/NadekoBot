using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IXpRepository : IRepository<UserXpStats>
    {
        UserXpStats GetOrCreateUser(ulong guildId, ulong userId);
        int GetUserGuildRanking(ulong userId, ulong guildId);
        UserXpStats[] GetUsersFor(ulong guildId, int page);
    }
}
