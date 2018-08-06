using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Gambling.Services;
using Discord.WebSocket;
using System.Linq;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PlantPickCommands : NadekoSubmodule<PlantPickService>
        {

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Pick(string pass = null)
            {
                if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric())
                {
                    return;
                }

                var picked = await _service.PickAsync(Context.Guild.Id, (ITextChannel)Context.Channel, Context.User.Id, pass);

                if (picked > 0)
                {
                    var msg = await ReplyConfirmLocalized("picked", picked + Bc.BotConfig.CurrencySign)
                       .ConfigureAwait(false);
                    msg.DeleteAfter(10);
                }

                if (((SocketGuild)Context.Guild).CurrentUser.GuildPermissions.ManageMessages)
                {
                    try { await Context.Message.DeleteAsync().ConfigureAwait(false); } catch { }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Plant(int amount = 1, string pass = null)
            {
                if (amount < 1)
                    return;

                if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric())
                {
                    return;
                }

                var success = await _service.PlantAsync(Context.Guild.Id, Context.Channel, Context.User.Id, Context.User.ToString(), amount, pass);
                if (!success)
                {
                    await ReplyErrorLocalized("not_enough", Bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }

                if (((SocketGuild)Context.Guild).CurrentUser.GuildPermissions.ManageMessages)
                {
                    await Context.Message.DeleteAsync().ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
#if GLOBAL_NADEKO
            [OwnerOnly]
#endif
            public async Task GenCurrency()
            {
                bool enabled = _service.ToggleCurrencyGeneration(Context.Guild.Id, Context.Channel.Id);
                if (enabled)
                {
                    await ReplyConfirmLocalized("curgen_enabled").ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("curgen_disabled").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [OwnerOnly]
            public Task GenCurList(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                var enabledIn = _service.GetAllGeneratingChannels();

                return Context.SendPaginatedConfirmAsync(page, (cur) =>
                {
                    var items = enabledIn.Skip(page * 9).Take(9);

                    if (!items.Any())
                    {
                        return new EmbedBuilder().WithErrorColor()
                            .WithDescription("-");
                    }

                    return items.Aggregate(new EmbedBuilder().WithOkColor(),
                        (eb, i) => eb.AddField(i.GuildId.ToString(), i.ChannelId));
                }, enabledIn.Count(), 9);
            }
        }
    }
}