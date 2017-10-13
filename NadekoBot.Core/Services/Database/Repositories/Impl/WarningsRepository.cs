using NadekoBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class WarningsRepository : Repository<Warning>, IWarningsRepository
    {
        public WarningsRepository(DbContext context) : base(context)
        {
        }

        public Warning[] For(ulong guildId, ulong userId)
        {
            var query = _set.Where(x => x.GuildId == guildId && x.UserId == userId)
                .OrderByDescending(x => x.DateAdded);

            return query.ToArray();
        }

        public async Task ForgiveAll(ulong guildId, ulong userId, string mod)
        {
            await _set.Where(x => x.GuildId == guildId && x.UserId == userId)
                .ForEachAsync(x =>
                {
                    if (x.Forgiven != true)
                    {
                        x.Forgiven = true;
                        x.ForgivenBy = mod;
                    }
                })
                .ConfigureAwait(false);
        }

        public Warning[] GetForGuild(ulong id)
        {
            return _set.Where(x => x.GuildId == id).ToArray();
        }
    }
}
