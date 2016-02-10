using Discord.Commands.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace NadekoBot.Classes.PermissionCheckers {
    abstract class PermissionChecker<T> : IPermissionChecker where T : new() {
        public static readonly T _instance = new T();
        public static T Instance => _instance;

        public abstract bool CanRun(Command command, User user, Channel channel, out string error);
    }
}
