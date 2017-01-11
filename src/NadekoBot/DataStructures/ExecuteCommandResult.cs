using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NadekoBot.Modules.Permissions.Permissions;

namespace NadekoBot.DataStructures
{
    public struct ExecuteCommandResult
    {
        public readonly CommandInfo CommandInfo;
        public readonly PermissionCache PermissionCache;
        public readonly IResult Result;

        public ExecuteCommandResult(CommandInfo commandInfo, PermissionCache cache, IResult result)
        {
            this.CommandInfo = commandInfo;
            this.PermissionCache = cache;
            this.Result = result;
        }
    }
}
