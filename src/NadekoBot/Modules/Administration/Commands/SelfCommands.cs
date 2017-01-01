using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        class SelfCommands : ModuleBase
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Leave([Remainder] string guildStr)
            {
                guildStr = guildStr.Trim().ToUpperInvariant();
                var server = NadekoBot.Client.GetGuilds().FirstOrDefault(g => g.Id.ToString().Trim().ToUpperInvariant() == guildStr) ??
                    NadekoBot.Client.GetGuilds().FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == guildStr);

                if (server == null)
                {
                    await Context.Channel.SendErrorAsync("⚠️ Cannot find that server").ConfigureAwait(false);
                    return;
                }
                if (server.OwnerId != NadekoBot.Client.CurrentUser().Id)
                {
                    await server.LeaveAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync("✅ Left server " + server.Name).ConfigureAwait(false);
                }
                else
                {
                    await server.DeleteAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync("Deleted server " + server.Name).ConfigureAwait(false);
                }
            }
        }
    }
}
