using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Modules.Permissions
{
    public static class PermissionExtensions
    {
        public static bool CheckPermissions(this IEnumerable<Permission> permsEnumerable, IUserMessage message, CommandInfo command)
        {
            var perms = permsEnumerable as List<Permission> ?? permsEnumerable.ToList();
            int throwaway;
            return perms.CheckPermissions(message, command.Name, command.Module.Name, out throwaway);
        }

        public static bool CheckPermissions(this IEnumerable<Permission> permsEnumerable, IUserMessage message, string commandName, string moduleName)
        {
            var perms = permsEnumerable as List<Permission> ?? permsEnumerable.ToList();
            int throwaway;
            return perms.CheckPermissions(message, commandName, moduleName, out throwaway);
        }

        public static bool CheckPermissions(this IEnumerable<Permission> permsEnumerable, IUserMessage message, string commandName, string moduleName, out int permIndex)
        {
            var perms = permsEnumerable as List<Permission> ?? permsEnumerable.ToList();

            for (int i = 0; i < perms.Count; i++)
            {
                var perm = perms[i];

                var result = perm.CheckPermission(message, commandName, moduleName);

                if (result == null)
                {
                    continue;
                }
                else
                {
                    permIndex = i;
                    return result.Value;
                }
            }
            permIndex = -1; //defaut behaviour
            return true;
        }

        //null = not applicable
        //true = applicable, allowed
        //false = applicable, not allowed
        public static bool? CheckPermission(this Permission perm, IUserMessage message, string commandName, string moduleName)
        {
            if (!((perm.SecondaryTarget == SecondaryPermissionType.Command &&
                    perm.SecondaryTargetName.ToLowerInvariant() == commandName.ToLowerInvariant()) ||
                (perm.SecondaryTarget == SecondaryPermissionType.Module &&
                    perm.SecondaryTargetName.ToLowerInvariant() == moduleName.ToLowerInvariant()) ||
                    perm.SecondaryTarget == SecondaryPermissionType.AllModules))
                return null;

            var guildUser = message.Author as IGuildUser;

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
                    if (guildUser == null)
                        break;
                    if (guildUser.RoleIds.Contains(perm.PrimaryTargetId))
                        return perm.State;
                    break;
                case PrimaryPermissionType.Server:
                    if (guildUser == null)
                        break;
                    return perm.State;
            }
            return null;
        }

        public static string GetCommand(this Permission perm, SocketGuild guild = null)
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
                case PrimaryPermissionType.Server:
                    com += "s";
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
                case SecondaryPermissionType.AllModules:
                    com = "a" + com + "m";
                    break;
            }
            com += " " + (perm.SecondaryTargetName != "*" ? perm.SecondaryTargetName + " " : "") + (perm.State ? "enable" : "disable") + " ";

            switch (perm.PrimaryTarget)
            {
                case PrimaryPermissionType.User:
                    if (guild == null)
                        com += $"<@{perm.PrimaryTargetId}>";
                    else
                        com += guild.GetUser(perm.PrimaryTargetId).ToString() ?? $"<@{perm.PrimaryTargetId}>";
                    break;
                case PrimaryPermissionType.Channel:
                    com += $"<#{perm.PrimaryTargetId}>";
                    break;
                case PrimaryPermissionType.Role:
                    if(guild == null)
                        com += $"<@&{perm.PrimaryTargetId}>";
                    else
                        com += guild.GetRole(perm.PrimaryTargetId).ToString() ?? $"<@{perm.PrimaryTargetId}>";
                    break;
                case PrimaryPermissionType.Server:
                    break;
            }

            return NadekoBot.ModulePrefixes[typeof(Permissions).Name] + com;
        }

        public static void Prepend(this Permission perm, Permission toAdd)
        {
            perm = perm.GetRoot();

            perm.Previous = toAdd;
            toAdd.Next = perm;
        }

        /* /this can't work if index < 0 and perm isn't roo
        public static void Insert(this Permission perm, int index, Permission toAdd)
        {
            if (index < 0)
                throw new IndexOutOfRangeException();

            if (index == 0)
            {
                perm.Prepend(toAdd);
                return;
            }

            var atIndex = perm;
            var i = 0;
            while (i != index)
            {
                atIndex = atIndex.Next;
                i++;
                if (atIndex == null)
                    throw new IndexOutOfRangeException();
            }
            var previous = atIndex.Previous;

            //connect right side
            atIndex.Previous = toAdd;
            toAdd.Next = atIndex;

            //connect left side
            toAdd.Previous = previous;
            previous.Next = toAdd;
        }
        */
        public static Permission RemoveAt(this Permission perm, int index)
        {
            if (index <= 0) //can't really remove at 0, that means deleting the element right now. Just use perm.Next if its 0
                throw new IndexOutOfRangeException();

            var toRemove = perm;
            var i = 0;
            while (i != index)
            {
                toRemove = toRemove.Next;
                i++;
                if (toRemove == null)
                    throw new IndexOutOfRangeException();
            }

            toRemove.Previous.Next = toRemove.Next;
            if (toRemove.Next != null)
                toRemove.Next.Previous = toRemove.Previous;
            return toRemove;
        }

        public static Permission GetAt(this Permission perm, int index)
        {
            if (index < 0)
                throw new IndexOutOfRangeException();
            var temp = perm;
            while (index > 0) { temp = temp?.Next; index--; }
            if (temp == null)
                throw new IndexOutOfRangeException();
            return temp;
        }

        public static int Count(this Permission perm)
        {
            var i = 1;
            var temp = perm;
            while ((temp = temp.Next) != null) { i++; }
            return i;
        }

        public static IEnumerable<Permission> AsEnumerable(this Permission perm)
        {
            do yield return perm;
            while ((perm = perm.Next) != null);
        }

        public static Permission GetRoot(this Permission perm)
        {
            Permission toReturn;
            do toReturn = perm;
            while ((perm = perm.Previous) != null);
            return toReturn;
        }
    }
}
