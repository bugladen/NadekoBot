using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class AutoAssignRoleCommands : NadekoSubmodule<AutoAssignRoleService>
        {

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task AutoAssignRole([Remainder] IRole role = null)
            {
                var guser = (IGuildUser)Context.User;
                if (role != null)
                {
                    if (role.Id == Context.Guild.EveryoneRole.Id)
                        return;

                    // the user can't aar the role which is higher or equal to his highest role
                    if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                        return;

                    _service.EnableAar(Context.Guild.Id, role.Id);
                    await ReplyConfirmLocalized("aar_enabled").ConfigureAwait(false);
                }
                else
                {
                    _service.DisableAar(Context.Guild.Id);
                    await ReplyConfirmLocalized("aar_disabled").ConfigureAwait(false);
                    return;
                }
            }
        }
    }
}
