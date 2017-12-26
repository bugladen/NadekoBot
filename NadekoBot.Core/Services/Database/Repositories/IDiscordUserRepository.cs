using Discord;
using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IDiscordUserRepository : IRepository<DiscordUser>
    {
        DiscordUser GetOrCreate(IUser original);
        int GetUserGlobalRanking(ulong id);
        DiscordUser[] GetUsersXpLeaderboardFor(int page);

        long GetUserCurrency(ulong userId);
        bool TryUpdateCurrencyState(ulong userId, long change, bool allowNegative = false);
        IEnumerable<DiscordUser> GetTopRichest(int count, int skip);
        void RemoveFromMany(List<long> ids);
    }
}
