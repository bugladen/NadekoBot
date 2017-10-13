using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Administration.Services
{
    public class VcRoleService : INService
    {
        private readonly Logger _log;
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>> VcRoles { get; }

        public VcRoleService(DiscordSocketClient client, NadekoBot bot, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            _client = client;

            _client.UserVoiceStateUpdated += ClientOnUserVoiceStateUpdated;
            VcRoles = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>>();
            var missingRoles = new List<VcRoleInfo>();
            foreach (var gconf in bot.AllGuildConfigs)
            {
                var g = _client.GetGuild(gconf.GuildId);
                if (g == null)
                    continue;

                var infos = new ConcurrentDictionary<ulong, IRole>();
                VcRoles.TryAdd(gconf.GuildId, infos);
                foreach (var ri in gconf.VcRoleInfos)
                {
                    var role = g.GetRole(ri.RoleId);
                    if (role == null)
                    {
                        missingRoles.Add(ri);
                        continue;
                    }

                    infos.TryAdd(ri.VoiceChannelId, role);
                }
            }
            if(missingRoles.Any())
                using (var uow = _db.UnitOfWork)
                {
                    _log.Warn($"Removing {missingRoles.Count} missing roles from {nameof(VcRoleService)}");
                    uow._context.RemoveRange(missingRoles);
                    uow.Complete();
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
                                try
                                {
                                    await gusr.RemoveRoleAsync(role).ConfigureAwait(false);
                                }
                                catch
                                {
                                    try
                                    {
                                        await Task.Delay(500).ConfigureAwait(false);
                                        await gusr.RemoveRoleAsync(role).ConfigureAwait(false);
                                    }
                                    catch { }
                                }
                            }
                            //add new
                            if (newVc != null && guildVcRoles.TryGetValue(newVc.Id, out role))
                            {
                                if (!gusr.Roles.Contains(role))
                                {
                                    await Task.Delay(500).ConfigureAwait(false);
                                    await gusr.AddRoleAsync(role).ConfigureAwait(false);
                                }
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
