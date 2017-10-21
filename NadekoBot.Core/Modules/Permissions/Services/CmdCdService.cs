using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Permissions.Services
{
    public class CmdCdService : ILateBlocker, INService
    {
        public ConcurrentDictionary<ulong, ConcurrentHashSet<CommandCooldown>> CommandCooldowns { get; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<ActiveCooldown>> ActiveCooldowns { get; } = new ConcurrentDictionary<ulong, ConcurrentHashSet<ActiveCooldown>>();

        public CmdCdService(NadekoBot bot)
        {
            CommandCooldowns = new ConcurrentDictionary<ulong, ConcurrentHashSet<CommandCooldown>>(
                bot.AllGuildConfigs.ToDictionary(k => k.GuildId, 
                                 v => new ConcurrentHashSet<CommandCooldown>(v.CommandCooldowns)));
        }

        public Task<bool> TryBlockLate(DiscordSocketClient client, IUserMessage msg, IGuild guild, 
            IMessageChannel channel, IUser user, string moduleName, string commandName)
        {
            if (guild == null)
                return Task.FromResult(false);
            var cmdcds = CommandCooldowns.GetOrAdd(guild.Id, new ConcurrentHashSet<CommandCooldown>());
            CommandCooldown cdRule;
            if ((cdRule = cmdcds.FirstOrDefault(cc => cc.CommandName == commandName.ToLowerInvariant())) != null)
            {
                var activeCdsForGuild = ActiveCooldowns.GetOrAdd(guild.Id, new ConcurrentHashSet<ActiveCooldown>());
                if (activeCdsForGuild.FirstOrDefault(ac => ac.UserId == user.Id && ac.Command == commandName.ToLowerInvariant()) != null)
                {
                    return Task.FromResult(true);
                }
                activeCdsForGuild.Add(new ActiveCooldown()
                {
                    UserId = user.Id,
                    Command = commandName.ToLowerInvariant(),
                });
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(cdRule.Seconds * 1000);
                        activeCdsForGuild.RemoveWhere(ac => ac.Command == commandName.ToLowerInvariant() && ac.UserId == user.Id);
                    }
                    catch
                    {
                        // ignored
                    }
                });
            }
            return Task.FromResult(false);
        }
    }

    public class ActiveCooldown
    {
        public string Command { get; set; }
        public ulong UserId { get; set; }
    }
}
