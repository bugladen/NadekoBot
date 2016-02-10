using Discord.Commands.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace NadekoBot.Classes.PermissionCheckers {
    class NSFWPermissionChecker : PermissionChecker<NSFWPermissionChecker> {
        public override bool CanRun(Command command, User user, Channel channel, out string error) {
            error = string.Empty;
            Console.WriteLine(command.Category);
            return false;
        }
    }
}
