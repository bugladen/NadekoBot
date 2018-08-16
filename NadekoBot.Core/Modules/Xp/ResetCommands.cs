using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Modules.Xp.Services;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Xp
{
    public partial class Xp
    {
        public class ResetCommands : NadekoSubmodule<XpService>
        {

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public Task XpReset(IGuildUser user)
                => XpReset(user.Id);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task XpReset(ulong userId)
            {
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("reset"))
                    .WithDescription(GetText("reset_user_confirm"));

                if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    return;

                _service.XpReset(Context.Guild.Id, userId);

                await ReplyConfirmLocalized("reset_user", userId).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task XpReset()
            {
                var embed = new EmbedBuilder()
                       .WithTitle(GetText("reset"))
                       .WithDescription(GetText("reset_server_confirm"));

                if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    return;

                _service.XpReset(Context.Guild.Id);

                await ReplyConfirmLocalized("reset_server").ConfigureAwait(false);
            }
        }
    }
}
