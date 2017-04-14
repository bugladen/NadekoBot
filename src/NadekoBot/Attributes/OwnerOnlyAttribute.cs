using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot.Attributes
{
    public class OwnerOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo executingCommand,IDependencyMap depMap) =>
            Task.FromResult((NadekoBot.Credentials.IsOwner(context.User) || NadekoBot.Client.CurrentUser.Id == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner")));
    }
}