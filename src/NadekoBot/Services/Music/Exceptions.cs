using System;

namespace NadekoBot.Services.Music
{
    public class PlaylistFullException : Exception
    {
        public PlaylistFullException(string message) : base(message)
        {
        }
        public PlaylistFullException() : base("Queue is full.") { }
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
