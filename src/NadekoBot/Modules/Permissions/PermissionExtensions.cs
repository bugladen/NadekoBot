using Discord;
using Discord.Commands;
using NadekoBot.Services.Database;
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

        public static void Add(this Permission perm, Permission toAdd)
        {
            var last = perm;
            while (last.Next != null)
            {
                last = last.Next;
            }

            toAdd.Previous = last;
            last.Next = toAdd;
            toAdd.Next = null;
        }

        public static void Insert(this Permission perm, int index, Permission toAdd)
        {
            if (index < 0)
                throw new IndexOutOfRangeException();

            if (index == 0)
            {
                perm.Previous = toAdd;
                toAdd.Next = perm;
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

        public static Permission RemoveAt(this Permission perm, int index)
        {
            if (index < 0)
                throw new IndexOutOfRangeException();

            if (index == 0)
            {
                perm.Next.Previous = null;
                perm.Next = null;
                return perm;
            }

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
