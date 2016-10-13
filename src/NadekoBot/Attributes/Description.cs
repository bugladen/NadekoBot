using Discord.Commands;
using NadekoBot.Services;
using System.Runtime.CompilerServices;

namespace NadekoBot.Attributes
{
    public class Description : SummaryAttribute
    {
        public Description([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_desc"))
        {

        }
    }
}
