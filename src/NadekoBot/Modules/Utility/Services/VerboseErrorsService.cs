using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Collections;
using NadekoBot.Extensions;
using NadekoBot.Modules.Help.Services;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Utility.Services
{
    public class VerboseErrorsService : INService
    {
        private readonly ConcurrentHashSet<ulong> guildsEnabled;
        private readonly DbService _db;
        private readonly CommandHandler _ch;
        private readonly HelpService _hs;

        public VerboseErrorsService(IEnumerable<GuildConfig> gcs, DbService db, CommandHandler ch, HelpService hs)
        {
            _db = db;
            _ch = ch;
            _hs = hs;

            ch.CommandErrored += LogVerboseError;

            guildsEnabled = new ConcurrentHashSet<ulong>(gcs.Where(x => x.VerboseErrors).Select(x => x.GuildId));
        }

        private async Task LogVerboseError(CommandInfo cmd, ITextChannel channel, string reason)
        {
            if (channel == null || !guildsEnabled.Contains(channel.GuildId))
                return;

            try
            {
                var embed = _hs.GetCommandHelp(cmd, channel.Guild)
                    .WithTitle("Command Error")
                    .WithDescription(reason)
                    .WithErrorColor();

                await channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch
            {
                //ignore
            }
        }

        public bool ToggleVerboseErrors(ulong guildId)
        {
            bool enabled;
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);

                enabled = gc.VerboseErrors = !gc.VerboseErrors;

                uow.Complete();

                if (gc.VerboseErrors)
                    guildsEnabled.Add(guildId);
                else
                    guildsEnabled.TryRemove(guildId);
            }

            if (enabled)
                guildsEnabled.Add(guildId);
            else
                guildsEnabled.TryRemove(guildId);

            return enabled;            
        }

    }
}
