using System;

namespace NadekoBot.Modules.Searches.Exceptions
{
    public class TagBlacklistedException : Exception
    {
        public TagBlacklistedException() : base("Tag you used is blacklisted.")
        {

        }
    }
}
