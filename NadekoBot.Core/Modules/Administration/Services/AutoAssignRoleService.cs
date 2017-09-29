using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Administration.Services
{
    public class AutoAssignRoleService : INService
    {
        private readonly Logger _log;
        private readonly DiscordSocketClient _client;

        //guildid/roleid
        public ConcurrentDictionary<ulong, ulong> AutoAssignedRoles { get; }

        public AutoAssignRoleService(DiscordSocketClient client, IEnumerable<GuildConfig> gcs)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;

            AutoAssignedRoles = new ConcurrentDictionary<ulong, ulong>(
                gcs.Where(x => x.AutoAssignRoleId != 0)
                    .ToDictionary(k => k.GuildId, v => v.AutoAssignRoleId));

            _client.UserJoined += (user) =>
            {
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        AutoAssignedRoles.TryGetValue(user.Guild.Id, out ulong roleId);

                        if (roleId == 0)
                            return;

                        var role = user.Guild.Roles.FirstOrDefault(r => r.Id == roleId);

                        if (role != null)
                            await user.AddRoleAsync(role).ConfigureAwait(false);
                    }
                    catch (Exception ex) { _log.Warn(ex); }
                });
                return Task.CompletedTask;
            };
        }
    }
}
