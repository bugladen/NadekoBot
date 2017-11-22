using System.Threading.Tasks;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Permissions.Services
{
    public class ResetPermissionsService : INService
    {
        private readonly PermissionService _perms;
        private readonly GlobalPermissionService _globalPerms;
        private readonly DbService _db;

        public ResetPermissionsService(PermissionService perms, GlobalPermissionService globalPerms, DbService db)
        {
            _perms = perms;
            _globalPerms = globalPerms;
            _db = db;
        }

        public async Task ResetPermissions(ulong guildId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.GcWithPermissionsv2For(guildId);
                config.Permissions = Permissionv2.GetDefaultPermlist;
                await uow.CompleteAsync().ConfigureAwait(false);
                _perms.UpdateCache(config);
            }
        }

        public async Task ResetGlobalPermissions()
        {
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.BotConfig.GetOrCreate();
                gc.BlockedCommands.Clear();
                gc.BlockedModules.Clear();

                _globalPerms.BlockedCommands.Clear();
                _globalPerms.BlockedModules.Clear();
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }
    }
}
