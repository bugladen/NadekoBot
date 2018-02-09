using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class QuoteRepository : Repository<Quote>, IQuoteRepository
    {
        public QuoteRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Quote> GetGroup(ulong guildId, int page, OrderType order)
        {
            var q = _set.Where(x => x.GuildId == guildId);
            if (order == OrderType.Keyword)
                q = q.OrderBy(x => x.Keyword);
            else
                q = q.OrderBy(x => x.Id);

            return q.Skip(15 * page).Take(15).ToArray();
        }

        public Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword)
        {
            var rng = new NadekoRandom();
            return _set.Where(q => q.GuildId == guildId && q.Keyword == keyword).OrderBy(q => rng.Next())
                .FirstOrDefaultAsync();
        }

        public Task<Quote> SearchQuoteKeywordTextAsync(ulong guildId, string keyword, string text)
        {
            var rngk = new NadekoRandom();
            return _set.Where(q => q.Text.ContainsNoCase(text, StringComparison.OrdinalIgnoreCase)
                && q.GuildId == guildId && q.Keyword == keyword)
                .OrderBy(q => rngk.Next())
                .FirstOrDefaultAsync();
        }

        public void RemoveAllByKeyword(ulong guildId, string keyword)
        {
            _set.RemoveRange(_set.Where(x => x.GuildId == guildId && x.Keyword.ToUpper() == keyword));
        }

    }
}
