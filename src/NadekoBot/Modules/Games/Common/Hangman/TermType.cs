using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Common.Hangman
{
    [Flags]
    public enum TermType
    {
        Countries = 0,
        Movies = 1,
        Animals = 2,
        Things = 4,
        Random = 8,
    }
}
