using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Core.Services;
using NLog;
using Discord;

namespace NadekoBot.Modules.Administration.Services
{
    public class AutoAssignRoleService : INService
    {
        private readonly Logger _log;
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        //guildid/roleid
        public ConcurrentDictionary<ulong, ulong> AutoAssignedRoles { get; }
        public BlockingCollection<(IGuildUser, ulong)> AutoAssignQueue { get; } = new BlockingCollection<(IGuildUser, ulong)>();

        public AutoAssignRoleService(DiscordSocketClient client, NadekoBot bot, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            _db = db;

            AutoAssignedRoles = new ConcurrentDictionary<ulong, ulong>(
                bot.AllGuildConfigs
                    .Where(x => x.AutoAssignRoleId != 0)
                    .ToDictionary(k => k.GuildId, v => v.AutoAssignRoleId));

            var _queueRunner = Task.Run(async () =>
            {
                while (true)
                {
                    var (user, roleId) = AutoAssignQueue.Take();
                    try
                    {
                        var role = user.Guild.Roles.FirstOrDefault(r => r.Id == roleId);

                        if (role != null)
                            await user.AddRoleAsync(role).ConfigureAwait(false);
                        else
                        {
                            _log.Warn($"Disabled 'Auto assign role' feature on {0} server the role doesn't exist.",
                               roleId);
                            DisableAar(user.GuildId);
                        }
                    }
                    catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _log.Warn($"Disabled 'Auto assign role' feature on {0} server because I don't have role management permissions.",
                            roleId);
                        DisableAar(user.GuildId);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }
            });

            _client.UserJoined += (user) =>
            {
                if (AutoAssignedRoles.TryGetValue(user.Guild.Id, out ulong roleId)
                    && roleId != 0)
                {
                    AutoAssignQueue.Add((user, roleId));
                }
                return Task.CompletedTask;
            };
        }

        public void EnableAar(ulong guildId, ulong roleId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                gc.AutoAssignRoleId = roleId;
                uow.Complete();
            }
            AutoAssignedRoles.AddOrUpdate(guildId, 
                roleId, 
                delegate { return roleId; });
        }

        public void DisableAar(ulong guildId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                gc.AutoAssignRoleId = 0;
                uow.Complete();
            }
            AutoAssignedRoles.TryRemove(guildId, out _);
        }
    }
}
