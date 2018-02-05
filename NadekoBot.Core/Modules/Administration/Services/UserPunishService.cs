using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Administration.Services
{
    public class UserPunishService : INService
    {
        private readonly MuteService _mute;
        private readonly DbService _db;

        public UserPunishService(MuteService mute, DbService db)
        {
            _mute = mute;
            _db = db;
        }

        public async Task<PunishmentAction?> Warn(IGuild guild, ulong userId, string modName, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                reason = "-";

            var guildId = guild.Id;

            var warn = new Warning()
            {
                UserId = userId,
                GuildId = guildId,
                Forgiven = false,
                Reason = reason,
                Moderator = modName,
            };

            int warnings = 1;
            List<WarningPunishment> ps;
            using (var uow = _db.UnitOfWork)
            {
                ps = uow.GuildConfigs.For(guildId, set => set.Include(x => x.WarnPunishments))
                    .WarnPunishments;

                warnings += uow.Warnings
                    .For(guildId, userId)
                    .Where(w => !w.Forgiven && w.UserId == userId)
                    .Count();

                uow.Warnings.Add(warn);

                uow.Complete();
            }

            var p = ps.FirstOrDefault(x => x.Count == warnings);

            if (p != null)
            {
                var user = await guild.GetUserAsync(userId);
                if (user == null)
                    return null;
                switch (p.Punishment)
                {
                    case PunishmentAction.Mute:
                        if (p.Time == 0)
                            await _mute.MuteUser(user).ConfigureAwait(false);
                        else
                            await _mute.TimedMute(user, TimeSpan.FromMinutes(p.Time)).ConfigureAwait(false);
                        break;
                    case PunishmentAction.Kick:
                        await user.KickAsync("Warned too many times.").ConfigureAwait(false);
                        break;
                    case PunishmentAction.Ban:
                        await guild.AddBanAsync(user, reason: "Warned too many times.").ConfigureAwait(false);
                        break;
                    case PunishmentAction.Softban:
                        await guild.AddBanAsync(user, 7, reason: "Warned too many times").ConfigureAwait(false);
                        try
                        {
                            await guild.RemoveBanAsync(user).ConfigureAwait(false);
                        }
                        catch
                        {
                            await guild.RemoveBanAsync(user).ConfigureAwait(false);
                        }
                        break;
                    case PunishmentAction.RemoveRoles:
                        await user.RemoveRolesAsync(user.GetRoles().Where(x => x.Id != guild.EveryoneRole.Id));
                        break;
                    default:
                        break;
                }
                return p.Punishment;
            }

            return null;
        }
    }
}
