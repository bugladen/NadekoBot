using Discord;
using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IDiscordUserRepository : IRepository<DiscordUser>
    {
        DiscordUser GetOrCreate(IUser original);
        Task<int> GetUserGlobalRankingAsync(ulong id);
        DiscordUser[] GetUsersXpLeaderboardFor(int page);

        long GetUserCurrency(ulong userId);
        bool TryUpdateCurrencyState(ulong userId, string name, string discrim, string avatar, long change, bool allowNegative = false);
        IEnumerable<DiscordUser> GetTopRichest(ulong botId, int count, int skip);
        void RemoveFromMany(List<long> ids);
    }
}
