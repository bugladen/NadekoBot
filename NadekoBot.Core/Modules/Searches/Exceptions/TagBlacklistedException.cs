using System;

namespace NadekoBot.Modules.Searches.Exceptions
{
    public class TagBlacklistedException : Exception
    {
        public TagBlacklistedException() : base("Tag you used is blacklisted.")
        {

        }

        public TagBlacklistedException(string message) : base(message)
        {
        }

        public TagBlacklistedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
