using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Services;
using NadekoBot.Attributes;
using System.Collections.Concurrent;
using NadekoBot.Services.Database.Models;
using Discord;
using NadekoBot.Extensions;
using NLog;
using System.Diagnostics;
using Discord.WebSocket;

namespace NadekoBot.Modules.CustomReactions
{
    [NadekoModule("CustomReactions", ".")]
    public class CustomReactions : DiscordModule
    {
        public static ConcurrentHashSet<CustomReaction> GlobalReactions { get; } = new ConcurrentHashSet<CustomReaction>();
        public static ConcurrentDictionary<ulong, ConcurrentHashSet<CustomReaction>> GuildReactions { get; } = new ConcurrentDictionary<ulong, ConcurrentHashSet<CustomReaction>>();

        public static ConcurrentDictionary<string, uint> ReactionStats { get; } = new ConcurrentDictionary<string, uint>();

        private static new readonly Logger _log;

        static CustomReactions()
        {
            _log = LogManager.GetCurrentClassLogger();
            var sw = Stopwatch.StartNew();
            using (var uow = DbHandler.UnitOfWork())
            {
                var items = uow.CustomReactions.GetAll();
                GuildReactions = new ConcurrentDictionary<ulong, ConcurrentHashSet<CustomReaction>>(items.Where(g => g.GuildId != null && g.GuildId != 0).GroupBy(k => k.GuildId.Value).ToDictionary(g => g.Key, g => new ConcurrentHashSet<CustomReaction>(g)));
                GlobalReactions = new ConcurrentHashSet<CustomReaction>(items.Where(g => g.GuildId == null || g.GuildId == 0));
            }
            sw.Stop();
            _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
        }

        public void ClearStats() => ReactionStats.Clear();

        public static async Task<bool> TryExecuteCustomReaction(SocketUserMessage umsg)
        {
            var channel = umsg.Channel as SocketTextChannel;
            if (channel == null)
                return false;

            var content = umsg.Content.Trim().ToLowerInvariant();
            ConcurrentHashSet<CustomReaction> reactions;

            GuildReactions.TryGetValue(channel.Guild.Id, out reactions);
            if (reactions != null && reactions.Any())
            {
                var reaction = reactions.Where(cr =>
                {
                    var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                    var trigger = cr.TriggerWithContext(umsg).Trim().ToLowerInvariant();
                    return ((hasTarget && content.StartsWith(trigger + " ")) || content == trigger);
                }).Shuffle().FirstOrDefault();
                if (reaction != null)
                {
                    if (reaction.Response != "-")
                        try { await channel.SendMessageAsync(reaction.ResponseWithContext(umsg)).ConfigureAwait(false); } catch { }

                    ReactionStats.AddOrUpdate(reaction.Trigger, 1, (k, old) => ++old);
                    return true;
                }
            }
            var greaction = GlobalReactions.Where(cr =>
            {
                var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                var trigger = cr.TriggerWithContext(umsg).Trim().ToLowerInvariant();
                return ((hasTarget && content.StartsWith(trigger + " ")) || content == trigger);
            }).Shuffle().FirstOrDefault();

            if (greaction != null)
            {
                try { await channel.SendMessageAsync(greaction.ResponseWithContext(umsg)).ConfigureAwait(false); } catch { }
                ReactionStats.AddOrUpdate(greaction.Trigger, 1, (k, old) => ++old);
                return true;
            }
            return false;
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task AddCustReact(string key, [Remainder] string message)
        {
            var channel = Context.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
                return;

            key = key.ToLowerInvariant();

            if ((channel == null && !NadekoBot.Credentials.IsOwner(Context.User)) || (channel != null && !((IGuildUser)Context.User).GuildPermissions.Administrator))
            {
                try { await Context.Channel.SendErrorAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
                return;
            }

            var cr = new CustomReaction()
            {
                GuildId = channel?.Guild.Id,
                IsRegex = false,
                Trigger = key,
                Response = message,
            };

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.CustomReactions.Add(cr);

                await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (channel == null)
            {
                GlobalReactions.Add(cr);
            }
            else
            {
                var reactions = GuildReactions.GetOrAdd(Context.Guild.Id, new ConcurrentHashSet<CustomReaction>());
                reactions.Add(cr);
            }

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle("New Custom Reaction")
                .WithDescription($"#{cr.Id}")
                .AddField(efb => efb.WithName("Trigger").WithValue(key))
                .AddField(efb => efb.WithName("Response").WithValue(message))
                ).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task ListCustReact(int page = 1)
        {
            if (page < 1 || page > 1000)
                return;
            ConcurrentHashSet<CustomReaction> customReactions;
            if (Context.Guild == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(Context.Guild.Id, new ConcurrentHashSet<CustomReaction>());

            if (customReactions == null || !customReactions.Any())
                await Context.Channel.SendErrorAsync("No custom reactions found").ConfigureAwait(false);
            else
            {
                var lastPage = customReactions.Count / 20;
                await Context.Channel.SendPaginatedConfirmAsync(page, curPage =>
                    new EmbedBuilder().WithOkColor()
                        .WithTitle("Custom reactions")
                        .WithDescription(string.Join("\n", customReactions.OrderBy(cr => cr.Trigger)
                                                     .Skip((curPage - 1) * 20)
                                                     .Take(20)
                                                     .Select(cr => $"`#{cr.Id}`  `Trigger:` {cr.Trigger}"))), lastPage)
                                 .ConfigureAwait(false);
            }
        }

        public enum All
        {
            All
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task ListCustReact(All x)
        {
            ConcurrentHashSet<CustomReaction> customReactions;
            if (Context.Guild == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(Context.Guild.Id, new ConcurrentHashSet<CustomReaction>());

            if (customReactions == null || !customReactions.Any())
                await Context.Channel.SendErrorAsync("No custom reactions found").ConfigureAwait(false);
            else
            {
                var txtStream = await customReactions.GroupBy(cr => cr.Trigger)
                                                          .OrderBy(cr => cr.Key)
                                                          .Select(cr => new { Trigger = cr.Key, Responses = cr.Select(y => new { id = y.Id, text = y.Response }).ToList() })
                                                          .ToJson()
                                                          .ToStream()
                                                          .ConfigureAwait(false);
                if (Context.Guild == null) // its a private one, just send back
                    await Context.Channel.SendFileAsync(txtStream, "customreactions.txt", "List of all custom reactions").ConfigureAwait(false);
                else
                    await ((IGuildUser)Context.User).SendFileAsync(txtStream, "customreactions.txt", "List of all custom reactions").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ListCustReactG(int page = 1)
        {
            if (page < 1 || page > 10000)
                return;
            ConcurrentHashSet<CustomReaction> customReactions;
            if (Context.Guild == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(Context.Guild.Id, new ConcurrentHashSet<CustomReaction>());

            if (customReactions == null || !customReactions.Any())
                await Context.Channel.SendErrorAsync("No custom reactions found").ConfigureAwait(false);
            else
            {
                var ordered = customReactions
                    .GroupBy(cr => cr.Trigger)
                    .OrderBy(cr => cr.Key)
                    .ToList();

                var lastPage = ordered.Count / 20;
                await Context.Channel.SendPaginatedConfirmAsync(page, (curPage) =>
                    new EmbedBuilder().WithOkColor()
                        .WithTitle($"Custom Reactions (grouped)")
                        .WithDescription(string.Join("\r\n", ordered
                                                         .Skip((curPage - 1) * 20)
                                                         .Take(20)
                                                         .Select(cr => $"**{cr.Key.Trim().ToLowerInvariant()}** `x{cr.Count()}`"))), lastPage)
                             .ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ShowCustReact(int id)
        {
            ConcurrentHashSet<CustomReaction> customReactions;
            if (Context.Guild == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(Context.Guild.Id, new ConcurrentHashSet<CustomReaction>());

            var found = customReactions.FirstOrDefault(cr => cr.Id == id);

            if (found == null)
                await Context.Channel.SendErrorAsync("No custom reaction found with that id.").ConfigureAwait(false);
            else
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription($"#{id}")
                    .AddField(efb => efb.WithName("Trigger").WithValue(found.Trigger))
                    .AddField(efb => efb.WithName("Response").WithValue(found.Response + "\n```css\n" + found.Response + "```"))
                    ).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DelCustReact(int id)
        {
            if ((Context.Guild == null && !NadekoBot.Credentials.IsOwner(Context.User)) || (Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator))
            {
                try { await Context.Channel.SendErrorAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
                return;
            }

            var success = false;
            CustomReaction toDelete;
            using (var uow = DbHandler.UnitOfWork())
            {
                toDelete = uow.CustomReactions.Get(id);
                if (toDelete == null) //not found
                    return;

                if ((toDelete.GuildId == null || toDelete.GuildId == 0) && Context.Guild == null)
                {
                    uow.CustomReactions.Remove(toDelete);
                    GlobalReactions.RemoveWhere(cr => cr.Id == toDelete.Id);
                    success = true;
                }
                else if ((toDelete.GuildId != null && toDelete.GuildId != 0) && Context.Guild.Id == toDelete.GuildId)
                {
                    uow.CustomReactions.Remove(toDelete);
                    GuildReactions.GetOrAdd(Context.Guild.Id, new ConcurrentHashSet<CustomReaction>()).RemoveWhere(cr => cr.Id == toDelete.Id);
                    success = true;
                }
                if (success)
                    await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (success)
                await Context.Channel.SendConfirmAsync("Deleted custom reaction", toDelete.ToString()).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorAsync("Failed to find that custom reaction.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task CrStatsClear(string trigger = null)
        {
            if (string.IsNullOrWhiteSpace(trigger))
            {
                ClearStats();
                await Context.Channel.SendConfirmAsync($"Custom reaction stats cleared.").ConfigureAwait(false);
            }
            else
            {
                uint throwaway;
                if (ReactionStats.TryRemove(trigger, out throwaway))
                {
                    await Context.Channel.SendConfirmAsync($"Stats cleared for `{trigger}` custom reaction.").ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorAsync("No stats for that trigger found, no action taken.").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task CrStats(int page = 1)
        {
            if (page < 1)
                return;
            var ordered = ReactionStats.OrderByDescending(x => x.Value).ToList();
            var lastPage = ordered.Count / 9;
            await Context.Channel.SendPaginatedConfirmAsync(page,
                (curPage) => ordered.Skip((curPage - 1) * 9)
                                    .Take(9)
                                    .Aggregate(new EmbedBuilder().WithOkColor().WithTitle($"Custom Reaction Stats"),
                                            (agg, cur) => agg.AddField(efb => efb.WithName(cur.Key).WithValue(cur.Value.ToString()).WithIsInline(true))), lastPage)
                .ConfigureAwait(false);
        }
    }
}