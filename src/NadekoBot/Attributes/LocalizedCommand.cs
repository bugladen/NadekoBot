using Discord.Commands;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NadekoBot.Attributes
{
    public class LocalizedCommandAttribute : CommandAttribute
    {
        public LocalizedCommandAttribute([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_text"))
        {

        }
    }
}
