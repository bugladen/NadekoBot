using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using NadekoBot.Services;

namespace NadekoBot.Services.Administration
{
    public class GuildTimezoneService
    {
        private ConcurrentDictionary<ulong, TimeZoneInfo> _timezones;
        private readonly DbService _db;

        public GuildTimezoneService(IEnumerable<GuildConfig> gcs, DbService db)
        {
            _timezones = gcs
                .Select(x =>
                {
                    TimeZoneInfo tz;
                    try
                    {
                        tz = TimeZoneInfo.FindSystemTimeZoneById(x.TimeZoneId);
                    }
                    catch
                    {
                        tz = null;
                    }
                    return (x.GuildId, tz);
                })
                .Where(x => x.Item2 != null)
                .ToDictionary(x => x.Item1, x => x.Item2)
                .ToConcurrent();

            _db = db;
        }

        public TimeZoneInfo GetTimeZoneOrDefault(ulong guildId)
        {
            if (_timezones.TryGetValue(guildId, out var tz))
                return tz;
            return null;
        }

        public void SetTimeZone(ulong guildId, TimeZoneInfo tz)
        {
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);

                gc.TimeZoneId = tz?.Id;
                uow.Complete();

                if (tz == null)
                    _timezones.TryRemove(guildId, out tz);
                else
                    _timezones.AddOrUpdate(guildId, tz, (key, old) => tz);
            }
        }

        public TimeZoneInfo GetTimeZoneOrUtc(ulong guildId)
            => GetTimeZoneOrDefault(guildId) ?? TimeZoneInfo.Utc;
    }
}
