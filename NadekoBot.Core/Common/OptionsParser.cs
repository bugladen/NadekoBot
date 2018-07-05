using CommandLine;

namespace NadekoBot.Core.Common
{
    public static class OptionsParser
    {
        public static (T, bool) ParseFrom<T>(T options, string[] args) where T : INadekoCommandOptions
        {
            var res = Parser.Default.ParseArguments<T>(args);
            options = res.MapResult(x => x, x => options);
            options.NormalizeOptions();
            return (options, res.Tag == ParserResultType.Parsed);
        }
    }
}
