using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class AutoAssignRoleCommands : ModuleBase
        {
            private static Logger _log { get; }
            //guildid/roleid
            private static ConcurrentDictionary<ulong, ulong> AutoAssignedRoles { get; }

            static AutoAssignRoleCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();

                AutoAssignedRoles = new ConcurrentDictionary<ulong, ulong>(NadekoBot.AllGuildConfigs.Where(x => x.AutoAssignRoleId != 0)
                    .ToDictionary(k => k.GuildId, v => v.AutoAssignRoleId));
                NadekoBot.Client.UserJoined += async (user) =>
                {
                    try
                    {
                        ulong roleId = 0;
                        AutoAssignedRoles.TryGetValue(user.Guild.Id, out roleId);

                        if (roleId == 0)
                            return;

                        var role = user.Guild.Roles.FirstOrDefault(r => r.Id == roleId);

                        if (role != null)
                            await user.AddRolesAsync(role).ConfigureAwait(false);
                    }
                    catch (Exception ex) { _log.Warn(ex); }
                };

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task AutoAssignRole([Remainder] IRole role = null)
            {
                GuildConfig conf;
                using (var uow = DbHandler.UnitOfWork())
                {
                    conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    if (role == null)
                    {
                        conf.AutoAssignRoleId = 0;
                        ulong throwaway;
                        AutoAssignedRoles.TryRemove(Context.Guild.Id, out throwaway);
                    }
                    else
                    {
                        conf.AutoAssignRoleId = role.Id;
                        AutoAssignedRoles.AddOrUpdate(Context.Guild.Id, role.Id, (key, val) => role.Id);
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (role == null)
                {
                    await Context.Channel.SendConfirmAsync("🆗 **Auto assign role** on user join is now **disabled**.").ConfigureAwait(false);
                    return;
                }

                await Context.Channel.SendConfirmAsync("✅ **Auto assign role** on user join is now **enabled**.").ConfigureAwait(false);
            }
        }
    }
}
