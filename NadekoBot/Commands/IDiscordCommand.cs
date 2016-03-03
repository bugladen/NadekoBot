using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace NadekoBot.Commands
{
    /// <summary>
    /// Base DiscordCommand Class.
    /// Inherit this class to create your own command.
    /// </summary>
    public interface IDiscordCommand
    {
        /// <summary>
        /// Initializes the CommandBuilder with values using CommandGroupBuilder
        /// </summary>
        void Init(CommandGroupBuilder cgb);
    }
}
