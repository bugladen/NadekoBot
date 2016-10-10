using Discord.Commands;
using NadekoBot.Services;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NadekoBot.Attributes
{
    public class NadekoCommand : CommandAttribute
    {
        public NadekoCommand([CallerMemberName] string memberName="") : base(Localization.LoadCommandString(memberName.ToLowerInvariant() + "_cmd").Split(' ')[0])
        {

        }
    }
}
