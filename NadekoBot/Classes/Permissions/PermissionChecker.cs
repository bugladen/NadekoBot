using Discord.Commands.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace NadekoBot.Classes.Permissions {
    class PermissionChecker : IPermissionChecker {
        public static readonly PermissionChecker _instance = new PermissionChecker();
        public static PermissionChecker Instance => _instance;

        static PermissionChecker() { }
        public PermissionChecker() { }

        public bool CanRun(Command command, User user, Channel channel, out string error) {
            error = null;
            try {
                //is it a permission command?
                if (command.Text == "Permissions")
                    // if it is, check if the user has the correct role
                    // if yes return true, if no return false
                    if (user.Server.IsOwner || user.HasRole(PermissionHelper.ValidateRole(user.Server, PermissionsHandler.GetServerPermissionsRoleName(user.Server))))
                        return true;
                    else
                        throw new Exception("You do not have necessary role to change permissions.");

                var permissionType = PermissionsHandler.GetPermissionBanType(command, user, channel);

                if (permissionType == PermissionsHandler.PermissionBanType.None)
                    return true;

                throw new InvalidOperationException($"Cannot run this command: {permissionType}");
            } catch (Exception ex) {
                if (PermissionsHandler._permissionsDict[user.Server].Verbose) //if verbose - print errors
                    channel.SendMessage(ex.Message);
                return false;
            }
        }
    }
}
