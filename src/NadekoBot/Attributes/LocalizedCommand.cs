using Discord.Commands;
using NadekoBot.Services;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NadekoBot.Attributes
{
    public class LocalizedCommandAttribute : CommandAttribute
    {
        public LocalizedCommandAttribute([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_text").Split(' ')[0])
        {

        }
    }
}
