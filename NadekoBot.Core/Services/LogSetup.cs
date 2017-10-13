using NLog;
using NLog.Config;
using NLog.Targets;

namespace NadekoBot.Core.Services
{
    public class LogSetup
    {
        public static void SetupLogger()
        {
            var logConfig = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget()
            {
                Layout = @"${date:format=HH\:mm\:ss} ${logger:shortName=True} | ${message}"
            };
            logConfig.AddTarget("Console", consoleTarget);

            logConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));

            LogManager.Configuration = logConfig;
        }
    }
}
