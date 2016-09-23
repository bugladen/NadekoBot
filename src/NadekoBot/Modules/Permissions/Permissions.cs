using NadekoBot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using Discord;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Permissions
{
    [NadekoModule("Permissions", ";")]
    public class Permissions : DiscordModule
    {
        public Permissions(ILocalization loc, CommandService cmds, DiscordSocketClient client) : base(loc, cmds, client)
        {
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task ListPerms(IUserMessage msg)
        {
            var channel = (ITextChannel)msg.Channel;

            string toSend = "";
            using (var uow = DbHandler.UnitOfWork())
            {
                var perms = uow.GuildConfigs.For(channel.Guild.Id).Permissions;

                var i = 1;
                toSend = String.Join("\n", perms.Select(p => $"`{(i++)}.` {p.GetCommand()}"));
            }

            if (string.IsNullOrWhiteSpace(toSend))
                await channel.SendMessageAsync("`No permissions set.`").ConfigureAwait(false);
            else
                await channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task UsrCmd(IUserMessage imsg, Command command, PermissionAction action, IGuildUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = user.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Text.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `{command.Text}` command for `{user}` user.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task UsrMdl(IUserMessage imsg, Module module, PermissionAction action, IGuildUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = user.Id,
                    SecondaryTarget = SecondaryPermissionType.Module,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `{module.Name}` module for `{user}` user.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task RoleCmd(IUserMessage imsg, Command command, PermissionAction action, IRole role)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Role,
                    PrimaryTargetId = role.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Text.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `{command.Text}` command for `{role}` role.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task RoleMdl(IUserMessage imsg, Module module, PermissionAction action, IRole role)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Role,
                    PrimaryTargetId = role.Id,
                    SecondaryTarget = SecondaryPermissionType.Module,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `{module.Name}` module for `{role}` role.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlCmd(IUserMessage imsg, Command command, PermissionAction action, ITextChannel chnl)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Channel,
                    PrimaryTargetId = chnl.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = command.Text.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `{command.Text}` command for `{chnl}` channel.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task ChnlMdl(IUserMessage imsg, Module module, PermissionAction action, ITextChannel chnl)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Channel,
                    PrimaryTargetId = chnl.Id,
                    SecondaryTarget = SecondaryPermissionType.Module,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `{module.Name}` module for `{chnl}` channel.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task AllChnlMdls(IUserMessage imsg, PermissionAction action, ITextChannel chnl)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Channel,
                    PrimaryTargetId = chnl.Id,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `ALL MODULES` for `{chnl}` channel.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task AllRoleMdls(IUserMessage imsg, PermissionAction action, IRole role)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Role,
                    PrimaryTargetId = role.Id,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `ALL MODULES` for `{role}` role.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task AllUserMdls(IUserMessage imsg, PermissionAction action, IUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = user.Id,
                    SecondaryTarget = SecondaryPermissionType.AllModules,
                    SecondaryTargetName = "*",
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `ALL MODULES` for `{user}` user.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task AllChnlCmds(IUserMessage imsg, Module module, PermissionAction action, ITextChannel chnl)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Channel,
                    PrimaryTargetId = chnl.Id,
                    SecondaryTarget = SecondaryPermissionType.Command,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `ALL COMMANDS` from `{module.Name}` module for `{chnl}` channel.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task AllRoleCmds(IUserMessage imsg, Module module, PermissionAction action, IRole role)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.Role,
                    PrimaryTargetId = role.Id,
                    SecondaryTarget = SecondaryPermissionType.AllCommands,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `ALL COMMANDS` from `{module.Name}` module for `{role}` role.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task AllUserCmds(IUserMessage imsg, Module module, PermissionAction action, IUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    PrimaryTarget = PrimaryPermissionType.User,
                    PrimaryTargetId = user.Id,
                    SecondaryTarget = SecondaryPermissionType.AllCommands,
                    SecondaryTargetName = module.Name.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `ALL COMMANDS` from `{module.Name}` module for `{user}` user.").ConfigureAwait(false);
        }

    }
}
