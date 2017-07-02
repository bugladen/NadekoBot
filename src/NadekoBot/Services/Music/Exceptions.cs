using System;

namespace NadekoBot.Services.Music
{
    public class QueueFullException : Exception
    {
        public QueueFullException(string message) : base(message)
        {
        }
        public QueueFullException() : base("Queue is full.") { }
    }

    public class SongNotFoundException : Exception
    {
        public SongNotFoundException(string message) : base(message)
        {
        }
        public SongNotFoundException() : base("Song is not found.") { }
    }
    public class NotInVoiceChannelException : Exception
    {
        public NotInVoiceChannelException(string message) : base(message)
        {
        }

        public NotInVoiceChannelException() : base("You're not in the voice channel on this server.") { }
    }
}
