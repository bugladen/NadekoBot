using Discord;
using Discord.Commands;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions
{
    public static class PermissionExtensions
    {
        public static bool CheckPermissions(this IEnumerable<Permission> permsEnumerable, IUserMessage message, Command command)
        {
            var perms = permsEnumerable as List<Permission> ?? permsEnumerable.ToList();
            int throwaway;
            return perms.CheckPermissions(message, command, out throwaway);
        }

        public static bool CheckPermissions(this IEnumerable<Permission> permsEnumerable, IUserMessage message, Command command, out int permIndex)
        {
            permsEnumerable = permsEnumerable.Reverse();
            var perms = permsEnumerable as List<Permission> ?? permsEnumerable.ToList();

            for (int i = 0; i < perms.Count; i++)
            {
                var perm = perms[i];

                var result = perm.CheckPermission(message, command);

                if (result == null)
                {
                    continue;
                }
                else
                {
                    permIndex = i + 1;
                    return result.Value;
                }
            }
            permIndex = -1; //defaut behaviour
            return true;
        }

        //null = not applicable
        //true = applicable, allowed
        //false = applicable, not allowed
        public static bool? CheckPermission(this Permission perm, IUserMessage message, Command command)
        {
            if (!((perm.SecondaryTarget == SecondaryPermissionType.Command &&
                    perm.SecondaryTargetName == command.Text.ToLowerInvariant()) ||
                ((perm.SecondaryTarget == SecondaryPermissionType.Module || perm.SecondaryTarget == SecondaryPermissionType.AllCommands) &&
                    perm.SecondaryTargetName == command.Module.Name.ToLowerInvariant()) || 
                    perm.SecondaryTarget == SecondaryPermissionType.AllModules || 
                    (perm.SecondaryTarget == SecondaryPermissionType.AllCommands && perm.SecondaryTargetName == command.Module.Name.ToLowerInvariant())))
                return null;

            switch (perm.PrimaryTarget)
            {
                case PrimaryPermissionType.User:
                    if (perm.PrimaryTargetId == message.Author.Id)
                        return perm.State;
                    break;
                case PrimaryPermissionType.Channel:
                    if (perm.PrimaryTargetId == message.Channel.Id)
                        return perm.State;
                    break;
                case PrimaryPermissionType.Role:
                    var guildUser = message.Author as IGuildUser;
                    if (guildUser == null)
                        break;
                    if (guildUser.Roles.Any(r => r.Id == perm.PrimaryTargetId))
                        return perm.State;
                    break;
            }
            return null;
        }

        public static string GetCommand(this Permission perm)
        {
            var com = "";
            switch (perm.PrimaryTarget)
            {
                case PrimaryPermissionType.User:
                    com += "u";
                    break;
                case PrimaryPermissionType.Channel:
                    com += "c";
                    break;
                case PrimaryPermissionType.Role:
                    com += "r";
                    break;
            }

            switch (perm.SecondaryTarget)
            {
                case SecondaryPermissionType.Module:
                    com += "m";
                    break;
                case SecondaryPermissionType.Command:
                    com += "c";
                    break;
                case SecondaryPermissionType.AllCommands:
                    com = "a" + com + "c";
                    break;
                case SecondaryPermissionType.AllModules:
                    com = "a" + com + "m";
                    break;
            }
            com += " " + (perm.SecondaryTargetName != "*" ? perm.SecondaryTargetName + " " : "") + (perm.State ? "enable" : "disable") + " ";

            switch (perm.PrimaryTarget)
            {
                case PrimaryPermissionType.User:
                    com += $"<@{perm.PrimaryTargetId}>";
                    break;
                case PrimaryPermissionType.Channel:
                    com += $"<#{perm.PrimaryTargetId}>";
                    break;
                case PrimaryPermissionType.Role:
                    com += $"<@&{perm.PrimaryTargetId}>";
                    break;
            }

            return NadekoBot.ModulePrefixes[typeof(Permissions).Name] + com;
        }

    }
}
