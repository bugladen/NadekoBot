using Discord.Commands;
using NadekoBot.Services;
using System.Runtime.CompilerServices;

namespace NadekoBot.Attributes
{
    public class Usage : RemarksAttribute
    {
        public Usage([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant()+"_usage"))
        {

        }
    }
}
