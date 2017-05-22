using Discord.Commands;

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
