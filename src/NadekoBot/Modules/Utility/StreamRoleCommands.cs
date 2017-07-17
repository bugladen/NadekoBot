using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        public class StreamRoleCommands : NadekoSubmodule<StreamRoleService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole(IRole fromRole, IRole addRole)
            {
                this._service.SetStreamRole(fromRole, addRole);

                await ReplyConfirmLocalized("stream_role_enabled", Format.Bold(fromRole.ToString()), Format.Bold(addRole.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole()
            {
                this._service.StopStreamRole(Context.Guild.Id);
                await ReplyConfirmLocalized("stream_role_disabled").ConfigureAwait(false);
            }
        }
    }
}