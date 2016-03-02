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
    public abstract class DiscordCommand
    {
        /// <summary>
        /// Client at the moment of creating this object
        /// </summary>
        public DiscordClient client { get; set; }

        /// <summary>
        /// Constructor of the base class
        /// </summary>
        /// <param name="cb">CommandBuilder which will be modified</param>
        protected DiscordCommand()
        {
            client = NadekoBot.Client;
        }
        /// <summary>
        /// Function containing the behaviour of the command.
        /// </summary>
        /// <param name="client">Client who will handle the message sending, etc, if any.</param>
        /// <returns></returns>
        public abstract Func<CommandEventArgs,Task> DoFunc();

        /// <summary>
        /// Initializes the CommandBuilder with values using CommandGroupBuilder
        /// </summary>
        public abstract void Init(CommandGroupBuilder cgb);
    }
}
