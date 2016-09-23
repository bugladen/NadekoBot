using Discord.Commands;
using NadekoBot.Services;
using System.Runtime.CompilerServices;

namespace NadekoBot.Attributes
{
    public class LocalizedRemarksAttribute : RemarksAttribute
    {
        public LocalizedRemarksAttribute([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant()+"_desc"))
        {

        }
    }
}
