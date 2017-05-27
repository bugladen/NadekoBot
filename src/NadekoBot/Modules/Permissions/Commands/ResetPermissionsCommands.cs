using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Permissions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions.Commands
{
    public partial class Permissions
    {
        [Group]
        public class ResetPermissionsCommands : NadekoSubmodule
        {
            private readonly PermissionsService _service;
            private readonly DbHandler _db;
            private readonly GlobalPermissionService _globalPerms;

            public ResetPermissionsCommands(PermissionsService service, GlobalPermissionService globalPerms, DbHandler db)
            {
                _service = service;
                _db = db;
                _globalPerms = globalPerms;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ResetPermissions()
            {
                //todo 80 move to service
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
                    config.Permissions = Permissionv2.GetDefaultPermlist;
                    await uow.CompleteAsync();
                    _service.UpdateCache(config);
                }
                await ReplyConfirmLocalized("perms_reset").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ResetGlobalPermissions()
            {
                //todo 80 move to service
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.BotConfig.GetOrCreate();
                    gc.BlockedCommands.Clear();
                    gc.BlockedModules.Clear();

                    _globalPerms.BlockedCommands.Clear();
                    _globalPerms.BlockedModules.Clear();
                    await uow.CompleteAsync();
                }
                await ReplyConfirmLocalized("global_perms_reset").ConfigureAwait(false);
            }
        }
    }
}
