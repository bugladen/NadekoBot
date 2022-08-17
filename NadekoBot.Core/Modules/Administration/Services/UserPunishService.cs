using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Common.TypeReaders.Models;
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

        public async Task<PunishmentAction?> Warn(IGuild guild, ulong userId, IUser mod, string reason)
        {
            var modName = mod.ToString();

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
            using (var uow = _db.GetDbContext())
            {
                ps = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.WarnPunishments))
                    .WarnPunishments;

                warnings += uow.Warnings
                    .ForId(guildId, userId)
                    .Where(w => !w.Forgiven && w.UserId == userId)
                    .Count();

                uow.Warnings.Add(warn);

                uow.SaveChanges();
            }

            var p = ps.FirstOrDefault(x => x.Count == warnings);

            if (p != null)
            {
                var user = await guild.GetUserAsync(userId).ConfigureAwait(false);
                if (user == null)
                    return null;
                switch (p.Punishment)
                {
                    case PunishmentAction.Mute:
                        if (p.Time == 0)
                            await _mute.MuteUser(user, mod).ConfigureAwait(false);
                        else
                            await _mute.TimedMute(user, mod, TimeSpan.FromMinutes(p.Time)).ConfigureAwait(false);
                        break;
                    case PunishmentAction.Kick:
                        await user.KickAsync("Warned too many times.").ConfigureAwait(false);
                        break;
                    case PunishmentAction.Ban:
                        if (p.Time == 0)
                            await guild.AddBanAsync(user, reason: "Warned too many times.").ConfigureAwait(false);
                        else
                            await _mute.TimedBan(user, TimeSpan.FromMinutes(p.Time), "Warned too many times.").ConfigureAwait(false);
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
                        await user.RemoveRolesAsync(user.GetRoles().Where(x => x.Id != guild.EveryoneRole.Id)).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }
                return p.Punishment;
            }

            return null;
        }

        public IGrouping<ulong, Warning>[] WarnlogAll(ulong gid)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.Warnings.GetForGuild(gid).GroupBy(x => x.UserId).ToArray();
            }
        }

        public Warning[] UserWarnings(ulong gid, ulong userId)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.Warnings.ForId(gid, userId);
            }
        }

        public async Task<bool> WarnClearAsync(ulong guildId, ulong userId, int index, string moderator)
        {
            bool toReturn = true;
            using (var uow = _db.GetDbContext())
            {
                if (index == 0)
                {
                    await uow.Warnings.ForgiveAll(guildId, userId, moderator);
                }
                else
                {
                    toReturn = uow.Warnings.Forgive(guildId, userId, moderator, index - 1);
                }
                uow.SaveChanges();
            }
            return toReturn;
        }

        public bool WarnPunish(ulong guildId, int number, PunishmentAction punish, StoopidTime time)
        {
            if ((punish != PunishmentAction.Ban && punish != PunishmentAction.Mute) && time != null)
                return false;
            if (number <= 0 || (time != null && time.Time > TimeSpan.FromDays(49)))
                return false;

            using (var uow = _db.GetDbContext())
            {
                var ps = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
                var toDelete = ps.Where(x => x.Count == number);

                uow._context.RemoveRange(toDelete);

                ps.Add(new WarningPunishment()
                {
                    Count = number,
                    Punishment = punish,
                    Time = (int?)(time?.Time.TotalMinutes) ?? 0,
                });
                uow.SaveChanges();
            }
            return true;
        }

        public bool WarnPunish(ulong guildId, int number)
        {
            if (number <= 0)
                return false;

            using (var uow = _db.GetDbContext())
            {
                var ps = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
                var p = ps.FirstOrDefault(x => x.Count == number);

                if (p != null)
                {
                    uow._context.Remove(p);
                    uow.SaveChanges();
                }
            }
            return true;
        }

        public WarningPunishment[] WarnPunishList(ulong guildId)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.GuildConfigs.ForId(guildId, gc => gc.Include(x => x.WarnPunishments))
                    .WarnPunishments
                    .OrderBy(x => x.Count)
                    .ToArray();
            }
        }

        public (IEnumerable<(string Original, ulong? Id, string Reason)> Bans, int Missing) MassKill(SocketGuild guild, string people)
        {
            var gusers = guild.Users;
            //get user objects and reasons
            var bans = people.Split("\n")
                .Select(x =>
                {
                    var split = x.Trim().Split(" ");

                    var reason = string.Join(" ", split.Skip(1));

                    if (ulong.TryParse(split[0], out var id))
                        return (Original: split[0], Id: id, Reason: reason);

                    return (Original: split[0],
                        Id: gusers
                            .FirstOrDefault(u => u.ToString().ToLowerInvariant() == x)
                            ?.Id,
                        Reason: reason);
                })
                .ToArray();

            //if user is null, means that person couldn't be found
            var missing = bans
                .Where(x => !x.Id.HasValue)
                .Count();

            //get only data for found users
            var found = bans
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id.Value)
                .ToArray();

            using (var uow = _db.GetDbContext())
            {
                var bc = uow.BotConfig.GetOrCreate(set => set.Include(x => x.Blacklist));
                //blacklist the users
                bc.Blacklist.AddRange(found.Select(x =>
                    new BlacklistItem
                    {
                        ItemId = x,
                        Type = BlacklistType.User,
                    }));
                //clear their currencies
                uow.DiscordUsers.RemoveFromMany(found.Select(x => x).ToList());
                uow.SaveChanges();
            }

            return (bans, missing);
        }
    }
}
