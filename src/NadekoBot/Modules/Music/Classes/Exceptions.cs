using System;

namespace NadekoBot.Modules.Music.Classes
{
    class PlaylistFullException : Exception
    {
        public PlaylistFullException(string message) : base(message)
        {
        }
        public PlaylistFullException() : base("Queue is full.") { }
    }

    class SongNotFoundException : Exception
    {
        public SongNotFoundException(string message) : base(message)
        {
        }
        public SongNotFoundException() : base("Song is not found.") { }
    }
}
