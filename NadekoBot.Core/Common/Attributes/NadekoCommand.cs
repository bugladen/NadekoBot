using System.Runtime.CompilerServices;
using Discord.Commands;
using NadekoBot.Core.Services.Impl;

namespace NadekoBot.Common.Attributes
{
    public class NadekoCommand : CommandAttribute
    {
        public NadekoCommand([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Cmd.Split(' ')[0])
        {

        }
    }
}
