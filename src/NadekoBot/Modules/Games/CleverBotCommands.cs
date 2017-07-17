using Discord;
using Discord.Commands;
using NadekoBot.Services;
using System;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class CleverBotCommands : NadekoSubmodule<ChatterBotService>
        {
            private readonly DbService _db;

            public CleverBotCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Cleverbot()
            {
                var channel = (ITextChannel)Context.Channel;

                if (_service.ChatterBotGuilds.TryRemove(channel.Guild.Id, out Lazy<ChatterBotSession> throwaway))
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        uow.GuildConfigs.SetCleverbotEnabled(Context.Guild.Id, false);
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await ReplyConfirmLocalized("cleverbot_disabled").ConfigureAwait(false);
                    return;
                }

                _service.ChatterBotGuilds.TryAdd(channel.Guild.Id, new Lazy<ChatterBotSession>(() => new ChatterBotSession(Context.Guild.Id), true));

                using (var uow = _db.UnitOfWork)
                {
                    uow.GuildConfigs.SetCleverbotEnabled(Context.Guild.Id, true);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ReplyConfirmLocalized("cleverbot_enabled").ConfigureAwait(false);
            }
        }

       
    }
}