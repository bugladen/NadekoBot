using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Attributes;
using System;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;
using Discord;
using NadekoBot.Services.Utility;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class PatreonCommands : NadekoSubmodule
        {
            private readonly PatreonRewardsService _patreon;
            private readonly IBotCredentials _creds;
            private readonly BotConfig _config;
            private readonly DbService _db;
            private readonly CurrencyService _currency;

            public PatreonCommands(PatreonRewardsService p, IBotCredentials creds, BotConfig config, DbService db, CurrencyService currency)
            {
                _creds = creds;
                _config = config;
                _db = db;
                _currency = currency;
                _patreon = p;                
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task PatreonRewardsReload()
            {
                await _patreon.LoadPledges().ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("👌").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task ClaimPatreonRewards()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                    return;
                if (DateTime.UtcNow.Day < 5)
                {
                    await ReplyErrorLocalized("clpa_too_early").ConfigureAwait(false);
                    return;
                }
                int amount = 0;
                try
                {
                    amount = await _patreon.ClaimReward(Context.User.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }

                if (amount > 0)
                {
                    await ReplyConfirmLocalized("clpa_success", amount + _config.CurrencySign).ConfigureAwait(false);
                    return;
                }
                var rem = (_patreon.Interval - (DateTime.UtcNow - _patreon.LastUpdate));
                var helpcmd = Format.Code(NadekoBot.Prefix + "donate");
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(GetText("clpa_fail"))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_already_title")).WithValue(GetText("clpa_fail_already")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_wait_title")).WithValue(GetText("clpa_fail_wait")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_conn_title")).WithValue(GetText("clpa_fail_conn")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_sup_title")).WithValue(GetText("clpa_fail_sup", helpcmd)))
                    .WithFooter(efb => efb.WithText(GetText("clpa_next_update", rem))))
                    .ConfigureAwait(false);
            }
        }

    }
}