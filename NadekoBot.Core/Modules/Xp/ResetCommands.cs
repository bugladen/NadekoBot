using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Xp
{
    public partial class Xp
    {
        public class ResetCommands : NadekoSubmodule
        {
            private readonly DbService _db;

            public ResetCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public Task XpReset(IGuildUser user)
                => XpReset(user.Id);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task XpReset(ulong id)
            {
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("reset"))
                    .WithDescription(GetText("reset_user_confirm"));

                if (!await PromptUserConfirmAsync(embed))
                    return;
                using (var uow = _db.UnitOfWork)
                {
                    uow.Xp.ResetGuildUserXp(id, Context.Guild.Id);
                    uow.Complete();
                }

                await ReplyConfirmLocalized("reset_user", id).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task XpReset()
            {
                var embed = new EmbedBuilder()
                       .WithTitle(GetText("reset"))
                       .WithDescription(GetText("reset_server_confirm"));

                if (!await PromptUserConfirmAsync(embed))
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    uow.Xp.ResetGuildXp(Context.Guild.Id);
                    uow.Complete();
                }

                await ReplyConfirmLocalized("reset_server").ConfigureAwait(false);
            }
        }
    }
}
