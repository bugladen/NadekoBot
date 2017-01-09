using Discord.Commands;
using NLog;

namespace NadekoBot.Modules
{
    public abstract class DiscordModule : ModuleBase
    {
        protected Logger _log { get; }
        protected string _prefix { get; }

        public DiscordModule()
        {
            string prefix;
            if (NadekoBot.ModulePrefixes.TryGetValue(this.GetType().Name, out prefix))
                _prefix = prefix;
            else
                _prefix = "?missing_prefix?";

            _log = LogManager.GetCurrentClassLogger();
        }
    }
}
