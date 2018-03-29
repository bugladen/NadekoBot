using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Gambling.Services;
using Discord.WebSocket;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        public enum ClaimTitles
        {
            Lonely,
            Devoted,
            Rookie,
            Schemer,
            Dilettante,
            Intermediate,
            Seducer,
            Expert,
            Veteran,
            Incubis,
            Harem_King,
            Harem_God,
        }

        public enum AffinityTitles
        {
            Pure,
            Faithful,
            Defiled,
            Cheater,
            Tainted,
            Corrupted,
            Lewd,
            Sloot,
            Depraved,
            Harlot
        }

        [Group]
        public class WaifuClaimCommands : NadekoSubmodule<WaifuService>
        {
            enum WaifuClaimResult
            {
                Success,
                NotEnoughFunds,
                InsufficientAmount
            }

            private readonly ICurrencyService _cs;
            private readonly DbService _db;
            private readonly IDataCache _cache;
            private readonly DiscordSocketClient _client;

            public WaifuClaimCommands(IDataCache cache, ICurrencyService cs, DbService db, DiscordSocketClient client)
            {
                _cs = cs;
                _db = db;
                _cache = cache;
                _client = client;
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            public async Task WaifuReset()
            {
                var price = _service.GetResetPrice(Context.User);
                var embed = new EmbedBuilder()
                        .WithTitle(GetText("waifu_reset_confirm"))
                        .WithDescription(GetText("cost", Format.Bold(price + _bc.BotConfig.CurrencySign)));

                if (!await PromptUserConfirmAsync(embed))
                    return;

                if (await _service.TryReset(Context.User).ConfigureAwait(false))
                {
                    await ReplyConfirmLocalized("waifu_reset").ConfigureAwait(false);
                    return;
                }
                await ReplyErrorLocalized("waifu_reset_fail").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuClaim(int amount, [Remainder]IUser target)
            {
                if (amount < _bc.BotConfig.MinWaifuPrice)
                {
                    await ReplyErrorLocalized("waifu_isnt_cheap", _bc.BotConfig.MinWaifuPrice + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }

                if (target.Id == Context.User.Id)
                {
                    await ReplyErrorLocalized("waifu_not_yourself").ConfigureAwait(false);
                    return;
                }

                WaifuClaimResult result;
                WaifuInfo w;
                bool isAffinity;
                using (var uow = _db.UnitOfWork)
                {
                    w = uow.Waifus.ByWaifuUserId(target.Id);
                    isAffinity = (w?.Affinity?.UserId == Context.User.Id);
                    if (w == null)
                    {
                        var claimer = uow.DiscordUsers.GetOrCreate(Context.User);
                        var waifu = uow.DiscordUsers.GetOrCreate(target);
                        if (!await _cs.RemoveAsync(Context.User.Id, "Claimed Waifu", amount, gamble: true))
                        {
                            result = WaifuClaimResult.NotEnoughFunds;
                        }
                        else
                        {
                            uow.Waifus.Add(w = new WaifuInfo()
                            {
                                Waifu = waifu,
                                Claimer = claimer,
                                Affinity = null,
                                Price = amount
                            });
                            uow._context.WaifuUpdates.Add(new WaifuUpdate()
                            {
                                User = waifu,
                                Old = null,
                                New = claimer,
                                UpdateType = WaifuUpdateType.Claimed
                            });
                            result = WaifuClaimResult.Success;
                        }
                    }
                    else if (isAffinity && amount > w.Price * 0.88f)
                    {
                        if (!await _cs.RemoveAsync(Context.User.Id, "Claimed Waifu", amount, gamble: true))
                        {
                            result = WaifuClaimResult.NotEnoughFunds;
                        }
                        else
                        {
                            var oldClaimer = w.Claimer;
                            w.Claimer = uow.DiscordUsers.GetOrCreate(Context.User);
                            w.Price = amount + (amount / 4);
                            result = WaifuClaimResult.Success;

                            uow._context.WaifuUpdates.Add(new WaifuUpdate()
                            {
                                User = w.Waifu,
                                Old = oldClaimer,
                                New = w.Claimer,
                                UpdateType = WaifuUpdateType.Claimed
                            });
                        }
                    }
                    else if (amount >= w.Price * 1.1f) // if no affinity
                    {
                        if (!await _cs.RemoveAsync(Context.User.Id, "Claimed Waifu", amount, gamble: true))
                        {
                            result = WaifuClaimResult.NotEnoughFunds;
                        }
                        else
                        {
                            var oldClaimer = w.Claimer;
                            w.Claimer = uow.DiscordUsers.GetOrCreate(Context.User);
                            w.Price = amount;
                            result = WaifuClaimResult.Success;

                            uow._context.WaifuUpdates.Add(new WaifuUpdate()
                            {
                                User = w.Waifu,
                                Old = oldClaimer,
                                New = w.Claimer,
                                UpdateType = WaifuUpdateType.Claimed
                            });
                        }
                    }
                    else
                        result = WaifuClaimResult.InsufficientAmount;


                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (result == WaifuClaimResult.InsufficientAmount)
                {
                    await ReplyErrorLocalized("waifu_not_enough", Math.Ceiling(w.Price * (isAffinity ? 0.88f : 1.1f))).ConfigureAwait(false);
                    return;
                }
                if (result == WaifuClaimResult.NotEnoughFunds)
                {
                    await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }
                var msg = GetText("waifu_claimed", 
                    Format.Bold(target.ToString()), 
                    amount + _bc.BotConfig.CurrencySign);
                if (w.Affinity?.UserId == Context.User.Id)
                    msg += "\n" + GetText("waifu_fulfilled", target, w.Price + _bc.BotConfig.CurrencySign);
                else
                    msg = " " + msg;
                await Context.Channel.SendConfirmAsync(Context.User.Mention + msg).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuTransfer(IUser waifu, IUser newOwner)
            {
                if(!await _service.WaifuTransfer(Context.User, waifu.Id, newOwner)
                    .ConfigureAwait(false))
                {
                    await ReplyErrorLocalized("waifu_transfer_fail").ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalized("waifu_transfer_success",
                    Format.Bold(waifu.ToString()),
                    Format.Bold(Context.User.ToString()),
                    Format.Bold(newOwner.ToString())).ConfigureAwait(false);
            }

            public enum DivorceResult
            {
                Success,
                SucessWithPenalty,
                NotYourWife,
                Cooldown
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public Task Divorce([Remainder]IGuildUser target) => Divorce(target.Id);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task Divorce([Remainder]ulong targetId)
            {
                if (targetId == Context.User.Id)
                    return;

                DivorceResult result;
                TimeSpan? remaining = null;
                var amount = 0;
                WaifuInfo w = null;
                using (var uow = _db.UnitOfWork)
                {
                    w = uow.Waifus.ByWaifuUserId(targetId);
                    var now = DateTime.UtcNow;
                    if (w?.Claimer == null || w.Claimer.UserId != Context.User.Id)
                        result = DivorceResult.NotYourWife;
                    else if (!_cache.TryAddDivorceCooldown(Context.User.Id, out remaining))
                    {
                        result = DivorceResult.Cooldown;
                    }
                    else
                    {
                        amount = w.Price / 2;

                        if (w.Affinity?.UserId == Context.User.Id)
                        {
                            await _cs.AddAsync(w.Waifu.UserId, "Waifu Compensation", amount, gamble: true).ConfigureAwait(false);
                            w.Price = (int)Math.Floor(w.Price * 0.75f);
                            result = DivorceResult.SucessWithPenalty;
                        }
                        else
                        {
                            await _cs.AddAsync(Context.User.Id, "Waifu Refund", amount, gamble: true).ConfigureAwait(false);

                            result = DivorceResult.Success;
                        }
                        var oldClaimer = w.Claimer;
                        w.Claimer = null;

                        uow._context.WaifuUpdates.Add(new WaifuUpdate()
                        {
                            User = w.Waifu,
                            Old = oldClaimer,
                            New = null,
                            UpdateType = WaifuUpdateType.Claimed
                        });
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (result == DivorceResult.SucessWithPenalty)
                {
                    await ReplyConfirmLocalized("waifu_divorced_like", Format.Bold(w.Waifu.ToString()), amount + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                }
                else if (result == DivorceResult.Success)
                {
                    await ReplyConfirmLocalized("waifu_divorced_notlike", amount + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                }
                else if (result == DivorceResult.NotYourWife)
                {
                    await ReplyErrorLocalized("waifu_not_yours").ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalized("waifu_recent_divorce", 
                        Format.Bold(((int)remaining?.TotalHours).ToString()),
                        Format.Bold(remaining?.Minutes.ToString())).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuClaimerAffinity([Remainder]IGuildUser u = null)
            {
                if (u?.Id == Context.User.Id)
                {
                    await ReplyErrorLocalized("waifu_egomaniac").ConfigureAwait(false);
                    return;
                }
                DiscordUser oldAff = null;
                var sucess = false;
                TimeSpan? remaining = null;
                using (var uow = _db.UnitOfWork)
                {
                    var w = uow.Waifus.ByWaifuUserId(Context.User.Id);
                    var newAff = u == null ? null : uow.DiscordUsers.GetOrCreate(u);
                    var now = DateTime.UtcNow;
                    if (w?.Affinity?.UserId == u?.Id)
                    {

                    }
                    else if (!_cache.TryAddAffinityCooldown(Context.User.Id, out remaining))
                    {
                    }
                    else if (w == null)
                    {
                        var thisUser = uow.DiscordUsers.GetOrCreate(Context.User);
                        uow.Waifus.Add(new WaifuInfo()
                        {
                            Affinity = newAff,
                            Waifu = thisUser,
                            Price = 1,
                            Claimer = null
                        });
                        sucess = true;

                        uow._context.WaifuUpdates.Add(new WaifuUpdate()
                        {
                            User = thisUser,
                            Old = null,
                            New = newAff,
                            UpdateType = WaifuUpdateType.AffinityChanged
                        });
                    }
                    else
                    {
                        if (w.Affinity != null)
                            oldAff = w.Affinity;
                        w.Affinity = newAff;
                        sucess = true;

                        uow._context.WaifuUpdates.Add(new WaifuUpdate()
                        {
                            User = w.Waifu,
                            Old = oldAff,
                            New = newAff,
                            UpdateType = WaifuUpdateType.AffinityChanged
                        });
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (!sucess)
                {
                    if (remaining != null)
                    {
                        await ReplyErrorLocalized("waifu_affinity_cooldown", 
                            Format.Bold(((int)remaining?.TotalHours).ToString()),
                            Format.Bold(remaining?.Minutes.ToString())).ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyErrorLocalized("waifu_affinity_already").ConfigureAwait(false);
                    }
                    return;
                }
                if (u == null)
                {
                    await ReplyConfirmLocalized("waifu_affinity_reset").ConfigureAwait(false);
                }
                else if (oldAff == null)
                {
                    await ReplyConfirmLocalized("waifu_affinity_set", Format.Bold(u.ToString())).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("waifu_affinity_changed", Format.Bold(oldAff.ToString()), Format.Bold(u.ToString())).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuLeaderboard(int page = 1)
            {
                page--;

                if (page < 0)
                    return;

                IList<WaifuInfo> waifus;
                using (var uow = _db.UnitOfWork)
                {
                    waifus = uow.Waifus.GetTop(9, page * 9);
                }

                if (waifus.Count == 0)
                {
                    await ReplyConfirmLocalized("waifus_none").ConfigureAwait(false);
                    return;
                }
                
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("waifus_top_waifus"))
                    .WithOkColor();

                for (var i = 0; i < waifus.Count; i++)
                {
                    var w = waifus[i];

                    var j = i;
                    embed.AddField(efb => efb.WithName("#" + ((page * 9) + j + 1) + " - " + w.Price + _bc.BotConfig.CurrencySign).WithValue(w.ToString()).WithIsInline(false));
                }

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuInfo([Remainder]IGuildUser target = null)
            {
                if (target == null)
                    target = (IGuildUser)Context.User;
                WaifuInfo w;
                IList<WaifuInfo> claims;
                int divorces;
                using (var uow = _db.UnitOfWork)
                {
                    w = uow.Waifus.ByWaifuUserId(target.Id);
                    claims = uow.Waifus.ByClaimerUserId(target.Id);
                    divorces = uow._context.WaifuUpdates.Count(x => x.Old != null &&
                        x.Old.UserId == target.Id &&
                        x.UpdateType == WaifuUpdateType.Claimed &&
                        x.New == null);
                    if (w == null)
                    {
                        uow.Waifus.Add(w = new WaifuInfo()
                        {
                            Affinity = null,
                            Claimer = null,
                            Price = 1,
                            Waifu = uow.DiscordUsers.GetOrCreate(target),
                        });
                    }

                    w.Waifu.Username = target.Username;
                    w.Waifu.Discriminator = target.Discriminator;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                var claimInfo = GetClaimTitle(target.Id);
                var affInfo = GetAffinityTitle(target.Id);

                var rng = new NadekoRandom();

                var nobody = GetText("nobody");
                var i = 0;
                var itemsStr = !w.Items.Any()
                    ? "-"
                    : string.Join("\n", w.Items
                        .OrderBy(x => x.Price)
                        .GroupBy(x => x.ItemEmoji)
                        .Select(x => $"{x.Key} x{x.Count(),-3}")
                        .GroupBy(x => i++ / 2)
                        .Select(x => string.Join(" ", x)));


                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("Waifu " + w.Waifu + " - \"the " + claimInfo.Title + "\"")
                    .AddField(efb => efb.WithName(GetText("price")).WithValue(w.Price.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("claimed_by")).WithValue(w.Claimer?.ToString() ?? nobody).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("likes")).WithValue(w.Affinity?.ToString() ?? nobody).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("changes_of_heart")).WithValue($"{affInfo.Count} - \"the {affInfo.Title}\"").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("divorces")).WithValue(divorces.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("gifts")).WithValue(itemsStr).WithIsInline(false))
                    .AddField(efb => efb.WithName($"Waifus ({claims.Count})").WithValue(claims.Count == 0 ? nobody : string.Join("\n", claims.OrderBy(x => rng.Next()).Take(30).Select(x => x.Waifu))).WithIsInline(false));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task WaifuGift(int page = 1)
            {
                if (--page < 0 || page > 3)
                    return;

                await Context.SendPaginatedConfirmAsync(page, (cur) =>
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("waifu_gift_shop"))
                        .WithOkColor();

                    Enum.GetValues(typeof(WaifuItem.ItemName))
                                        .Cast<WaifuItem.ItemName>()
                                        .Select(x => WaifuItem.GetItem(x, _bc.BotConfig.WaifuGiftMultiplier))
                                        .OrderBy(x => x.Price)
                                        .Skip(9 * cur)
                                        .Take(9)
                                        .ForEach(x => embed.AddField(f => f.WithName(x.ItemEmoji + " " + x.Item).WithValue(x.Price).WithIsInline(true)));

                    return embed;
                }, Enum.GetValues(typeof(WaifuItem.ItemName)).Length, 9);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task WaifuGift(WaifuItem.ItemName item, [Remainder] IUser waifu)
            {
                if (waifu.Id == Context.User.Id)
                    return;

                var itemObj = WaifuItem.GetItem(item, _bc.BotConfig.WaifuGiftMultiplier);

                using (var uow = _db.UnitOfWork)
                {
                    var w = uow.Waifus.ByWaifuUserId(waifu.Id);

                    //try to buy the item first

                    if (!await _cs.RemoveAsync(Context.User.Id, "Bought waifu item", itemObj.Price, gamble: true))
                    {
                        await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                    if (w == null)
                    {
                        uow.Waifus.Add(w = new WaifuInfo()
                        {
                            Affinity = null,
                            Claimer = null,
                            Price = 1,
                            Waifu = uow.DiscordUsers.GetOrCreate(waifu),
                        });

                        w.Waifu.Username = waifu.Username;
                        w.Waifu.Discriminator = waifu.Discriminator;
                    }
                    w.Items.Add(itemObj);
                    if (w.Claimer?.UserId == Context.User.Id)
                    {
                        w.Price += itemObj.Price;
                    }
                    else
                        w.Price += itemObj.Price / 2;

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ReplyConfirmLocalized("waifu_gift", Format.Bold(item.ToString() + " " +itemObj.ItemEmoji), Format.Bold(waifu.ToString())).ConfigureAwait(false);
            }


            public struct WaifuProfileTitle
            {
                public int Count { get; }
                public string Title { get; }

                public WaifuProfileTitle(int count, string title)
                {
                    Count = count;
                    Title = title;
                }
            }

            private WaifuProfileTitle GetClaimTitle(ulong userId)
            {
                int count;
                using (var uow = _db.UnitOfWork)
                {
                    count = uow.Waifus.ByClaimerUserId(userId).Count;
                }

                ClaimTitles title;
                if (count == 0)
                    title = ClaimTitles.Lonely;
                else if (count == 1)
                    title = ClaimTitles.Devoted;
                else if (count < 3)
                    title = ClaimTitles.Rookie;
                else if (count < 6)
                    title = ClaimTitles.Schemer;
                else if (count < 10)
                    title = ClaimTitles.Dilettante;
                else if (count < 17)
                    title = ClaimTitles.Intermediate;
                else if (count < 25)
                    title = ClaimTitles.Seducer;
                else if (count < 35)
                    title = ClaimTitles.Expert;
                else if (count < 50)
                    title = ClaimTitles.Veteran;
                else if (count < 75)
                    title = ClaimTitles.Incubis;
                else if (count < 100)
                    title = ClaimTitles.Harem_King;
                else
                    title = ClaimTitles.Harem_God;

                return new WaifuProfileTitle(count, title.ToString().Replace('_', ' '));
            }

            private WaifuProfileTitle GetAffinityTitle(ulong userId)
            {
                int count;
                using (var uow = _db.UnitOfWork)
                {
                    count = uow._context.WaifuUpdates
                        .Where(w => w.User.UserId == userId && w.UpdateType == WaifuUpdateType.AffinityChanged && w.New != null)
                        .GroupBy(x => x.New)
                        .Count();
                }

                AffinityTitles title;
                if (count < 1)
                    title = AffinityTitles.Pure;
                else if (count < 2)
                    title = AffinityTitles.Faithful;
                else if (count < 4)
                    title = AffinityTitles.Defiled;
                else if (count < 9)
                    title = AffinityTitles.Cheater;
                else if (count < 12)
                    title = AffinityTitles.Tainted;
                else if (count < 16)
                    title = AffinityTitles.Corrupted;
                else if (count < 20)
                    title = AffinityTitles.Lewd;
                else if (count < 25)
                    title = AffinityTitles.Sloot;
                else if (count < 35)
                    title = AffinityTitles.Depraved;
                else
                    title = AffinityTitles.Harlot;

                return new WaifuProfileTitle(count, title.ToString().Replace('_', ' '));
            }
        }
    }
}