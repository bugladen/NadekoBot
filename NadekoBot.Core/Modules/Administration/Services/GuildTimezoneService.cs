using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services
{
    public class GuildTimezoneService : INService
    {
        // todo 70 this is a hack >.<
        public static ConcurrentDictionary<ulong, GuildTimezoneService> AllServices { get; } = new ConcurrentDictionary<ulong, GuildTimezoneService>();
        private ConcurrentDictionary<ulong, TimeZoneInfo> _timezones;
        private readonly DbService _db;

        public GuildTimezoneService(DiscordSocketClient client, NadekoBot bot, DbService db)
        {
            _timezones = bot.AllGuildConfigs
                .Select(x =>
                {
                    TimeZoneInfo tz;
                    try
                    {
                        if (x.TimeZoneId == null)
                            tz = null;
                        else
                            tz = TimeZoneInfo.FindSystemTimeZoneById(x.TimeZoneId);
                    }
                    catch
                    {
                        tz = null;
                    }
                    return (x.GuildId, Timezone: tz);
                })
                .Where(x => x.Timezone != null)
                .ToDictionary(x => x.GuildId, x => x.Timezone)
                .ToConcurrent();

            var curUser = client.CurrentUser;
            if (curUser != null)
                AllServices.TryAdd(curUser.Id, this);
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
