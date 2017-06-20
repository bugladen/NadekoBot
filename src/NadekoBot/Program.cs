namespace NadekoBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 2 && int.TryParse(args[0], out int shardId) && int.TryParse(args[1], out int parentProcessId))
                new NadekoBot(shardId, parentProcessId).RunAndBlockAsync(args).GetAwaiter().GetResult();
            else
                new NadekoBot(0, 0).RunAndBlockAsync(args).GetAwaiter().GetResult();
        }
    }
}
