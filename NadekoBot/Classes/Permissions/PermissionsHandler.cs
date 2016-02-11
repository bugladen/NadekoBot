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

        public static void Initialize() {
            Console.WriteLine("Reading from the permission files.");
            Directory.CreateDirectory("data/permissions");
            foreach (var file in Directory.EnumerateFiles("data/permissions/")) {
                try {
                    var strippedFileName = file.Substring(file.LastIndexOf('/') + 1, file.LastIndexOf(".json") - file.LastIndexOf('/') - 1);
                    var id = ulong.Parse(strippedFileName);
                    var server = NadekoBot.client.GetServer(id);
                    if (server == null)
                        throw new ArgumentException("Server does not exist");

                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerPermissions>(File.ReadAllText(file));
                    _permissionsDict.TryAdd(server, data);
                } catch (Exception ex){
                    Console.WriteLine($"Failed getting server with id: {file}\nReason: {ex.Message}");
                }
            }
            Console.WriteLine("Permission initialization complete.");
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
            if(!_permissionsDict[server].ChannelPermissions.ContainsKey(channel.Id))
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
    }

    public class ServerPermissions {
        /// <summary>
        /// The guy who can edit the permissions
        /// </summary>
        public string PermissionsControllerRoleName { get; set; }
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
            PermissionsControllerRoleName = "PermissionsKing";
            Verbose = true;

            Permissions = new Permissions(name);
            UserPermissions = new Dictionary<ulong, Permissions>();
            ChannelPermissions = new Dictionary<ulong, Permissions>();
            RolePermissions = new Dictionary<ulong, Permissions>();
        }
    }
}