using Discord.Commands;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NadekoBot.Attributes
{
    public class LocalizedSummaryAttribute : SummaryAttribute
    {
        public LocalizedSummaryAttribute([CallerMemberName] string memberName="") : base(Localization.LoadString(memberName.ToLowerInvariant() + "_summary"))
        {

        }
    }
}
