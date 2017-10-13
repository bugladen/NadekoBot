using System.Runtime.CompilerServices;
using Discord.Commands;
using NadekoBot.Core.Services.Impl;

namespace NadekoBot.Common.Attributes
{
    public class Description : SummaryAttribute
    {
        public Description([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Desc)
        {

        }
    }
}
