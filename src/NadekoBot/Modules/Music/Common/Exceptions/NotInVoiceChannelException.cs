using System;

namespace NadekoBot.Modules.Music.Common.Exceptions
{
    // todo use this
    public class NotInVoiceChannelException : Exception
    {
        public NotInVoiceChannelException(string message) : base(message)
        {
        }

        public NotInVoiceChannelException() : base("You're not in the voice channel on this server.") { }
    }
}
