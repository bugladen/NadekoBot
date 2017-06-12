using Discord.Commands;
using NadekoBot.Services.Administration;
using System;
using System.Threading.Tasks;

namespace NadekoBot.TypeReaders
{
    public class GuildDateTimeTypeReader : TypeReader
    {
        private readonly GuildTimezoneService _gts;

        public GuildDateTimeTypeReader(GuildTimezoneService gts)
        {
            _gts = gts;
        }

        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
        {
            if (!DateTime.TryParse(input, out var dt))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input string is in an incorrect format."));

            var tz = _gts.GetTimeZoneOrUtc(context.Guild.Id);

            return Task.FromResult(TypeReaderResult.FromSuccess(new GuildDateTime(tz, dt)));
        }
    }

    public class GuildDateTime
    {
        public TimeZoneInfo Timezone { get; }
        public DateTime CurrentGuildTime { get; }
        public DateTime InputTime { get; }
        public DateTime InputTimeUtc { get; }

        private GuildDateTime() { }

        public GuildDateTime(TimeZoneInfo guildTimezone, DateTime inputTime)
        {
            var now = DateTime.UtcNow;
            Timezone = guildTimezone;
            CurrentGuildTime = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Utc, Timezone);
            InputTime = inputTime;
            InputTimeUtc = TimeZoneInfo.ConvertTime(inputTime, Timezone, TimeZoneInfo.Utc);
        }
    }
}
