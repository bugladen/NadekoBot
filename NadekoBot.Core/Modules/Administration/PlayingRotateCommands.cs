using Discord.Commands;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Common;
using Discord;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class PlayingRotateCommands : NadekoSubmodule<PlayingRotateService>
        {
            private static readonly object _locker = new object();
            private readonly DbService _db;

            public PlayingRotateCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RotatePlaying()
            {
                bool enabled;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.BotConfig.GetOrCreate(set => set);

                    enabled = config.RotatingStatuses = !config.RotatingStatuses;
                    uow.Complete();
                }
                if (enabled)
                    await ReplyConfirmLocalized("ropl_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("ropl_disabled").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task AddPlaying(ActivityType t,[Remainder] string status)
            {
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.BotConfig.GetOrCreate(set => set.Include(x => x.RotatingStatusMessages));
                    var toAdd = new PlayingStatus { Status = status, Type = t };
                    config.RotatingStatusMessages.Add(toAdd);
                    await uow.CompleteAsync();
                }

                _bc.Reload();

                await ReplyConfirmLocalized("ropl_added").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ListPlaying()
            {
                if (!_service.BotConfig.RotatingStatusMessages.Any())
                    await ReplyErrorLocalized("ropl_not_set").ConfigureAwait(false);
                else
                {
                    var i = 1;
                    await ReplyConfirmLocalized("ropl_list",
                            string.Join("\n\t", _service.BotConfig.RotatingStatusMessages.Select(rs => $"`{i++}.` *{rs.Type}* {rs.Status}")))
                        .ConfigureAwait(false);
                }

            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemovePlaying(int index)
            {
                index -= 1;

                string msg;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.BotConfig.GetOrCreate(set => set.Include(x => x.RotatingStatusMessages));

                    if (index >= config.RotatingStatusMessages.Count)
                        return;
                    msg = config.RotatingStatusMessages[index].Status;
                    config.RotatingStatusMessages.RemoveAt(index);
                    await uow.CompleteAsync();
                }

                _bc.Reload();
                await ReplyConfirmLocalized("reprm", msg).ConfigureAwait(false);
            }
        }
    }
}