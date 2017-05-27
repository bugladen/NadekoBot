using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Linq;

namespace NadekoBot.Services.Permissions
{
    public class GlobalPermissionService
    {
        public readonly ConcurrentHashSet<string> BlockedModules;
        public readonly ConcurrentHashSet<string> BlockedCommands;

        public GlobalPermissionService(BotConfig bc)
        {
            BlockedModules = new ConcurrentHashSet<string>(bc.BlockedModules.Select(x => x.Name));
            BlockedCommands = new ConcurrentHashSet<string>(bc.BlockedCommands.Select(x => x.Name));
        }
    }
}
