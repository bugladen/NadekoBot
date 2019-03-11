﻿using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Modules.Searches.Services;
using NadekoBot.Extensions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        public class CryptoCommands : NadekoSubmodule<CryptoService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Crypto(string name)
            {
                name = name?.ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(name))
                    return;

                var (crypto, nearest) = await _service.GetCryptoData(name).ConfigureAwait(false);

                if (nearest != null)
                {
                    var embed = new EmbedBuilder()
                            .WithTitle(GetText("crypto_not_found"))
                            .WithDescription(GetText("did_you_mean", Format.Bold($"{nearest.Name} ({nearest.Symbol})")));

                    if (await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    {
                        crypto = nearest;
                    }
                }

                if (crypto == null)
                {
                    await ReplyErrorLocalizedAsync("crypto_not_found").ConfigureAwait(false);
                    return;
                }


                await Context.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle($"{crypto.Name} ({crypto.Symbol})")
                    .WithUrl($"https://coinmarketcap.com/currencies/{crypto.Website_Slug}/")
                    .WithThumbnailUrl($"https://s2.coinmarketcap.com/static/img/coins/128x128/{crypto.Id}.png")
                    .AddField(GetText("market_cap"), $"${crypto.Quotes["USD"].Market_Cap:n0}", true)
                    .AddField(GetText("price"), $"${crypto.Quotes["USD"].Price}", true)
                    .AddField(GetText("volume_24h"), $"${crypto.Quotes["USD"].Volume_24h:n0}", true)
                    .AddField(GetText("change_7d_24h"), $"{crypto.Quotes["USD"].Percent_Change_7d}% / {crypto.Quotes["USD"].Percent_Change_24h}%", true)
                    .WithImageUrl($"https://s2.coinmarketcap.com/generated/sparklines/web/7d/usd/{crypto.Id}.png")).ConfigureAwait(false);
            }
        }
    }
}
