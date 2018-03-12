using Discord;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Modules.Xp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Core.Modules.Administration.Services
{
    public class SelfAssignedRolesService : INService
    {
        private readonly DbService _db;

        public enum RemoveResult
        {
            Removed, // successfully removed
            Err_Not_Assignable, // not assignable (error)
            Err_Not_Have, // you don't have a role you want to remove (error)
            Err_Not_Perms, // bot doesn't have perms (error)
        }

        public enum AssignResult
        {
            Assigned, // successfully removed
            Err_Not_Assignable, // not assignable (error)
            Err_Already_Have, // you already have that role (error)
            Err_Not_Perms, // bot doesn't have perms (error)
            Err_Lvl_Req, // you are not required level (error)
        }

        public SelfAssignedRolesService(DbService db)
        {
            _db = db;
        }

        public bool AddNew(ulong guildId, IRole role, int group)
        {
            using (var uow = _db.UnitOfWork)
            {
                var roles = uow.SelfAssignedRoles.GetFromGuild(guildId);
                if (roles.Any(s => s.RoleId == role.Id && s.GuildId == role.Guild.Id))
                {
                    return false;
                }

                uow.SelfAssignedRoles.Add(new SelfAssignedRole
                {
                    Group = group,
                    RoleId = role.Id,
                    GuildId = role.Guild.Id
                });
                uow.Complete();
            }
            return true;
        }

        public bool ToggleAdSarm(ulong guildId)
        {
            bool newval;
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.For(guildId, set => set);
                newval = config.AutoDeleteSelfAssignedRoleMessages = !config.AutoDeleteSelfAssignedRoleMessages;
                uow.Complete();
            }
            return newval;
        }

        public async Task<(AssignResult Result, bool AutoDelete, object extra)> Assign(IGuildUser guildUser, IRole role)
        {
            LevelStats userLevelData;
            using (var uow = _db.UnitOfWork)
            {
                var stats = uow.Xp.GetOrCreateUser(guildUser.Guild.Id, guildUser.Id);
                userLevelData = new LevelStats(stats.Xp + stats.AwardedXp);
            }

            var (autoDelete, exclusive, roles) = GetAdAndRoles(guildUser.Guild.Id);

            var theRoleYouWant = roles.FirstOrDefault(r => r.RoleId == role.Id);
            if (theRoleYouWant == null)
            {
                return (AssignResult.Err_Not_Assignable, autoDelete, null);
            }
            else if (theRoleYouWant.LevelRequirement > userLevelData.Level)
            {
                return (AssignResult.Err_Lvl_Req, autoDelete, theRoleYouWant.LevelRequirement);
            }
            else if (guildUser.RoleIds.Contains(role.Id))
            {
                return (AssignResult.Err_Already_Have, autoDelete, null);
            }

            var roleIds = roles
                .Where(x => x.Group == theRoleYouWant.Group)
                .Select(x => x.RoleId).ToArray();
            if (exclusive)
            {
                var sameRoles = guildUser.RoleIds
                    .Where(r => roleIds.Contains(r));

                foreach (var roleId in sameRoles)
                {
                    var sameRole = guildUser.Guild.GetRole(roleId);
                    if (sameRole != null)
                    {
                        try
                        {
                            await guildUser.RemoveRoleAsync(sameRole).ConfigureAwait(false);
                            await Task.Delay(300).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
            try
            {
                await guildUser.AddRoleAsync(role).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return (AssignResult.Err_Not_Perms, autoDelete, ex);
            }

            return (AssignResult.Assigned, autoDelete, null);
        }

        public async Task<(RemoveResult Result, bool AutoDelete)> Remove(IGuildUser guildUser, IRole role)
        {
            var (autoDelete, _, roles) = GetAdAndRoles(guildUser.Guild.Id);

            if (roles.FirstOrDefault(r => r.RoleId == role.Id) == null)
            {
                return (RemoveResult.Err_Not_Assignable, autoDelete);
            }
            if (!guildUser.RoleIds.Contains(role.Id))
            {
                return (RemoveResult.Err_Not_Have, autoDelete);
            }
            try
            {
                await guildUser.RemoveRoleAsync(role).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return (RemoveResult.Err_Not_Perms, autoDelete);
            }

            return (RemoveResult.Removed, autoDelete);
        }

        public bool RemoveSar(ulong guildId, ulong roleId)
        {
            bool success;
            using (var uow = _db.UnitOfWork)
            {
                success = uow.SelfAssignedRoles.DeleteByGuildAndRoleId(guildId, roleId);
                uow.Complete();
            }
            return success;
        }

        public (bool AutoDelete, bool Exclusive, IEnumerable<SelfAssignedRole>) GetAdAndRoles(ulong guildId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                var autoDelete = gc.AutoDeleteSelfAssignedRoleMessages;
                var exclusive = gc.ExclusiveSelfAssignedRoles;
                var roles = uow.SelfAssignedRoles.GetFromGuild(guildId);

                return (autoDelete, exclusive, roles);
            }
        }

        public bool SetLevelReq(ulong guildId, IRole role, int level)
        {
            using (var uow = _db.UnitOfWork)
            {
                var roles = uow.SelfAssignedRoles.GetFromGuild(guildId);
                var sar = roles.FirstOrDefault(x => x.RoleId == role.Id);
                if (sar != null)
                {
                    sar.LevelRequirement = level;
                    uow.Complete();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public bool ToggleEsar(ulong guildId)
        {
            bool areExclusive;
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.For(guildId, set => set);

                areExclusive = config.ExclusiveSelfAssignedRoles = !config.ExclusiveSelfAssignedRoles;
                uow.Complete();
            }
            return areExclusive;
        }

        public (bool Exclusive, List<(SelfAssignedRole Model, IRole Role)> roles) GetRoles(IGuild guild, int page)
        {
            var exclusive = false;

            List<(SelfAssignedRole, IRole)> roles = new List<(SelfAssignedRole, IRole)>();
            IEnumerable<SelfAssignedRole> roleModels;
            using (var uow = _db.UnitOfWork)
            {
                exclusive = uow.GuildConfigs.For(guild.Id, set => set)
                    .ExclusiveSelfAssignedRoles;
                roleModels = uow.SelfAssignedRoles.GetFromGuild(guild.Id);

                foreach (var rm in roleModels)
                {
                    var role = guild.Roles.FirstOrDefault(r => r.Id == rm.RoleId);
                    if (role == null)
                    {
                        uow.SelfAssignedRoles.Remove(rm);
                    }
                }
                uow.Complete();
            }

            var skip = page * 20;
            foreach (var kvp in roleModels.GroupBy(x => x.Group))
            {
                var cnt = kvp.Count();
                if (skip >= cnt)
                {
                    skip -= cnt;
                    continue;
                }
                if (skip < -20)
                    break;
                foreach (var roleModel in kvp.AsEnumerable())
                {
                    if (skip-- > 0)
                    {
                        continue;
                    }
                    if (skip < -20)
                    {
                        break;
                    }

                    var role = guild.Roles.FirstOrDefault(r => r.Id == roleModel.RoleId);
                    if (role == null)
                    {
                        continue;
                    }
                    else
                    {
                        roles.Add((roleModel, role));
                    }
                }
            }
            return (exclusive, roles);
        }
    }
}
