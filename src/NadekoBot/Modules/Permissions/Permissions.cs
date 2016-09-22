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

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
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

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
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

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
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
    }
}
