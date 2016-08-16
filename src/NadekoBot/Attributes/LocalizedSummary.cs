using Discord.Commands;
using NadekoBot.Services;
using System.Runtime.CompilerServices;

namespace NadekoBot.Attributes
{
    public class LocalizedSummaryAttribute : SummaryAttribute
    {
        public LocalizedSummaryAttribute([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_summary"))
        {

        }
    }
}
