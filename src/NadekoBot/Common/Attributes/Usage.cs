using System.Runtime.CompilerServices;
using Discord.Commands;
using NadekoBot.Services;
using NadekoBot.Services.Impl;

namespace NadekoBot.Common.Attributes
{
    public class Usage : RemarksAttribute
    {
        public Usage([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant()+"_usage"))
        {

        }
    }
}
