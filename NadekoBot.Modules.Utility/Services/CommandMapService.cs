using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Utility.Services
{
    public class CommandMapService : IInputTransformer, INService
    {
        private readonly Logger _log;

        public ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>> AliasMaps { get; } = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>>();
        //commandmap
        public CommandMapService(NadekoBot bot)
        {
            _log = LogManager.GetCurrentClassLogger();
            AliasMaps = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>>(
                bot.AllGuildConfigs.ToDictionary(
                    x => x.GuildId,
                        x => new ConcurrentDictionary<string, string>(x.CommandAliases
                            .Distinct(new CommandAliasEqualityComparer())
                            .ToDictionary(ca => ca.Trigger, ca => ca.Mapping))));
        }

        public async Task<string> TransformInput(IGuild guild, IMessageChannel channel, IUser user, string input)
        {
            await Task.Yield();

            if (guild == null || string.IsNullOrWhiteSpace(input))
                return input;
            
            if (guild != null)
            {
                input = input.ToLowerInvariant();
                if (AliasMaps.TryGetValue(guild.Id, out ConcurrentDictionary<string, string> maps))
                {
                    var keys = maps.Keys
                        .OrderByDescending(x => x.Length);

                    foreach (var k in keys)
                    {
                        string newInput;
                        if (input.StartsWith(k + " "))
                            newInput = maps[k] + input.Substring(k.Length, input.Length - k.Length);
                        else if (input == k)
                            newInput = maps[k];
                        else
                            continue;

                        _log.Info(@"--Mapping Command--
            GuildId: {0}
            Trigger: {1}
            Mapping: {2}", guild.Id, input, newInput);

                        try { await channel.SendConfirmAsync($"{input} => {newInput}").ConfigureAwait(false); } catch { }
                        return newInput;
                    }
                }
            }

            return input;
        }
    }

    public class CommandAliasEqualityComparer : IEqualityComparer<CommandAlias>
    {
        public bool Equals(CommandAlias x, CommandAlias y) => x.Trigger == y.Trigger;

        public int GetHashCode(CommandAlias obj) => obj.Trigger.GetHashCode();
    }
}
