using Discord;
using Discord.Commands.Permissions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot.Classes.Permissions {
    public static class PermissionsHandler {
        public static ConcurrentDictionary<Server, ServerPermissions> _permissionsDict =
            new ConcurrentDictionary<Server, ServerPermissions>();


        public enum PermissionBanType {
            None, ServerBanCommand, ServerBanModule,
            ChannelBanCommand, ChannelBanModule, RoleBanCommand,
            RoleBanModule, UserBanCommand, UserBanModule
        }


        public static void Initialize() {
            Console.WriteLine("Reading from the permission files.");
            Directory.CreateDirectory("data/permissions");
            foreach (var file in Directory.EnumerateFiles("data/permissions/")) {
                try {
                    var strippedFileName = Path.GetFileNameWithoutExtension(file);
                    var id = ulong.Parse(strippedFileName);
                    var server = NadekoBot.client.GetServer(id);
                    if (server == null)
                        throw new ArgumentException("Server does not exist");

                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerPermissions>(File.ReadAllText(file));
                    _permissionsDict.TryAdd(server, data);
                } catch (Exception ex) {
                    Console.WriteLine($"Failed getting server with id: {file}\nReason: {ex.Message}");
                }
            }
            Console.WriteLine("Permission initialization complete.");
        }

        internal static Permissions GetRolePermissionsById(Server server, ulong id) {
            if (!_permissionsDict.ContainsKey(server))
                return null;

            Permissions toReturn;
            _permissionsDict[server].RolePermissions.TryGetValue(id, out toReturn);
            return toReturn;
        }

        internal static Permissions GetUserPermissionsById(Server server, ulong id) {
            if (!_permissionsDict.ContainsKey(server))
                return null;

            Permissions toReturn;
            _permissionsDict[server].UserPermissions.TryGetValue(id, out toReturn);
            return toReturn;
        }

        internal static Permissions GetChannelPermissionsById(Server server, ulong id) {
            if (!_permissionsDict.ContainsKey(server))
                return null;

            Permissions toReturn;
            _permissionsDict[server].ChannelPermissions.TryGetValue(id, out toReturn);
            return toReturn;
        }

        internal static Permissions GetServerPermissions(Server server) {
            if (!_permissionsDict.ContainsKey(server))
                return null;

            return _permissionsDict[server].Permissions;
        }

        internal static PermissionBanType GetPermissionBanType(Command command, User user, Channel channel) {
            var server = user.Server;
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            bool val;
            Permissions perm;
            //server
            if (_permissionsDict[server].Permissions.modules.TryGetValue(command.Category, out val) && val == false)
                return PermissionBanType.ServerBanModule;
            if (_permissionsDict[server].Permissions.commands.TryGetValue(command.Text, out val) && val == false)
                return PermissionBanType.ServerBanCommand;
            //channel
            if (_permissionsDict[server].ChannelPermissions.TryGetValue(channel.Id, out perm) &&
                perm.modules.TryGetValue(command.Category, out val) && val == false)
                return PermissionBanType.ChannelBanModule;
            if (_permissionsDict[server].ChannelPermissions.TryGetValue(channel.Id, out perm) &&
                perm.commands.TryGetValue(command.Text, out val) && val == false)
                return PermissionBanType.ChannelBanCommand;

            //ROLE PART - TWO CASES
            // FIRST CASE:
            // IF EVERY ROLE USER HAS IS BANNED FROM THE MODULE,
            // THAT MEANS USER CANNOT RUN THIS COMMAND
            // IF AT LEAST ONE ROLE EXIST THAT IS NOT BANNED,
            // USER CAN RUN THE COMMAND
            bool foundNotBannedRole = false;
            foreach (var role in user.Roles) {
                //if every role is banned from using the module -> rolebanmodule
                if (_permissionsDict[server].RolePermissions.TryGetValue(role.Id, out perm) &&
                perm.modules.TryGetValue(command.Category, out val) && val == false)
                    continue;
                else {
                    foundNotBannedRole = true;
                    break;
                }
            }
            if (!foundNotBannedRole)
                return PermissionBanType.RoleBanModule;

            // SECOND CASE:
            // IF EVERY ROLE USER HAS IS BANNED FROM THE COMMAND,
            // THAT MEANS USER CANNOT RUN THAT COMMAND
            // IF AT LEAST ONE ROLE EXISTS THAT IS NOT BANNED,
            // USER CAN RUN THE COMMAND
            foundNotBannedRole = false;
            foreach (var role in user.Roles) {
                //if every role is banned from using the module -> rolebanmodule
                if (_permissionsDict[server].RolePermissions.TryGetValue(role.Id, out perm) &&
                perm.commands.TryGetValue(command.Text, out val) && val == false)
                    continue;
                else {
                    foundNotBannedRole = true;
                    break;
                }
            }
            if (!foundNotBannedRole)
                return PermissionBanType.RoleBanCommand;

            //user
            if (_permissionsDict[server].UserPermissions.TryGetValue(user.Id, out perm) &&
                perm.modules.TryGetValue(command.Category, out val) && val == false)
                return PermissionBanType.UserBanModule;
            if (_permissionsDict[server].UserPermissions.TryGetValue(user.Id, out perm) &&
                perm.commands.TryGetValue(command.Text, out val) && val == false)
                return PermissionBanType.UserBanCommand;

            return PermissionBanType.None;
        }

        private static void WriteServerToJson(Server server) {
            string pathToFile = $"data/permissions/{server.Id}.json";
            File.WriteAllText(pathToFile, Newtonsoft.Json.JsonConvert.SerializeObject(_permissionsDict[server], Newtonsoft.Json.Formatting.Indented));
        }

        public static void WriteToJson() {
            Directory.CreateDirectory("data/permissions/");
            foreach (var kvp in _permissionsDict) {
                WriteServerToJson(kvp.Key);
            }
        }

        public static string GetServerPermissionsRoleName(Server server) {
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            return _permissionsDict[server].PermissionsControllerRole;
        }

        public static void SetServerModulePermission(Server server, string moduleName, bool value) {
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            var modules = _permissionsDict[server].Permissions.modules;
            if (modules.ContainsKey(moduleName))
                modules[moduleName] = value;
            else
                modules.Add(moduleName, value);
            WriteServerToJson(server);
        }

        public static void SetServerCommandPermission(Server server, string commandName, bool value) {
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            var commands = _permissionsDict[server].Permissions.commands;
            if (commands.ContainsKey(commandName))
                commands[commandName] = value;
            else
                commands.Add(commandName, value);
            WriteServerToJson(server);
        }

        public static void SetChannelModulePermission(Channel channel, string moduleName, bool value) {
            var server = channel.Server;
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            if (!_permissionsDict[server].ChannelPermissions.ContainsKey(channel.Id))
                _permissionsDict[server].ChannelPermissions.Add(channel.Id, new Permissions(channel.Name));

            var modules = _permissionsDict[server].ChannelPermissions[channel.Id].modules;

            if (modules.ContainsKey(moduleName))
                modules[moduleName] = value;
            else
                modules.Add(moduleName, value);
            WriteServerToJson(server);
        }

        public static void SetChannelCommandPermission(Channel channel, string commandName, bool value) {
            var server = channel.Server;
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            if (!_permissionsDict[server].ChannelPermissions.ContainsKey(channel.Id))
                _permissionsDict[server].ChannelPermissions.Add(channel.Id, new Permissions(channel.Name));

            var commands = _permissionsDict[server].ChannelPermissions[channel.Id].commands;

            if (commands.ContainsKey(commandName))
                commands[commandName] = value;
            else
                commands.Add(commandName, value);
            WriteServerToJson(server);
        }

        public static void SetRoleModulePermission(Role role, string moduleName, bool value) {
            var server = role.Server;
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            if (!_permissionsDict[server].RolePermissions.ContainsKey(role.Id))
                _permissionsDict[server].RolePermissions.Add(role.Id, new Permissions(role.Name));

            var modules = _permissionsDict[server].RolePermissions[role.Id].modules;

            if (modules.ContainsKey(moduleName))
                modules[moduleName] = value;
            else
                modules.Add(moduleName, value);
            WriteServerToJson(server);
        }

        public static void SetRoleCommandPermission(Role role, string commandName, bool value) {
            var server = role.Server;
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            if (!_permissionsDict[server].RolePermissions.ContainsKey(role.Id))
                _permissionsDict[server].RolePermissions.Add(role.Id, new Permissions(role.Name));

            var commands = _permissionsDict[server].RolePermissions[role.Id].commands;

            if (commands.ContainsKey(commandName))
                commands[commandName] = value;
            else
                commands.Add(commandName, value);
            WriteServerToJson(server);
        }

        public static void SetUserModulePermission(User user, string moduleName, bool value) {
            var server = user.Server;
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            if (!_permissionsDict[server].UserPermissions.ContainsKey(user.Id))
                _permissionsDict[server].UserPermissions.Add(user.Id, new Permissions(user.Name));

            var modules = _permissionsDict[server].UserPermissions[user.Id].modules;

            if (modules.ContainsKey(moduleName))
                modules[moduleName] = value;
            else
                modules.Add(moduleName, value);
            WriteServerToJson(server);
        }

        public static void SetUserCommandPermission(User user, string commandName, bool value) {
            var server = user.Server;
            if (!_permissionsDict.ContainsKey(server)) {
                _permissionsDict.TryAdd(server, new ServerPermissions(server.Id, server.Name));
            }
            if (!_permissionsDict[server].UserPermissions.ContainsKey(user.Id))
                _permissionsDict[server].UserPermissions.Add(user.Id, new Permissions(user.Name));

            var commands = _permissionsDict[server].UserPermissions[user.Id].commands;

            if (commands.ContainsKey(commandName))
                commands[commandName] = value;
            else
                commands.Add(commandName, value);
            WriteServerToJson(server);
        }
    }
    /// <summary>
    /// Holds a permission list
    /// </summary>
    public class Permissions {
        /// <summary>
        /// Name of the parent object whose permissions these are
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Module name with allowed/disallowed
        /// </summary>
        public Dictionary<string, bool> modules { get; set; }
        /// <summary>
        /// Command name with allowed/disallowed
        /// </summary>
        public Dictionary<string, bool> commands { get; set; }

        public Permissions(string name) {
            Name = name;
            modules = new Dictionary<string, bool>();
            commands = new Dictionary<string, bool>();
        }

        public override string ToString() {
            string toReturn = "";
            var bannedModules = modules.Where(kvp => kvp.Value == false);
            if (bannedModules.Count() > 0) {
                toReturn += "`Banned Modules:`\n";
                foreach (var m in bannedModules) {
                    toReturn += $"\t`[x]  {m.Key}`\n";
                }
            }
            var bannedCommands = commands.Where(kvp => kvp.Value == false);
            if (bannedCommands.Count() > 0) {
                toReturn += "`Banned Commands:`\n";
                foreach (var c in bannedCommands) {
                    toReturn += $"\t`[x]  {c.Key}`\n";
                }
            }
            return toReturn;
        }
    }

    public class ServerPermissions {
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

        public Dictionary<ulong, Permissions> UserPermissions { get; set; }
        public Dictionary<ulong, Permissions> ChannelPermissions { get; set; }
        public Dictionary<ulong, Permissions> RolePermissions { get; set; }

        public ServerPermissions(ulong id, string name) {
            Id = id;
            PermissionsControllerRole = "Nadeko";
            Verbose = true;

            Permissions = new Permissions(name);
            UserPermissions = new Dictionary<ulong, Permissions>();
            ChannelPermissions = new Dictionary<ulong, Permissions>();
            RolePermissions = new Dictionary<ulong, Permissions>();
        }
    }
}