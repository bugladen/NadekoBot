using CommandLine;

namespace NadekoBot.Core.Modules.Gambling.Common.AnimalRacing
{
    public class RaceOptions
    {
        [Option("start-delay", Default = 20, Required = false)]
        public int StartDelay { get; set; }
    }
}