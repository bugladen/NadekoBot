using Discord.Commands;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NadekoBot.Attributes
{
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        public LocalizedDescriptionAttribute([CallerMemberName] string memberName="") : base(Localization.LoadString(memberName.ToLowerInvariant()+"_description"))
        {

        }
    }
}
