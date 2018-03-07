using Discord.Commands;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Core.Common.Attributes
{
    public class Ratelimit : PreconditionAttribute
    {
        public int Seconds { get; }

        public Ratelimit(int seconds)
        {
            if (seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));

            Seconds = seconds;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Seconds == 0)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var cache = (IDataCache)services.GetService(typeof(IDataCache));
            var rem = cache.TryAddRatelimit(context.User.Id, command.Name, Seconds);

            if(rem == null)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var msgContent = $"You can use this command again in {rem.Value.TotalSeconds:F2} seconds.";
            var _ = Task.Run(() => context.Channel.SendErrorAsync(msgContent));

            return Task.FromResult(PreconditionResult.FromError(msgContent));
        }
    }
}
