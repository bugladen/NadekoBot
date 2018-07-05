using Discord;
using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IDiscordUserRepository : IRepository<DiscordUser>
    {
        DiscordUser GetOrCreate(ulong userId, string username, string discrim, string avatarId);
        DiscordUser GetOrCreate(IUser original);
        Task<int> GetUserGlobalRankingAsync(ulong id);
        DiscordUser[] GetUsersXpLeaderboardFor(int page);

        long GetUserCurrency(ulong userId);
        bool TryUpdateCurrencyState(ulong userId, string name, string discrim, string avatar, long change, bool allowNegative = false);
        IEnumerable<DiscordUser> GetTopRichest(ulong botId, int count, int skip);
        void RemoveFromMany(List<ulong> ids);
        void CurrencyDecay(float decay, ulong botId);
        long GetCurrencyDecayAmount(float decay);
    }
}
