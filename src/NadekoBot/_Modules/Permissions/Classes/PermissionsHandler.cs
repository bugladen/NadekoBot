using Discord;
using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions.Classes
{
    public static class PermissionsHandler
    {
        public static ConcurrentDictionary<ulong, ServerPermissions> PermissionsDict =
            new ConcurrentDictionary<ulong, ServerPermissions>();

        public enum PermissionBanType
        {
            None, ServerBanCommand, ServerBanModule,
            ChannelBanCommand, ChannelBanModule, RoleBanCommand,
            RoleBanModule, UserBanCommand, UserBanModule
        }


        public static void Initialize()
        {
            Console.WriteLine("Reading from the permission files.");
            Directory.CreateDirectory("data/permissions");
            foreach (var file in Directory.EnumerateFiles("data/permissions/"))
            {
                try
                {
                    var strippedFileName = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(strippedFileName)) continue;
                    var id = ulong.Parse(strippedFileName);
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerPermissions>(File.ReadAllText(file));
                    PermissionsDict.TryAdd(id, data);
                }
                catch { }
            }
            Console.WriteLine("Permission initialization complete.");
        }

        internal static Permissions GetRolePermissionsById(Server server, ulong id)
        {
            ServerPermissions serverPerms;
            if (!PermissionsDict.TryGetValue(server.Id, out serverPerms))
                return null;

            Permissions toReturn;
            serverPerms.RolePermissions.TryGetValue(id, out toReturn);
            return toReturn;
        }

        internal static Permissions GetUserPermissionsById(Server server, ulong id)
        {
            ServerPermissions serverPerms;
            if (!PermissionsDict.TryGetValue(server.Id, out serverPerms))
                return null;

            Permissions toReturn;
            serverPerms.UserPermissions.TryGetValue(id, out toReturn);
            return toReturn;
        }

        internal static Permissions GetChannelPermissionsById(Server server, ulong id)
        {
            ServerPermissions serverPerms;
            if (!PermissionsDict.TryGetValue(server.Id, out serverPerms))
                return null;

            Permissions toReturn;
            serverPerms.ChannelPermissions.TryGetValue(id, out toReturn);
            return toReturn;
        }

        internal static Permissions GetServerPermissions(Server server)
        {
            ServerPermissions serverPerms;
            return !PermissionsDict.TryGetValue(server.Id, out serverPerms) ? null : serverPerms.Permissions;
        }

        internal static PermissionBanType GetPermissionBanType(Command command, User user, Channel channel)
        {
            var server = user.Server;
            ServerPermissions serverPerms = PermissionsDict.GetOrAdd(server.Id, id => new ServerPermissions(id, server.Name));
            bool val;
            Permissions perm;
            //server
            if (serverPerms.Permissions.Modules.TryGetValue(command.Category, out val) && val == false)
                return PermissionBanType.ServerBanModule;
            if (serverPerms.Permissions.Commands.TryGetValue(command.Text, out val) && val == false)
                return PermissionBanType.ServerBanCommand;
            //channel
            if (serverPerms.ChannelPermissions.TryGetValue(channel.Id, out perm) &&
                perm.Modules.TryGetValue(command.Category, out val) && val == false)
                return PermissionBanType.ChannelBanModule;
            if (serverPerms.ChannelPermissions.TryGetValue(channel.Id, out perm) &&
                perm.Commands.TryGetValue(command.Text, out val) && val == false)
                return PermissionBanType.ChannelBanCommand;

            //ROLE PART - TWO CASES
            // FIRST CASE:
            // IF EVERY ROLE USER HAS IS BANNED FROM THE MODULE,
            // THAT MEANS USER CANNOT RUN THIS COMMAND
            // IF AT LEAST ONE ROLE EXIST THAT IS NOT BANNED,
            // USER CAN RUN THE COMMAND
            var foundNotBannedRole = false;
            foreach (var role in user.Roles)
            {
                //if every role is banned from using the module -> rolebanmodule
                if (serverPerms.RolePermissions.TryGetValue(role.Id, out perm) &&
                perm.Modules.TryGetValue(command.Category, out val) && val == false)
                    continue;
                foundNotBannedRole = true;
                break;
            }
            if (!foundNotBannedRole)
                return PermissionBanType.RoleBanModule;

            // SECOND CASE:
            // IF EVERY ROLE USER HAS IS BANNED FROM THE COMMAND,
            // THAT MEANS USER CANNOT RUN THAT COMMAND
            // IF AT LEAST ONE ROLE EXISTS THAT IS NOT BANNED,
            // USER CAN RUN THE COMMAND
            foundNotBannedRole = false;
            foreach (var role in user.Roles)
            {
                //if every role is banned from using the module -> rolebanmodule
                if (serverPerms.RolePermissions.TryGetValue(role.Id, out perm) &&
                perm.Commands.TryGetValue(command.Text, out val) && val == false)
                    continue;
                else
                {
                    foundNotBannedRole = true;
                    break;
                }
            }
            if (!foundNotBannedRole)
                return PermissionBanType.RoleBanCommand;

            //user
            if (serverPerms.UserPermissions.TryGetValue(user.Id, out perm) &&
                perm.Modules.TryGetValue(command.Category, out val) && val == false)
                return PermissionBanType.UserBanModule;
            if (serverPerms.UserPermissions.TryGetValue(user.Id, out perm) &&
                perm.Commands.TryGetValue(command.Text, out val) && val == false)
                return PermissionBanType.UserBanCommand;

            return PermissionBanType.None;
        }

        private static Task WriteServerToJson(ServerPermissions serverPerms) => Task.Run(() =>
        {
            string pathToFile = $"data/permissions/{serverPerms.Id}.json";
            File.WriteAllText(pathToFile,
                Newtonsoft.Json.JsonConvert.SerializeObject(serverPerms, Newtonsoft.Json.Formatting.Indented));
        });

        public static Task WriteToJson() => Task.Run(() => 
        {
            Directory.CreateDirectory("data/permissions/");
            foreach (var kvp in PermissionsDict)
            {
                WriteServerToJson(kvp.Value);
            }
        });

        public static string GetServerPermissionsRoleName(Server server)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            return serverPerms.PermissionsControllerRole;
        }

        internal static async Task SetPermissionsRole(Server server, string roleName)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            serverPerms.PermissionsControllerRole = roleName;
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        internal static async Task SetVerbosity(Server server, bool val)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            serverPerms.Verbose = val;
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        internal static async Task CopyRolePermissions(Role fromRole, Role toRole)
        {
            var server = fromRole.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            var from = GetRolePermissionsById(server, fromRole.Id);
            if (from == null)
                serverPerms.RolePermissions.Add(fromRole.Id, from = new Permissions(fromRole.Name));
            var to = GetRolePermissionsById(server, toRole.Id);
            if (to == null)
                serverPerms.RolePermissions.Add(toRole.Id, to = new Permissions(toRole.Name));

            to.CopyFrom(from);

            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        internal static async Task CopyChannelPermissions(Channel fromChannel, Channel toChannel)
        {
            var server = fromChannel.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            var from = GetChannelPermissionsById(server, fromChannel.Id);
            if (from == null)
                serverPerms.ChannelPermissions.Add(fromChannel.Id, from = new Permissions(fromChannel.Name));
            var to = GetChannelPermissionsById(server, toChannel.Id);
            if (to == null)
                serverPerms.ChannelPermissions.Add(toChannel.Id, to = new Permissions(toChannel.Name));

            to.CopyFrom(from);

            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        internal static async Task CopyUserPermissions(User fromUser, User toUser)
        {
            var server = fromUser.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            var from = GetUserPermissionsById(server, fromUser.Id);
            if (from == null)
                serverPerms.UserPermissions.Add(fromUser.Id, from = new Permissions(fromUser.Name));
            var to = GetUserPermissionsById(server, toUser.Id);
            if (to == null)
                serverPerms.UserPermissions.Add(toUser.Id, to = new Permissions(toUser.Name));

            to.CopyFrom(from);

            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetServerModulePermission(Server server, string moduleName, bool value)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            var modules = serverPerms.Permissions.Modules;
            if (modules.ContainsKey(moduleName))
                modules[moduleName] = value;
            else
                modules.TryAdd(moduleName, value);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetServerCommandPermission(Server server, string commandName, bool value)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            var commands = serverPerms.Permissions.Commands;
            if (commands.ContainsKey(commandName))
                commands[commandName] = value;
            else
                commands.TryAdd(commandName, value);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetChannelModulePermission(Channel channel, string moduleName, bool value)
        {
            var server = channel.Server;

            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            if (!serverPerms.ChannelPermissions.ContainsKey(channel.Id))
                serverPerms.ChannelPermissions.Add(channel.Id, new Permissions(channel.Name));

            var modules = serverPerms.ChannelPermissions[channel.Id].Modules;

            if (modules.ContainsKey(moduleName))
                modules[moduleName] = value;
            else
                modules.TryAdd(moduleName, value);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetChannelCommandPermission(Channel channel, string commandName, bool value)
        {
            var server = channel.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            if (!serverPerms.ChannelPermissions.ContainsKey(channel.Id))
                serverPerms.ChannelPermissions.Add(channel.Id, new Permissions(channel.Name));

            var commands = serverPerms.ChannelPermissions[channel.Id].Commands;

            if (commands.ContainsKey(commandName))
                commands[commandName] = value;
            else
                commands.TryAdd(commandName, value);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetRoleModulePermission(Role role, string moduleName, bool value)
        {
            var server = role.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            if (!serverPerms.RolePermissions.ContainsKey(role.Id))
                serverPerms.RolePermissions.Add(role.Id, new Permissions(role.Name));

            var modules = serverPerms.RolePermissions[role.Id].Modules;

            if (modules.ContainsKey(moduleName))
                modules[moduleName] = value;
            else
                modules.TryAdd(moduleName, value);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetRoleCommandPermission(Role role, string commandName, bool value)
        {
            var server = role.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            if (!serverPerms.RolePermissions.ContainsKey(role.Id))
                serverPerms.RolePermissions.Add(role.Id, new Permissions(role.Name));

            var commands = serverPerms.RolePermissions[role.Id].Commands;

            if (commands.ContainsKey(commandName))
                commands[commandName] = value;
            else
                commands.TryAdd(commandName, value);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetUserModulePermission(User user, string moduleName, bool value)
        {
            var server = user.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            if (!serverPerms.UserPermissions.ContainsKey(user.Id))
                serverPerms.UserPermissions.Add(user.Id, new Permissions(user.Name));

            var modules = serverPerms.UserPermissions[user.Id].Modules;

            if (modules.ContainsKey(moduleName))
                modules[moduleName] = value;
            else
                modules.TryAdd(moduleName, value);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetUserCommandPermission(User user, string commandName, bool value)
        {
            var server = user.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));
            if (!serverPerms.UserPermissions.ContainsKey(user.Id))
                serverPerms.UserPermissions.Add(user.Id, new Permissions(user.Name));

            var commands = serverPerms.UserPermissions[user.Id].Commands;

            if (commands.ContainsKey(commandName))
                commands[commandName] = value;
            else
                commands.TryAdd(commandName, value);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetServerWordPermission(Server server, bool value)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            serverPerms.Permissions.FilterWords = value;
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetChannelWordPermission(Channel channel, bool value)
        {
            var server = channel.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            if (!serverPerms.ChannelPermissions.ContainsKey(channel.Id))
                serverPerms.ChannelPermissions.Add(channel.Id, new Permissions(channel.Name));

            serverPerms.ChannelPermissions[channel.Id].FilterWords = value;
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetServerFilterInvitesPermission(Server server, bool value)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            serverPerms.Permissions.FilterInvites = value;
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetChannelFilterInvitesPermission(Channel channel, bool value)
        {
            var server = channel.Server;
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));

            if (!serverPerms.ChannelPermissions.ContainsKey(channel.Id))
                serverPerms.ChannelPermissions.Add(channel.Id, new Permissions(channel.Name));

            serverPerms.ChannelPermissions[channel.Id].FilterInvites = value;
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task SetCommandCooldown(Server server, string commandName, int value)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));
            if (value == 0) {
                int throwaway;
                serverPerms.CommandCooldowns.TryRemove(commandName, out throwaway);
            }
            else {
                serverPerms.CommandCooldowns.AddOrUpdate(commandName, value, (str, v) => value);
            }

            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }

        public static async Task AddFilteredWord(Server server, string word)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));
            if (serverPerms.Words.Contains(word))
                throw new InvalidOperationException("That word is already banned.");
            serverPerms.Words.Add(word);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }
        public static async Task RemoveFilteredWord(Server server, string word)
        {
            var serverPerms = PermissionsDict.GetOrAdd(server.Id,
                new ServerPermissions(server.Id, server.Name));
            if (!serverPerms.Words.Contains(word))
                throw new InvalidOperationException("That word is not banned.");
            serverPerms.Words.Remove(word);
            await WriteServerToJson(serverPerms).ConfigureAwait(false);
        }
    }
    /// <summary>
    /// Holds a permission list
    /// </summary>
    public class Permissions
    {
        /// <summary>
        /// Name of the parent object whose permissions these are
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Module name with allowed/disallowed
        /// </summary>
        public ConcurrentDictionary<string, bool> Modules { get; set; }
        /// <summary>
        /// Command name with allowed/disallowed
        /// </summary>
        public ConcurrentDictionary<string, bool> Commands { get; set; }
        /// <summary>
        /// Should the bot filter invites to other discord servers (and ref links in the future)
        /// </summary>
        public bool FilterInvites { get; set; }
        /// <summary>
        /// Should the bot filter words which are specified in the Words hashset
        /// </summary>
        public bool FilterWords { get; set; }

        public Permissions(string name)
        {
            Name = name;
            Modules = new ConcurrentDictionary<string, bool>();
            Commands = new ConcurrentDictionary<string, bool>();
            FilterInvites = false;
            FilterWords = false;
        }

        public void CopyFrom(Permissions other)
        {
            Modules.Clear();
            foreach (var mp in other.Modules)
                Modules.AddOrUpdate(mp.Key, mp.Value, (s, b) => mp.Value);
            Commands.Clear();
            foreach (var cp in other.Commands)
                Commands.AddOrUpdate(cp.Key, cp.Value, (s, b) => cp.Value);
            FilterInvites = other.FilterInvites;
            FilterWords = other.FilterWords;
        }

        public override string ToString()
        {
            var toReturn = "";
            var bannedModules = Modules.Where(kvp => kvp.Value == false);
            var bannedModulesArray = bannedModules as KeyValuePair<string, bool>[] ?? bannedModules.ToArray();
            if (bannedModulesArray.Any())
            {
                toReturn += "`Banned Modules:`\n";
                toReturn = bannedModulesArray.Aggregate(toReturn, (current, m) => current + $"\t`[x]  {m.Key}`\n");
            }
            var bannedCommands = Commands.Where(kvp => kvp.Value == false);
            var bannedCommandsArr = bannedCommands as KeyValuePair<string, bool>[] ?? bannedCommands.ToArray();
            if (bannedCommandsArr.Any())
            {
                toReturn += "`Banned Commands:`\n";
                toReturn = bannedCommandsArr.Aggregate(toReturn, (current, c) => current + $"\t`[x]  {c.Key}`\n");
            }
            return toReturn;
        }
    }

    public class ServerPermissions
    {
        /// <summary>
        /// The guy who can edit the permissions
        /// </summary>
        public string PermissionsControllerRole { get; set; }
        /// <summary>
        /// Does it print the error when a restriction occurs
        /// </summary>
        public bool Verbose { get; set; }
        /// <summary>
        /// The id of the thing (user/server/channel)
        /// </summary>
        public ulong Id { get; set; } //a string because of the role name.
        /// <summary>
        /// Permission object bound to the id of something/role name
        /// </summary>
        public Permissions Permissions { get; set; }
        /// <summary>
        /// Banned words, usually profanities, like word "java"
        /// </summary>
        public HashSet<string> Words { get; set; }

        public Dictionary<ulong, Permissions> UserPermissions { get; set; }
        public Dictionary<ulong, Permissions> ChannelPermissions { get; set; }
        public Dictionary<ulong, Permissions> RolePermissions { get; set; }
        /// <summary>
        /// Dictionary of command names with their respective cooldowns
        /// </summary>
        public ConcurrentDictionary<string, int> CommandCooldowns { get; set; }

        public ServerPermissions(ulong id, string name)
        {
            Id = id;
            PermissionsControllerRole = "Nadeko";
            Verbose = true;

            Permissions = new Permissions(name);
            Permissions.Modules.TryAdd("NSFW", false);
            UserPermissions = new Dictionary<ulong, Permissions>();
            ChannelPermissions = new Dictionary<ulong, Permissions>();
            RolePermissions = new Dictionary<ulong, Permissions>();
            CommandCooldowns = new ConcurrentDictionary<string, int>();
            Words = new HashSet<string>();
        }
    }
}