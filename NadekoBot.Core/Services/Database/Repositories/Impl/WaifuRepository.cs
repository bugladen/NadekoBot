using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class WaifuRepository : Repository<WaifuInfo>, IWaifuRepository
    {
        public WaifuRepository(DbContext context) : base(context)
        {
        }

        public WaifuInfo ByWaifuUserId(ulong userId)
        {
            return _set.Include(wi => wi.Waifu)
                        .Include(wi => wi.Affinity)
                        .Include(wi => wi.Claimer)
                        .Include(wi => wi.Items)
                        .FirstOrDefault(wi => wi.Waifu.UserId == userId);
        }

        public IList<WaifuInfo> ByClaimerUserId(ulong userId)
        {
            return _set.Include(wi => wi.Waifu)
                        .Include(wi => wi.Affinity)
                        .Include(wi => wi.Claimer)
                        .Include(wi => wi.Items)
                        .Where(wi => wi.Claimer != null && wi.Claimer.UserId == userId)
                        .ToList();
        }

        public IList<WaifuInfo> GetTop(int count, int skip = 0)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return new List<WaifuInfo>();
            return _set.Include(wi => wi.Waifu)
                        .Include(wi => wi.Affinity)
                        .Include(wi => wi.Claimer)
                    .OrderByDescending(wi => wi.Price)
                    .Skip(skip)
                    .Take(count)
                    .ToList();

        }
    }
}