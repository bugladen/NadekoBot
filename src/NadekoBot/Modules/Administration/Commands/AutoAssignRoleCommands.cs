using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class AutoAssignRoleCommands
        {
            public AutoAssignRoleCommands()
            {
                var _client = NadekoBot.Client;
                _client.UserJoined += async (user) =>
                {
                    GuildConfig conf;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        conf = uow.GuildConfigs.For(user.Guild.Id);
                    }
                    var aarType = conf.AutoAssignRoleId.GetType();

                    if (conf.AutoAssignRoleId == 0)
                        return;

                    var role = user.Guild.Roles.Where(r => r.Id == conf.AutoAssignRoleId).FirstOrDefault();

                    if (role != null)
                        await user.AddRolesAsync(role);
                };
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            public async Task AutoAssignRole(IMessage imsg, [Remainder] IRole role = null)
            {
                var channel = (ITextChannel)imsg.Channel;

                GuildConfig conf;
                using (var uow = DbHandler.UnitOfWork())
                {
                    conf = uow.GuildConfigs.For(channel.Guild.Id);
                    if (role == null)
                        conf.AutoAssignRoleId = 0;
                    else
                        conf.AutoAssignRoleId = role.Id;

                    uow.GuildConfigs.Update(conf);
                    await uow.CompleteAsync();
                }

                if (role == null)
                {
                    await channel.SendMessageAsync("`Auto assign role on user join is now disabled.`").ConfigureAwait(false);
                    return;
                }

                await channel.SendMessageAsync("`Auto assigned role is set.`").ConfigureAwait(false);
            }
        }
    }
}
