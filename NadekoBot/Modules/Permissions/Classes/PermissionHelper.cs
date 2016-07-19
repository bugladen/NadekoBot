using Discord;
using Discord.Commands;
using Discord.Modules;
using System;
using System.Linq;

namespace NadekoBot.Modules.Permissions.Classes
{
    internal static class PermissionHelper
    {
        public static bool ValidateBool(string passedArg)
        {
            if (string.IsNullOrWhiteSpace(passedArg))
            {
                throw new ArgumentException("No value supplied! Missing argument");
            }
            switch (passedArg.ToLower())
            {
                case "1":
                case "t":
                case "true":
                case "enable":
                case "enabled":
                case "allow":
                case "unban":
                    return true;
                case "0":
                case "f":
                case "false":
                case "disable":
                case "disabled":
                case "disallow":
                case "ban":
                    return false;
                default:
                    throw new ArgumentException("Did not receive a valid boolean value");
            }
        }

        internal static string ValidateModule(string mod)
        {
            if (string.IsNullOrWhiteSpace(mod))
                throw new ArgumentNullException(nameof(mod));

            foreach (var m in NadekoBot.Client.GetService<ModuleService>().Modules)
            {
                if (m.Name.ToLower().Equals(mod.Trim().ToLower()))
                    return m.Name;
            }
            throw new ArgumentException("That module does not exist.");
        }

        internal static string ValidateCommand(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentNullException(nameof(commandText));

            var normalizedCmdTxt = commandText.Trim().ToUpperInvariant();

            foreach (var com in NadekoBot.Client.GetService<CommandService>().AllCommands)
            {
                if (com.Text.ToUpperInvariant().Equals(normalizedCmdTxt) || com.Aliases.Select(c=>c.ToUpperInvariant()).Contains(normalizedCmdTxt))
                    return com.Text;
            }
            throw new NullReferenceException("That command does not exist.");
        }

        internal static Role ValidateRole(Server server, string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentNullException(nameof(roleName));

            if (roleName.Trim() == "everyone")
                roleName = "@everyone";
            var role = server.FindRoles(roleName.Trim()).FirstOrDefault();
            if (role == null)
                throw new NullReferenceException("That role does not exist.");
            return role;
        }

        internal static Channel ValidateChannel(Server server, string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentNullException(nameof(channelName));
            var channel = server.FindChannels(channelName.Trim(), ChannelType.Text).FirstOrDefault();
            if (channel == null)
                throw new NullReferenceException("That channel does not exist.");
            return channel;
        }

        internal static User ValidateUser(Server server, string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException(nameof(userName));
            var user = server.FindUsers(userName.Trim()).FirstOrDefault();
            if (user == null)
                throw new NullReferenceException("That user does not exist.");
            return user;
        }
    }
}
