using Discord;
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using NLog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace NadekoBot.Services
{
    public class CustomReactions
    {
        private CustomReaction[] _globalReactions = new CustomReaction[] { };
        public CustomReaction[] GlobalReactions => _globalReactions;
        public ConcurrentDictionary<ulong, CustomReaction[]> GuildReactions { get; } = new ConcurrentDictionary<ulong, CustomReaction[]>();

        public ConcurrentDictionary<string, uint> ReactionStats { get; } = new ConcurrentDictionary<string, uint>();

        private readonly Logger _log;
        private readonly DbHandler _db;

        public CustomReactions(DbHandler db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            var sw = Stopwatch.StartNew();
            using (var uow = _db.UnitOfWork)
            {
                var items = uow.CustomReactions.GetAll();
                GuildReactions = new ConcurrentDictionary<ulong, CustomReaction[]>(items.Where(g => g.GuildId != null && g.GuildId != 0).GroupBy(k => k.GuildId.Value).ToDictionary(g => g.Key, g => g.ToArray()));
                _globalReactions = items.Where(g => g.GuildId == null || g.GuildId == 0).ToArray();
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
            CustomReaction[] reactions;

            GuildReactions.TryGetValue(channel.Guild.Id, out reactions);
            if (reactions != null && reactions.Any())
            {
                var rs = reactions.Where(cr =>
                {
                    if (cr == null)
                        return false;

                    var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                    var trigger = cr.TriggerWithContext(umsg).Trim().ToLowerInvariant();
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
                var trigger = cr.TriggerWithContext(umsg).Trim().ToLowerInvariant();
                return ((hasTarget && content.StartsWith(trigger + " ")) || content == trigger);
            }).ToArray();
            if (grs.Length == 0)
                return null;
            var greaction = grs[new NadekoRandom().Next(0, grs.Length)];

            return greaction;
        }
    }
}
