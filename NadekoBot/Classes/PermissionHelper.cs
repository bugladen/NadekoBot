using Discord.Commands;
using Discord.Modules;
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
                case "enable":
                    return true;
                case "f":
                case "false":
                case "disable":
                    return false;
                default:
                    throw new System.ArgumentException("Did not receive a valid boolean value");
            }
        }

        internal static bool ValidateModule(string mod)
        {
            if (string.IsNullOrWhiteSpace(mod))
                throw new ArgumentNullException(nameof(mod));
            foreach (var m in NadekoBot.client.Modules().Modules) {
                if(m.Name.ToLower().Equals(mod))
                    return true;
            }
            return false;
        }

        internal static bool ValidateCommand(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentNullException(nameof(commandText));
            foreach (var com in NadekoBot.client.Commands().AllCommands) {
                if (com.Text == commandText)
                    return true;
            }
            return false;
        }
    }
}
