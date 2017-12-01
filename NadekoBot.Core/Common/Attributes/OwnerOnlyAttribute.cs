using System;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Core.Services;

namespace NadekoBot.Common.Attributes
{
    public class OwnerOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo executingCommand, IServiceProvider services)
        {
            var creds = (IBotCredentials)services.GetService(typeof(IBotCredentials));

            return Task.FromResult((creds.IsOwner(context.User) || context.Client.CurrentUser.Id == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner")));
        }
    }
}