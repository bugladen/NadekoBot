using NadekoBot.Core.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NadekoBot
{
    public sealed class Program
    {
        public static Task Main(string[] args)
        {
            if (args.Length == 2
                && int.TryParse(args[0], out int shardId)
                && int.TryParse(args[1], out int parentProcessId))
            {
                return new NadekoBot(shardId, parentProcessId)
                    .RunAndBlockAsync();
            }
            else
            {
#if DEBUG
                var _ = new NadekoBot(0, Process.GetCurrentProcess().Id)
                       .RunAsync();
#endif
                return new ShardsCoordinator()
                    .RunAndBlockAsync();
            }
        }
    }
}
