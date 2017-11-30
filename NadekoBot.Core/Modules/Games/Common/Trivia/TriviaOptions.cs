using CommandLine;
using NadekoBot.Core.Common;

namespace NadekoBot.Core.Modules.Games.Common.Trivia
{
    public class TriviaOptions : INadekoCommandOptions
    {
        [Option('p', "pokemon", Required = false, Default = false)]
        public bool IsPokemon { get; set; } = false;
        [Option("nohint", Required = false, Default = false)]
        public bool NoHint { get; set; } = true;
        [Option('w', "win-req", Required = false, Default = 10)]
        public int WinRequirement { get; set; } = 10;
        
        public void NormalizeOptions()
        {
        }
    }
}
