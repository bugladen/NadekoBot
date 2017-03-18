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
using NadekoBot.DataStructures;

namespace NadekoBot.Modules.CustomReactions
{
    public static class CustomReactionExtensions
    {
        public static async Task<IUserMessage> Send(this CustomReaction cr, IUserMessage context)
        {
            var channel = cr.DmResponse ? await context.Author.CreateDMChannelAsync() : context.Channel;
            
            CustomReactions.ReactionStats.AddOrUpdate(cr.Trigger, 1, (k, old) => ++old);

            CREmbed crembed;
            if (CREmbed.TryParse(cr.Response, out crembed))
            {
                return await channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText ?? "");
            }
            return await channel.SendMessageAsync(cr.ResponseWithContext(context));
        }
    }

    [NadekoModule("CustomReactions", ".")]
    public class CustomReactions : NadekoTopLevelModule
    {
        private static CustomReaction[] _globalReactions = new CustomReaction[] { };
        public static CustomReaction[] GlobalReactions => _globalReactions;
        public static ConcurrentDictionary<ulong, CustomReaction[]> GuildReactions { get; } = new ConcurrentDictionary<ulong, CustomReaction[]>();

        public static ConcurrentDictionary<string, uint> ReactionStats { get; } = new ConcurrentDictionary<string, uint>();

        private new static readonly Logger _log;

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

        public static CustomReaction TryGetCustomReaction(SocketUserMessage umsg)
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

        [NadekoCommand, Usage, Description, Aliases]
        public async Task AddCustReact(string key, [Remainder] string message)
        {
            var channel = Context.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
                return;

            key = key.ToLowerInvariant();

            if ((channel == null && !NadekoBot.Credentials.IsOwner(Context.User)) || (channel != null && !((IGuildUser)Context.User).GuildPermissions.Administrator))
            {
                await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
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
                GuildReactions.AddOrUpdate(Context.Guild.Id,
                    new CustomReaction[] { cr },
                    (k, old) =>
                    {
                        Array.Resize(ref old, old.Length + 1);
                        old[old.Length - 1] = cr;
                        return old;
                    });
            }

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle(GetText("new_cust_react"))
                .WithDescription($"#{cr.Id}")
                .AddField(efb => efb.WithName(GetText("trigger")).WithValue(key))
                .AddField(efb => efb.WithName(GetText("response")).WithValue(message))
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
            {
                await ReplyErrorLocalized("no_found").ConfigureAwait(false);
                return;
            }

            var lastPage = customReactions.Length / 20;
            await Context.Channel.SendPaginatedConfirmAsync(page, curPage =>
                new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("name"))
                    .WithDescription(string.Join("\n", customReactions.OrderBy(cr => cr.Trigger)
                                                    .Skip((curPage - 1) * 20)
                                                    .Take(20)
                                                    .Select(cr => $"`#{cr.Id}`  `{GetText("trigger")}:` {cr.Trigger}"))), lastPage)
                                .ConfigureAwait(false);
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
            {
                await ReplyErrorLocalized("no_found").ConfigureAwait(false);
                return;
            }

            var txtStream = await customReactions.GroupBy(cr => cr.Trigger)
                                                        .OrderBy(cr => cr.Key)
                                                        .Select(cr => new { Trigger = cr.Key, Responses = cr.Select(y => new { id = y.Id, text = y.Response }).ToList() })
                                                        .ToJson()
                                                        .ToStream()
                                                        .ConfigureAwait(false);

            if (Context.Guild == null) // its a private one, just send back
                await Context.Channel.SendFileAsync(txtStream, "customreactions.txt", GetText("list_all")).ConfigureAwait(false);
            else
                await ((IGuildUser)Context.User).SendFileAsync(txtStream, "customreactions.txt", GetText("list_all")).ConfigureAwait(false);
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
            {
                await ReplyErrorLocalized("no_found").ConfigureAwait(false);
            }
            else
            {
                var ordered = customReactions
                    .GroupBy(cr => cr.Trigger)
                    .OrderBy(cr => cr.Key)
                    .ToList();

                var lastPage = ordered.Count / 20;
                await Context.Channel.SendPaginatedConfirmAsync(page, (curPage) =>
                    new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("name"))
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
            {
                await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
                return;
            }
            else
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription($"#{id}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(found.Trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(found.Response + "\n```css\n" + found.Response + "```"))
                    ).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DelCustReact(int id)
        {
            if ((Context.Guild == null && !NadekoBot.Credentials.IsOwner(Context.User)) || (Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator))
            {
                await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
                return;
            }

            var success = false;
            CustomReaction toDelete;
            using (var uow = DbHandler.UnitOfWork())
            {
                toDelete = uow.CustomReactions.Get(id);
                if (toDelete == null) //not found
                    success = false;
                else
                {
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
            }

            if (success)
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("deleted"))
                    .WithDescription("#" + toDelete.Id)
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(toDelete.Trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(toDelete.Response)));
            }
            else
            {
                await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task CrDm(int id)
        {
            if ((Context.Guild == null && !NadekoBot.Credentials.IsOwner(Context.User)) || 
                (Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator))
            {
                await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
                return;
            }

            CustomReaction[] reactions = new CustomReaction[0];

            if (Context.Guild == null)
                reactions = GlobalReactions;
            else
            {
                GuildReactions.TryGetValue(Context.Guild.Id, out reactions);
            }
            if (reactions.Any())
            {
                var reaction = reactions.FirstOrDefault(x => x.Id == id);

                if (reaction == null)
                {
                    await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
                    return;
                }

                var setValue = reaction.DmResponse = !reaction.DmResponse;

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.CustomReactions.Get(id).DmResponse = setValue;
                    uow.Complete();
                }

                if (setValue)
                {
                    await ReplyConfirmLocalized("crdm_enabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("crdm_disabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
                }
            }
            else
            {
                await ReplyErrorLocalized("no_found").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task CrAd(int id)
        {
            if ((Context.Guild == null && !NadekoBot.Credentials.IsOwner(Context.User)) ||
                (Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator))
            {
                await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
                return;
            }

            CustomReaction[] reactions = new CustomReaction[0];

            if (Context.Guild == null)
                reactions = GlobalReactions;
            else
            {
                GuildReactions.TryGetValue(Context.Guild.Id, out reactions);
            }
            if (reactions.Any())
            {
                var reaction = reactions.FirstOrDefault(x => x.Id == id);

                if (reaction == null)
                {
                    await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
                    return;
                }

                var setValue = reaction.AutoDeleteTrigger = !reaction.AutoDeleteTrigger;

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.CustomReactions.Get(id).AutoDeleteTrigger = setValue;
                    uow.Complete();
                }

                if (setValue)
                {
                    await ReplyConfirmLocalized("crad_enabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("crad_disabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
                }
            }
            else
            {
                await ReplyErrorLocalized("no_found").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task CrStatsClear(string trigger = null)
        {
            if (string.IsNullOrWhiteSpace(trigger))
            {
                ClearStats();
                await ReplyConfirmLocalized("all_stats_cleared").ConfigureAwait(false);
            }
            else
            {
                uint throwaway;
                if (ReactionStats.TryRemove(trigger, out throwaway))
                {
                    await ReplyErrorLocalized("stats_cleared", Format.Bold(trigger)).ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalized("stats_not_found").ConfigureAwait(false);
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
                                    .Aggregate(new EmbedBuilder().WithOkColor().WithTitle(GetText("stats")),
                                            (agg, cur) => agg.AddField(efb => efb.WithName(cur.Key).WithValue(cur.Value.ToString()).WithIsInline(true))), lastPage)
                .ConfigureAwait(false);
        }
    }
}