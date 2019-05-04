﻿using System.Threading.Tasks;
using Discord.Commands;
using System;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using Discord;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class PatreonCommands : NadekoSubmodule<PatreonRewardsService>
        {
            private readonly IBotCredentials _creds;
            private readonly DbService _db;
            private readonly ICurrencyService _currency;

            public PatreonCommands(IBotCredentials creds, DbService db, ICurrencyService currency)
            {
                _creds = creds;
                _db = db;
                _currency = currency;
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.DM)]
            [OwnerOnly]
            public async Task PatreonRewardsReload()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                    return;
                await _service.RefreshPledges().ConfigureAwait(false);

                await ctx.Channel.SendConfirmAsync("👌").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.DM)]
            public async Task ClaimPatreonRewards()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                    return;

                if (DateTime.UtcNow.Day < 5)
                {
                    await ReplyErrorLocalizedAsync("clpa_too_early").ConfigureAwait(false);
                    return;
                }
                int amount = 0;
                try
                {
                    amount = await _service.ClaimReward(ctx.User.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }

                if (amount > 0)
                {
                    await ReplyConfirmLocalizedAsync("clpa_success", amount + Bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }
                var rem = (_service.Interval - (DateTime.UtcNow - _service.LastUpdate));
                var helpcmd = Format.Code(Prefix + "donate");
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
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