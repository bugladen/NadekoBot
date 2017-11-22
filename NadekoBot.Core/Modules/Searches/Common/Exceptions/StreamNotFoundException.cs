using System;

namespace NadekoBot.Modules.Searches.Common.Exceptions
{
    public class StreamNotFoundException : Exception
    {
        public StreamNotFoundException(string message) : base($"Stream '{message}' not found.")
        {
        }
    }
}
