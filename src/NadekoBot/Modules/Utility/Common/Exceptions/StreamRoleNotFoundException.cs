using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility.Common.Exceptions
{
    public class StreamRoleNotFoundException : Exception
    {
        public StreamRoleNotFoundException() : base("Stream role wasn't found.")
        {
        }
    }
}
