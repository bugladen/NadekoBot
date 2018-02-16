using NadekoBot.Core.Services.Database.Models;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IXpRepository : IRepository<UserXpStats>
    {
        UserXpStats GetOrCreateUser(ulong guildId, ulong userId);
        Task<int> GetUserGuildRankingAsync(ulong userId, ulong guildId);
        UserXpStats[] GetUsersFor(ulong guildId, int page);
        void ResetGuildUserXp(ulong userId, ulong guildId);
        void ResetGuildXp(ulong guildId);
    }
}
