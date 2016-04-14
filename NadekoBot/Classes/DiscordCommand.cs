using Discord.Commands;
using NadekoBot.Modules;

namespace NadekoBot.Classes
{
    /// <summary>
    /// Base DiscordCommand Class.
    /// Inherit this class to create your own command.
    /// </summary>
    internal abstract class DiscordCommand
    {

        /// <summary>
        /// Parent module
        /// </summary>
        protected DiscordModule Module { get; }

        /// <summary>
        /// Creates a new instance of discord command,
        /// use ": base(module)" in the derived class'
        /// constructor to make sure module is assigned
        /// </summary>
        /// <param name="module">Module this command resides in</param>
        protected DiscordCommand(DiscordModule module)
        {
            this.Module = module;
        }

        /// <summary>
        /// Initializes the CommandBuilder with values using CommandGroupBuilder
        /// </summary>
        internal abstract void Init(CommandGroupBuilder cgb);
    }
}
