using Discord;
using Discord.WebSocket;
using NadekoBot.DataStructures.ModuleBehaviors;
using NadekoBot.Services.Database.Models;
using NLog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System;
using System.Threading.Tasks;
using NadekoBot.Services.Permissions;
using NadekoBot.Extensions;

namespace NadekoBot.Services.CustomReactions
{
    public class CustomReactionsService : IEarlyBlockingExecutor
    {
        public CustomReaction[] GlobalReactions = new CustomReaction[] { };
        public ConcurrentDictionary<ulong, CustomReaction[]> GuildReactions { get; } = new ConcurrentDictionary<ulong, CustomReaction[]>();

        public ConcurrentDictionary<string, uint> ReactionStats { get; } = new ConcurrentDictionary<string, uint>();

        private readonly Logger _log;
        private readonly DbService _db;
        private readonly DiscordShardedClient _client;
        private readonly PermissionService _perms;

        public CustomReactionsService(PermissionService perms, DbService db, DiscordShardedClient client)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            _client = client;
            _perms = perms;

            var sw = Stopwatch.StartNew();
            using (var uow = _db.UnitOfWork)
            {
                var items = uow.CustomReactions.GetAll();
                GuildReactions = new ConcurrentDictionary<ulong, CustomReaction[]>(items.Where(g => g.GuildId != null && g.GuildId != 0).GroupBy(k => k.GuildId.Value).ToDictionary(g => g.Key, g => g.ToArray()));
                GlobalReactions = items.Where(g => g.GuildId == null || g.GuildId == 0).ToArray();
            }
            sw.Stop();
            _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
        }

        public void ClearStats() => ReactionStats.Clear();

        public CustomReaction TryGetCustomReaction(IUserMessage umsg)
        {
            var channel = umsg.Channel as SocketTextChannel;
            if (channel == null)
                return null;

            var content = umsg.Content.Trim().ToLowerInvariant();

            if (GuildReactions.TryGetValue(channel.Guild.Id, out CustomReaction[] reactions))
                if (reactions != null && reactions.Any())
                {
                    var rs = reactions.Where(cr =>
                    {
                        if (cr == null)
                            return false;

                        var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                        var trigger = cr.TriggerWithContext(umsg, _client).Trim().ToLowerInvariant();
                        return ((hasTarget && content.StartsWith(trigger + " ")) || content == trigger);
                    }).ToArray();

                    if (rs.Length != 0)
                    {
                        var reaction = rs[new NadekoRandom().Next(0, rs.Length)];
                        if (reaction != null)
                        {
                            if (reaction.Response == "-")
                                return null;
                            return reaction;
                        }
                    }
                }

            var grs = GlobalReactions.Where(cr =>
            {
                if (cr == null)
                    return false;
                var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                var trigger = cr.TriggerWithContext(umsg, _client).Trim().ToLowerInvariant();
                return ((hasTarget && content.StartsWith(trigger + " ")) || content == trigger);
            }).ToArray();
            if (grs.Length == 0)
                return null;
            var greaction = grs[new NadekoRandom().Next(0, grs.Length)];

            return greaction;
        }

        public async Task<bool> TryExecuteEarly(DiscordShardedClient client, IGuild guild, IUserMessage msg)
        {
            // maybe this message is a custom reaction
            var cr = await Task.Run(() => TryGetCustomReaction(msg)).ConfigureAwait(false);
            if (cr != null)
            {
                try
                {
                    if (guild is SocketGuild sg)
                    {
                        var pc = _perms.GetCache(guild.Id);
                        if (!pc.Permissions.CheckPermissions(msg, cr.Trigger, "ActualCustomReactions",
                            out int index))
                        {
                            if (pc.Verbose)
                            {
                                var returnMsg = $"Permission number #{index + 1} **{pc.Permissions[index].GetCommand(sg)}** is preventing this action.";
                                try { await msg.Channel.SendErrorAsync(returnMsg).ConfigureAwait(false); } catch { }
                                _log.Info(returnMsg);
                            }
                            return true;
                        }
                    }
                    await cr.Send(msg, _client, this).ConfigureAwait(false);

                    if (cr.AutoDeleteTrigger)
                    {
                        try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Warn("Sending CREmbed failed");
                    _log.Warn(ex);
                }
            }
            return false;
        }
    }
}
