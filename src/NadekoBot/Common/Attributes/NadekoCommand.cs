using System.Runtime.CompilerServices;
using Discord.Commands;
using NadekoBot.Services;
using NadekoBot.Services.Impl;

namespace NadekoBot.Common.Attributes
{
    public class NadekoCommand : CommandAttribute
    {
        public NadekoCommand([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_cmd").Split(' ')[0])
        {

        }
    }
}
