using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class ServerGreetCommands : NadekoSubmodule<GreetSettingsService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDel(int timer = 30)
            {
                if (timer < 0 || timer > 600)
                    return;

                await _service.SetGreetDel(Context.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await ReplyConfirmLocalized("greetdel_on", timer).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("greetdel_off").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Greet()
            {
                var enabled = await _service.SetGreet(Context.Guild.Id, Context.Channel.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalized("greet_on").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("greet_off").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public Task GreetMsg()
            {
                string greetMsg = _service.GetGreetMsg(Context.Guild.Id);
                return ReplyConfirmLocalized("greetmsg_cur", greetMsg?.SanitizeMentions());
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetMsg([Remainder] string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await GreetMsg().ConfigureAwait(false);
                    return;                    
                }

                var sendGreetEnabled = _service.SetGreetMessage(Context.Guild.Id, ref text);

                await ReplyConfirmLocalized("greetmsg_new").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await ReplyConfirmLocalized("greetmsg_enable", $"`{Prefix}greet`").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDm()
            {
                var enabled = await _service.SetGreetDm(Context.Guild.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalized("greetdm_on").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("greetdm_off").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public Task GreetDmMsg()
            {
                var dmGreetMsg = _service.GetDmGreetMsg(Context.Guild.Id);
                return ReplyConfirmLocalized("greetdmmsg_cur", dmGreetMsg?.SanitizeMentions());
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDmMsg([Remainder] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await GreetDmMsg().ConfigureAwait(false);
                    return;
                }

                var sendGreetEnabled = _service.SetGreetDmMessage(Context.Guild.Id, ref text);

                await ReplyConfirmLocalized("greetdmmsg_new").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await ReplyConfirmLocalized("greetdmmsg_enable", $"`{Prefix}greetdm`").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Bye()
            {
                var enabled = await _service.SetBye(Context.Guild.Id, Context.Channel.Id).ConfigureAwait(false);

                if (enabled)
                    await ReplyConfirmLocalized("bye_on").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("bye_off").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public Task ByeMsg()
            {
                var byeMsg = _service.GetByteMessage(Context.Guild.Id);
                return ReplyConfirmLocalized("byemsg_cur", byeMsg?.SanitizeMentions());
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ByeMsg([Remainder] string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await ByeMsg().ConfigureAwait(false);
                    return;
                }

                var sendByeEnabled = _service.SetByeMessage(Context.Guild.Id, ref text);

                await ReplyConfirmLocalized("byemsg_new").ConfigureAwait(false);
                if (!sendByeEnabled)
                    await ReplyConfirmLocalized("byemsg_enable", $"`{Prefix}bye`").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ByeDel(int timer = 30)
            {
                await _service.SetByeDel(Context.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await ReplyConfirmLocalized("byedel_on", timer).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("byedel_off").ConfigureAwait(false);
            }

        }
    }
}