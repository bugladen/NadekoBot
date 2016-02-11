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
            error = string.Empty;
            return false;
        }
    }
}
