using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SlowModeCommands : NadekoSubmodule<SlowmodeService>
        {
            private readonly DbService _db;

            public SlowModeCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public Task Slowmode()
            {
                if (_service.StopSlowmode(Context.Channel.Id))
                {
                    return ReplyConfirmLocalized("slowmode_disabled");
                }
                else
                {
                    return Slowmode(1, 5);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Slowmode(uint msgCount, int perSec)
            {
                if (msgCount < 1 || perSec < 1 || msgCount > 100 || perSec > 3600)
                {
                    await ReplyErrorLocalized("invalid_params").ConfigureAwait(false);
                    return;
                }
                if (_service.StartSlowmode(Context.Channel.Id, msgCount, perSec))
                {
                    await Context.Channel.SendConfirmAsync(GetText("slowmode_init"),
                            GetText("slowmode_desc", Format.Bold(msgCount.ToString()), Format.Bold(perSec.ToString())))
                                                .ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task SlowmodeWhitelist(IGuildUser user)
            {
                bool added = _service.ToggleWhitelistUser(user.Guild.Id, user.Id);

                if (!added)
                    await ReplyConfirmLocalized("slowmodewl_user_stop", Format.Bold(user.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("slowmodewl_user_start", Format.Bold(user.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(1)]
            public async Task SlowmodeWhitelist(IRole role)
            {
                bool added = _service.ToggleWhitelistRole(role.Guild.Id, role.Id);

                if (!added)
                    await ReplyConfirmLocalized("slowmodewl_role_stop", Format.Bold(role.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("slowmodewl_role_start", Format.Bold(role.ToString())).ConfigureAwait(false);
            }
        }
    }
}