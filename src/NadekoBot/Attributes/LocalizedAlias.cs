using Discord.Commands;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Attributes
{
    public class LocalizedAliasAttribute : AliasAttribute
    {
        public LocalizedAliasAttribute([CallerMemberName] string memberName = "") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_text").Split(' ').Skip(1).ToArray())
        {
        }
    }
}
