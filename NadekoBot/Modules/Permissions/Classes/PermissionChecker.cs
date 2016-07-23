using Discord;
using Discord.Commands;
using Discord.Commands.Permissions;
using NadekoBot.Classes.JSONModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions.Classes
{

    internal class PermissionChecker : IPermissionChecker
    {
        public static PermissionChecker Instance { get; } = new PermissionChecker();

        //key - sid:command
        //value - userid
        private ConcurrentDictionary<string, ulong> commandCooldowns = new ConcurrentDictionary<string, ulong>();
        private HashSet<ulong> timeBlackList { get; } = new HashSet<ulong>();

        static PermissionChecker() { }
        private PermissionChecker()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    //blacklist is cleared every 1.00 seconds. That is the most time anyone will be blocked
                    await Task.Delay(1000).ConfigureAwait(false);
                    timeBlackList.Clear();
                }
            });
        }

        public bool CanRun(Command command, User user, Channel channel, out string error)
        {
            error = String.Empty;

            if (!NadekoBot.Ready)
                return false;

            if (channel.IsPrivate || channel.Server == null)
                return command.Category == "Help";

            if (ConfigHandler.IsUserBlacklisted(user.Id) ||
                (!channel.IsPrivate &&
                 (ConfigHandler.IsServerBlacklisted(channel.Server.Id) || ConfigHandler.IsChannelBlacklisted(channel.Id))))
            {
                return false;
            }

            if (timeBlackList.Contains(user.Id))
                return false;

            if (!channel.IsPrivate && !channel.Server.CurrentUser.GetPermissions(channel).SendMessages)
            {
                return false;
            }

            timeBlackList.Add(user.Id);

            ServerPermissions perms;
            PermissionsHandler.PermissionsDict.TryGetValue(user.Server.Id, out perms);

            AddUserCooldown(user.Server.Id, user.Id, command.Text.ToLower());
            if (commandCooldowns.Keys.Contains(user.Server.Id+":"+command.Text.ToLower()))
            {
                if(perms?.Verbose == true)
                    error = $"{user.Mention} You have a cooldown on that command.";
                return false;
            }

            try
            {
                //is it a permission command?
                // if it is, check if the user has the correct role
                // if yes return true, if no return false
                if (command.Category == "Permissions")
                {
                    Discord.Role role = null;
                    try
                    {
                        role = PermissionHelper.ValidateRole(user.Server,
                            PermissionsHandler.GetServerPermissionsRoleName(user.Server));
                    }
                    catch { }
                    if (user.Server.Owner.Id == user.Id || (role != null && user.HasRole(role)))
                        return true;
                    throw new Exception($"You don't have the necessary role (**{(perms?.PermissionsControllerRole ?? "Nadeko")}**) to change permissions.");
                }

                var permissionType = PermissionsHandler.GetPermissionBanType(command, user, channel);

                string msg;

                if (permissionType == PermissionsHandler.PermissionBanType.ServerBanModule &&
                    command.Category.ToLower() == "nsfw")
                    msg = $"**{command.Category}** module has been banned from use on this **server**.\nNSFW module is disabled by default. Server owner can type `;sm nsfw enable` to enable it.";
                else
                    switch (permissionType)
                    {
                        case PermissionsHandler.PermissionBanType.None:
                            return true;
                        case PermissionsHandler.PermissionBanType.ServerBanCommand:
                            msg = $"**{command.Text}** command has been banned from use on this **server**.";
                            break;
                        case PermissionsHandler.PermissionBanType.ServerBanModule:
                            msg = $"**{command.Category}** module has been banned from use on this **server**.";
                            break;
                        case PermissionsHandler.PermissionBanType.ChannelBanCommand:
                            msg = $"**{command.Text}** command has been banned from use on this **channel**.";
                            break;
                        case PermissionsHandler.PermissionBanType.ChannelBanModule:
                            msg = $"**{command.Category}** module has been banned from use on this **channel**.";
                            break;
                        case PermissionsHandler.PermissionBanType.RoleBanCommand:
                            msg = $"You do not have a **role** which permits you the usage of **{command.Text}** command.";
                            break;
                        case PermissionsHandler.PermissionBanType.RoleBanModule:
                            msg = $"You do not have a **role** which permits you the usage of **{command.Category}** module.";
                            break;
                        case PermissionsHandler.PermissionBanType.UserBanCommand:
                            msg = $"{user.Mention}, You have been banned from using **{command.Text}** command.";
                            break;
                        case PermissionsHandler.PermissionBanType.UserBanModule:
                            msg = $"{user.Mention}, You have been banned from using **{command.Category}** module.";
                            break;
                        default:
                            return true;
                    }
                if (PermissionsHandler.PermissionsDict[user.Server.Id].Verbose) //if verbose - print errors
                    error = msg;
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in canrun: {ex}");
                try
                {
                    if (perms != null && perms.Verbose)
                        //if verbose - print errors
                        error = ex.Message;
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"SERIOUS PERMISSION ERROR {ex2}\n\nUser:{user} Server: {user?.Server?.Name}/{user?.Server?.Id}");
                }
                return false;
            }
        }

        public void AddUserCooldown(ulong serverId, ulong userId, string commandName) {
            commandCooldowns.TryAdd(commandName, userId);
            var tosave = serverId + ":" + commandName;
            Task.Run(async () =>
            {
                ServerPermissions perms;
                PermissionsHandler.PermissionsDict.TryGetValue(serverId, out perms);
                int cd;
                if (!perms.CommandCooldowns.TryGetValue(commandName,out cd)) {
                    return;
                }
                if (commandCooldowns.TryAdd(tosave, userId))
                {
                    await Task.Delay(cd * 1000);
                    ulong throwaway;
                    commandCooldowns.TryRemove(tosave, out throwaway);
                }

            });
        }
    }
}
