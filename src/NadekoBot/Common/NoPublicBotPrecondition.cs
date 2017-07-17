using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot.Common
{
    public class NoPublicBot : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
#if GLOBAL_NADEKo
            return Task.FromResult(PreconditionResult.FromError("Not available on the public bot"));
#else
            return Task.FromResult(PreconditionResult.FromSuccess());
#endif
        }
    }
}
