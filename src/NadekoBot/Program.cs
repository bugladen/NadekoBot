namespace NadekoBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
                return;
            if (args[0].ToLowerInvariant() == "main")
                new ShardsCoordinator().RunAndBlockAsync(args).GetAwaiter().GetResult();
            else if (int.TryParse(args[0], out int shardId) && int.TryParse(args[1], out int parentProcessId))
                new NadekoBot(shardId, parentProcessId).RunAndBlockAsync(args).GetAwaiter().GetResult();
        }
    }
}
