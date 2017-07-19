using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility.Common.Exceptions
{
    public class StreamRolePermissionException : Exception
    {
        public StreamRolePermissionException() : base("Stream role was unable to be applied.")
        {
        }
    }
}
