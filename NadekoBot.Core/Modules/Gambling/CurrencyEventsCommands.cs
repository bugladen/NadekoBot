using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Modules.Gambling.Common.CurrencyEvents;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEventsCommands : NadekoSubmodule<CurrencyEventsService>
        {
            public enum CurrencyEvent
            {
                Reaction,
                SneakyGameStatus
            }

            private readonly DiscordSocketClient _client;
            private readonly IBotConfigProvider _bc;
            private readonly CurrencyService _cs;

            public CurrencyEventsCommands(DiscordSocketClient client, IBotConfigProvider bc, CurrencyService cs)
            {
                _client = client;
                _bc = bc;
                _cs = cs;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartEvent(CurrencyEvent e, int arg = -1)
            {
                switch (e)
                {
                    case CurrencyEvent.Reaction:
                        await ReactionEvent(Context, arg).ConfigureAwait(false);
                        break;
                    case CurrencyEvent.SneakyGameStatus:
                        await SneakyGameStatusEvent(Context, arg).ConfigureAwait(false);
                        break;
                }
            }

            private async Task SneakyGameStatusEvent(ICommandContext context, int num)
            {
                if (num < 10 || num > 600)
                    num = 60;

                var ev = new SneakyEvent(_cs, _client, _bc, num);
                if (!await _service.StartSneakyEvent(ev, context.Message, context))
                    return;
                try
                {
                    var title = GetText("sneakygamestatus_title");
                    var desc = GetText("sneakygamestatus_desc", 
                        Format.Bold(100.ToString()) + _bc.BotConfig.CurrencySign,
                        Format.Bold(num.ToString()));
                    await context.Channel.SendConfirmAsync(title, desc)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }

            public async Task ReactionEvent(ICommandContext context, int amount)
            {
                if (amount <= 0)
                    amount = 100;

                var title = GetText("reaction_title");
                var desc = GetText("reaction_desc", _bc.BotConfig.CurrencySign, Format.Bold(amount.ToString()) + _bc.BotConfig.CurrencySign);
                var footer = GetText("reaction_footer", 24);
                var re = new ReactionEvent(_bc.BotConfig, _client, _cs, amount);
                var msg = await context.Channel.SendConfirmAsync(title,
                        desc, footer: footer)
                    .ConfigureAwait(false);
                await re.Start(msg, context);
            }
        }
    }
}
