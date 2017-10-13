using NadekoBot.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            if (args.Length == 2 
                && int.TryParse(args[0], out int shardId) 
                && int.TryParse(args[1], out int parentProcessId))
            {
                return new NadekoBot(shardId, parentProcessId)
                    .RunAndBlockAsync(args);
            }
            else
            {
                //todo revert
                var _ = new NadekoBot(0, Process.GetCurrentProcess().Id)
                       .RunAsync(args);
                return new ShardsCoordinator()
                    .RunAndBlockAsync();
            }
        }
    }
}
