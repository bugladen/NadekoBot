using System.Linq;
using System.Runtime.CompilerServices;
using Discord.Commands;
using NadekoBot.Services;
using NadekoBot.Services.Impl;

namespace NadekoBot.Common.Attributes
{
    public class Aliases : AliasAttribute
    {
        public Aliases([CallerMemberName] string memberName = "") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_cmd").Split(' ').Skip(1).ToArray())
        {
        }
    }
}
