using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services.Administration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class TimeZoneCommands : NadekoSubmodule
        {
            private readonly GuildTimezoneService _service;

            public TimeZoneCommands(GuildTimezoneService service)
            {
                _service = service;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Timezones(int page = 1)
            {
                page -= 1;

                if (page < 0 || page > 20)
                    return;

                var timezones = TimeZoneInfo.GetSystemTimeZones();
                var timezonesPerPage = 20;

                await Context.Channel.SendPaginatedConfirmAsync((DiscordShardedClient)Context.Client, page + 1, (curPage) => new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("Available Timezones")
                    .WithDescription(string.Join("\n", timezones.Skip((curPage - 1) * timezonesPerPage).Take(timezonesPerPage).Select(x => $"`{x.Id,-25}` UTC{x.BaseUtcOffset:hhmm}"))),
                    timezones.Count / timezonesPerPage);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Timezone([Remainder] string id = null)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    await ReplyConfirmLocalized("timezone_guild", _service.GetTimeZoneOrUtc(Context.Guild.Id)).ConfigureAwait(false);
                    return;
                }

                TimeZoneInfo tz;
                try { tz = TimeZoneInfo.FindSystemTimeZoneById(id); } catch { tz = null; }

                _service.SetTimeZone(Context.Guild.Id, tz);

                if (tz == null)
                {
                    await Context.Channel.SendErrorAsync("Timezone not found. You should specify one of the timezones listed in the 'timezones' command.").ConfigureAwait(false);
                    return;
                }

                await Context.Channel.SendConfirmAsync(tz.ToString()).ConfigureAwait(false);
            }
        }
    }
}
