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
        public async Task UsrCmd(IUserMessage imsg, Command command, PermissionAction action, IGuildUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.GuildConfigs.For(channel.Guild.Id).Permissions.Add(new Permission
                {
                    TargetType = PermissionType.User,
                    Target = user.Id.ToString(),
                    Command = command.Text.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync();
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `{command.Text}` command for `{user}` user.");
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
                    TargetType = PermissionType.User,
                    Target = user.Id.ToString(),
                    Module = module.Name.ToLowerInvariant(),
                    State = action.Value,
                });
                await uow.CompleteAsync();
            }
            await channel.SendMessageAsync($"{(action.Value ? "Allowed" : "Denied")} usage of `{module.Name}` module for `{user}` user.");
        }
    }
}
