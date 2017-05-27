using Discord;
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Administration
{
    public class VcRoleService
    {
        private readonly Logger _log;

        public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>> VcRoles { get; }

        public VcRoleService(DiscordShardedClient client, IEnumerable<GuildConfig> gcs)
        {
            _log = LogManager.GetCurrentClassLogger();

            client.UserVoiceStateUpdated += ClientOnUserVoiceStateUpdated;
            VcRoles = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>>();
            foreach (var gconf in gcs)
            {
                var g = client.GetGuild(gconf.GuildId);
                if (g == null)
                    continue; //todo delete everything from db if guild doesn't exist?

                var infos = new ConcurrentDictionary<ulong, IRole>();
                VcRoles.TryAdd(gconf.GuildId, infos);
                foreach (var ri in gconf.VcRoleInfos)
                {
                    var role = g.GetRole(ri.RoleId);
                    if (role == null)
                        continue; //todo remove this entry from db

                    infos.TryAdd(ri.VoiceChannelId, role);
                }
            }
        }

        private Task ClientOnUserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState,
            SocketVoiceState newState)
        {

            var gusr = usr as SocketGuildUser;
            if (gusr == null)
                return Task.CompletedTask;

            var oldVc = oldState.VoiceChannel;
            var newVc = newState.VoiceChannel;
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (oldVc != newVc)
                    {
                        ulong guildId;
                        guildId = newVc?.Guild.Id ?? oldVc.Guild.Id;

                        if (VcRoles.TryGetValue(guildId, out ConcurrentDictionary<ulong, IRole> guildVcRoles))
                        {
                            //remove old
                            if (oldVc != null && guildVcRoles.TryGetValue(oldVc.Id, out IRole role))
                            {
                                if (gusr.Roles.Contains(role))
                                {
                                    try
                                    {
                                        await gusr.RemoveRoleAsync(role).ConfigureAwait(false);
                                        await Task.Delay(500).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        await Task.Delay(200).ConfigureAwait(false);
                                        await gusr.RemoveRoleAsync(role).ConfigureAwait(false);
                                        await Task.Delay(500).ConfigureAwait(false);
                                    }
                                }
                            }
                            //add new
                            if (newVc != null && guildVcRoles.TryGetValue(newVc.Id, out role))
                            {
                                if (!gusr.Roles.Contains(role))
                                    await gusr.AddRoleAsync(role).ConfigureAwait(false);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            });
            return Task.CompletedTask;
        }
    }
}
