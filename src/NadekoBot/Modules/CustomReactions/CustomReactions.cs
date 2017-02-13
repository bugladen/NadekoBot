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
using System;
using Newtonsoft.Json;
using NadekoBot.DataStructures;

namespace NadekoBot.Modules.CustomReactions
{
    [NadekoModule("CustomReactions", ".")]
    public class CustomReactions : NadekoModule
    {
        private static CustomReaction[] _globalReactions = new CustomReaction[] { };
        public static CustomReaction[] GlobalReactions => _globalReactions;
        public static ConcurrentDictionary<ulong, CustomReaction[]> GuildReactions { get; } = new ConcurrentDictionary<ulong, CustomReaction[]>();

        public static ConcurrentDictionary<string, uint> ReactionStats { get; } = new ConcurrentDictionary<string, uint>();

        private static new readonly Logger _log;

        static CustomReactions()
        {
            _log = LogManager.GetCurrentClassLogger();
            var sw = Stopwatch.StartNew();
            using (var uow = DbHandler.UnitOfWork())
            {
                var items = uow.CustomReactions.GetAll();
                GuildReactions = new ConcurrentDictionary<ulong, CustomReaction[]>(items.Where(g => g.GuildId != null && g.GuildId != 0).GroupBy(k => k.GuildId.Value).ToDictionary(g => g.Key, g => g.ToArray()));
                _globalReactions = items.Where(g => g.GuildId == null || g.GuildId == 0).ToArray();
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
                        if (reaction.Response != "-")
                        {
                            CREmbed crembed;
                            if (CREmbed.TryParse(reaction.Response, out crembed))
                            {
                                try { await channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText ?? "").ConfigureAwait(false); }
                                catch (Exception ex)
                                {
                                    _log.Warn("Sending CREmbed failed");
                                    _log.Warn(ex);
                                }
                            }
                            else
                            {
                                try { await channel.SendMessageAsync(reaction.ResponseWithContext(umsg)).ConfigureAwait(false); } catch { }
                            }
                        }

                        ReactionStats.AddOrUpdate(reaction.Trigger, 1, (k, old) => ++old);
                        return true;
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
                return false;
            var greaction = grs[new NadekoRandom().Next(0, grs.Length)];

            if (greaction != null)
            {
                CREmbed crembed;
                if (CREmbed.TryParse(greaction.Response, out crembed))
                {
                    try { await channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText ?? "").ConfigureAwait(false); }
                    catch (Exception ex)
                    {
                        _log.Warn("Sending CREmbed failed");
                        _log.Warn(ex);
                    }
                }
                else
                {
                    try { await channel.SendMessageAsync(greaction.ResponseWithContext(umsg)).ConfigureAwait(false); } catch { }
                }
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
                Array.Resize(ref _globalReactions, _globalReactions.Length + 1);
                _globalReactions[_globalReactions.Length - 1] = cr;
            }
            else
            {
                var reactions = GuildReactions.AddOrUpdate(Context.Guild.Id,
                    Array.Empty<CustomReaction>(),
                    (k, old) =>
                    {
                        Array.Resize(ref old, old.Length + 1);
                        old[old.Length - 1] = cr;
                        return old;
                    });
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
            CustomReaction[] customReactions;
            if (Context.Guild == null)
                customReactions = GlobalReactions.Where(cr => cr != null).ToArray();
            else
                customReactions = GuildReactions.GetOrAdd(Context.Guild.Id, Array.Empty<CustomReaction>()).Where(cr => cr != null).ToArray();

            if (customReactions == null || !customReactions.Any())
                await Context.Channel.SendErrorAsync("No custom reactions found").ConfigureAwait(false);
            else
            {
                var lastPage = customReactions.Length / 20;
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
            CustomReaction[] customReactions;
            if (Context.Guild == null)
                customReactions = GlobalReactions.Where(cr => cr != null).ToArray();
            else
                customReactions = GuildReactions.GetOrAdd(Context.Guild.Id, new CustomReaction[]{ }).Where(cr => cr != null).ToArray();

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
            CustomReaction[] customReactions;
            if (Context.Guild == null)
                customReactions = GlobalReactions.Where(cr => cr != null).ToArray();
            else
                customReactions = GuildReactions.GetOrAdd(Context.Guild.Id, new CustomReaction[]{ }).Where(cr => cr != null).ToArray();

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
            CustomReaction[] customReactions;
            if (Context.Guild == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(Context.Guild.Id, new CustomReaction[]{ });

            var found = customReactions.FirstOrDefault(cr => cr?.Id == id);

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
                    //todo i can dramatically improve performance of this, if Ids are ordered.
                    _globalReactions = GlobalReactions.Where(cr => cr?.Id != toDelete.Id).ToArray();
                    success = true;
                }
                else if ((toDelete.GuildId != null && toDelete.GuildId != 0) && Context.Guild.Id == toDelete.GuildId)
                {
                    uow.CustomReactions.Remove(toDelete);
                    GuildReactions.AddOrUpdate(Context.Guild.Id, new CustomReaction[] { }, (key, old) =>
                    {
                        return old.Where(cr => cr?.Id != toDelete.Id).ToArray();
                    });
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
            var ordered = ReactionStats.OrderByDescending(x => x.Value).ToArray();
            if (!ordered.Any())
                return;
            var lastPage = ordered.Length / 9;
            await Context.Channel.SendPaginatedConfirmAsync(page,
                (curPage) => ordered.Skip((curPage - 1) * 9)
                                    .Take(9)
                                    .Aggregate(new EmbedBuilder().WithOkColor().WithTitle($"Custom Reaction Stats"),
                                            (agg, cur) => agg.AddField(efb => efb.WithName(cur.Key).WithValue(cur.Value.ToString()).WithIsInline(true))), lastPage)
                .ConfigureAwait(false);
        }
    }
}