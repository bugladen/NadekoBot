using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace NadekoBot.Common
{
    public class Shard0Precondition : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var c = (DiscordSocketClient)context.Client;
            if (c.ShardId != 0)
                return Task.FromResult(PreconditionResult.FromError("Must be ran from shard #0"));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
