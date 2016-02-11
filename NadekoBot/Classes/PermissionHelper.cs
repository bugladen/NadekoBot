using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NadekoBot.Classes
{
    static class PermissionHelper
    {
        public static bool ValidateBool(string passedArg)
        {
            if (string.IsNullOrEmpty(passedArg.Trim()))
            {
                throw new System.ArgumentException("No value supplied! Missing argument");
            }
            switch (passedArg.ToLower())
            {
                case "t":
                case "true":
                case "enabled":
                    return true;
                case "f":
                case "false":
                case "disabled":
                    return false;
                default:
                    throw new System.ArgumentException("Did not receive a valid boolean value");
            }
        }

        internal static void ValidateModule(string v)
        {
            return;
        }

        internal static void ValidateCommand(string v)
        {
            return;
        }
    }
}
