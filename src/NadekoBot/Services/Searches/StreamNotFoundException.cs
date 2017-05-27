using System;

namespace NadekoBot.Services.Searches
{
    public class StreamNotFoundException : Exception
    {
        public StreamNotFoundException(string message) : base($"Stream '{message}' not found.")
        {
        }
    }
}
