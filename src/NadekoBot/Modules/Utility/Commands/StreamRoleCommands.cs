using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Services.Utility;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        public class StreamRoleCommands : NadekoSubmodule
        {
            private readonly StreamRoleService service;

            public StreamRoleCommands(StreamRoleService service)
            {
                this.service = service;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole(IRole fromRole, IRole addRole)
            {
                this.service.SetStreamRole(fromRole, addRole);

                await ReplyConfirmLocalized("stream_role_enabled", Format.Bold(fromRole.ToString()), Format.Bold(addRole.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole()
            {
                this.service.StopStreamRole(Context.Guild.Id);
                await ReplyConfirmLocalized("stream_role_disabled").ConfigureAwait(false);
            }
        }
    }
}