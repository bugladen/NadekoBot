using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    //todo DB
    public partial class Administration
    {
        [Group]
        public class AutoAssignRole
        {
            public AutoAssignRole()
            {
                var _client = NadekoBot.Client;
                _client.UserJoined += (user) =>
                {
                    //var config = SpecificConfigurations.Default.Of(e.Server.Id);

                    //var role = e.Server.Roles.Where(r => r.Id == config.AutoAssignedRole).FirstOrDefault();

                    //if (role == null)
                    //    return;

                    //imsg.Author.AddRoles(role);
                    return Task.CompletedTask;
                };
            }

            //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
            //[RequireContext(ContextType.Guild)]
            //[RequirePermission(GuildPermission.ManageRoles)]
            //public async Task AutoAssignRole(IMessage imsg, IRole role)
            //{
            //    var channel = imsg.Channel as ITextChannel;

            //    var config = SpecificConfigurations.Default.Of(e.Server.Id);

            //    if (string.IsNullOrWhiteSpace(r)) //if role is not specified, disable
            //    {
            //        config.AutoAssignedRole = 0;

            //        await channel.SendMessageAsync("`Auto assign role on user join is now disabled.`").ConfigureAwait(false);
            //        return;
            //    }

            //    config.AutoAssignedRole = role.Id;
            //    await channel.SendMessageAsync("`Auto assigned role is set.`").ConfigureAwait(false);
            //}
        }
    }
}
