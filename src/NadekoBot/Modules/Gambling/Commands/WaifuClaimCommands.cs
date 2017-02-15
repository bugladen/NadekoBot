using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public class WaifuClaimCommands : NadekoSubmodule
        {
            private static ConcurrentDictionary<ulong, DateTime> _divorceCooldowns { get; } = new ConcurrentDictionary<ulong, DateTime>();
            private static ConcurrentDictionary<ulong, DateTime> _affinityCooldowns { get; } = new ConcurrentDictionary<ulong, DateTime>();

            enum WaifuClaimResult
            {
                Success,
                NotEnoughFunds,
                InsufficientAmount
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuClaim(int amount, [Remainder]IUser target)
            {
                if (amount < 50)
                {
                    await Context.Channel.SendErrorAsync($"{Context.User.Mention} No waifu is that cheap. You must pay at least 50{NadekoBot.BotConfig.CurrencySign} to get a waifu, even if their actual value is lower.").ConfigureAwait(false);
                    return;
                }

                if (target.Id == Context.User.Id)
                {
                    await Context.Channel.SendErrorAsync(Context.User.Mention + " You can't claim yourself.").ConfigureAwait(false);
                    return;
                }

                WaifuClaimResult result = WaifuClaimResult.NotEnoughFunds;
                int? oldPrice = null;
                WaifuInfo w;
                var isAffinity = false;
                using (var uow = DbHandler.UnitOfWork())
                {
                    w = uow.Waifus.ByWaifuUserId(target.Id);
                    isAffinity = (w?.Affinity?.UserId == Context.User.Id);
                    if (w == null)
                    {
                        var claimer = uow.DiscordUsers.GetOrCreate(Context.User);
                        var waifu = uow.DiscordUsers.GetOrCreate(target);
                        if (!await CurrencyHandler.RemoveCurrencyAsync(Context.User.Id, "Claimed Waifu", amount, uow).ConfigureAwait(false))
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
                        if (!await CurrencyHandler.RemoveCurrencyAsync(Context.User.Id, "Claimed Waifu", amount, uow).ConfigureAwait(false))
                        {
                            result = WaifuClaimResult.NotEnoughFunds;
                        }
                        else
                        {
                            var oldClaimer = w.Claimer;
                            w.Claimer = uow.DiscordUsers.GetOrCreate(Context.User);
                            oldPrice = w.Price;
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
                        if (!await CurrencyHandler.RemoveCurrencyAsync(Context.User.Id, "Claimed Waifu", amount, uow).ConfigureAwait(false))
                        {
                            result = WaifuClaimResult.NotEnoughFunds;
                        }
                        else
                        {
                            var oldClaimer = w.Claimer;
                            w.Claimer = uow.DiscordUsers.GetOrCreate(Context.User);
                            oldPrice = w.Price;
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
                    await Context.Channel.SendErrorAsync($"{Context.User.Mention} You must pay {Math.Ceiling(w.Price * (isAffinity ? 0.88f : 1.1f))} or more to claim that waifu!").ConfigureAwait(false);
                    return;
                }
                else if (result == WaifuClaimResult.NotEnoughFunds)
                {
                    await Context.Channel.SendConfirmAsync($"{Context.User.Mention} you don't have {amount}{NadekoBot.BotConfig.CurrencySign}!")
                            .ConfigureAwait(false);
                }
                else
                {
                    var msg = $"{Context.User.Mention} claimed {target.Mention} as their waifu for {amount}{NadekoBot.BotConfig.CurrencySign}!";
                    if (w.Affinity?.UserId == Context.User.Id)
                        msg += $"\n🎉 Their love is fulfilled! 🎉\n**{target}'s** new value is {w.Price}{NadekoBot.BotConfig.CurrencySign}!";
                    await Context.Channel.SendConfirmAsync(msg)
                            .ConfigureAwait(false);
                }
            }

            public enum DivorceResult
            {
                Success,
                SucessWithPenalty,
                NotYourWife,
                Cooldown
            }


            private static readonly TimeSpan DivorceLimit = TimeSpan.FromHours(6);
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Divorce([Remainder]IUser target)
            {
                if (target.Id == Context.User.Id)
                    return;

                var result = DivorceResult.NotYourWife;
                TimeSpan difference = TimeSpan.Zero;
                var amount = 0;
                WaifuInfo w = null;
                using (var uow = DbHandler.UnitOfWork())
                {
                    w = uow.Waifus.ByWaifuUserId(target.Id);
                    var now = DateTime.UtcNow;
                    if (w == null || w.Claimer == null || w.Claimer.UserId != Context.User.Id)
                        result = DivorceResult.NotYourWife;
                    else if (_divorceCooldowns.AddOrUpdate(Context.User.Id,
                        now,
                        (key, old) => ((difference = now.Subtract(old)) > DivorceLimit) ? now : old) != now)
                    {
                        result = DivorceResult.Cooldown;
                    }
                    else
                    {
                        amount = w.Price / 2;

                        if (w.Affinity?.UserId == Context.User.Id)
                        {
                            await CurrencyHandler.AddCurrencyAsync(w.Waifu.UserId, "Waifu Compensation", amount, uow).ConfigureAwait(false);
                            w.Price = (int)Math.Floor(w.Price * 0.75f);
                            result = DivorceResult.SucessWithPenalty;
                        }
                        else
                        {
                            await CurrencyHandler.AddCurrencyAsync(Context.User.Id, "Waifu Refund", amount, uow).ConfigureAwait(false);

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
                    await Context.Channel.SendConfirmAsync($"{Context.User.Mention} You have divorced a waifu who likes you. You heartless monster.\n{w.Waifu} received {amount}{NadekoBot.BotConfig.CurrencySign} as a compensation.").ConfigureAwait(false);
                }
                else if (result == DivorceResult.Success)
                {
                    await Context.Channel.SendConfirmAsync($"{Context.User.Mention} You have divorced a waifu who doesn't like you. You received {amount}{NadekoBot.BotConfig.CurrencySign} back.").ConfigureAwait(false);
                }
                else if (result == DivorceResult.NotYourWife)
                {
                    await Context.Channel.SendErrorAsync($"{Context.User.Mention} That waifu is not yours.").ConfigureAwait(false);
                }
                else
                {
                    var remaining = DivorceLimit.Subtract(difference);
                    await Context.Channel.SendErrorAsync($"{Context.User.Mention} You divorced recently. You must wait **{remaining.Hours} hours and {remaining.Minutes} minutes** to divorce again.").ConfigureAwait(false);
                }
            }

            private static readonly TimeSpan AffinityLimit = TimeSpan.FromMinutes(30);
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuClaimerAffinity([Remainder]IUser u = null)
            {
                if (u?.Id == Context.User.Id)
                {
                    await Context.Channel.SendErrorAsync($"{Context.User.Mention} you can't set affinity to yourself, you egomaniac.").ConfigureAwait(false);
                    return;
                }
                DiscordUser oldAff = null;
                var sucess = false;
                var cooldown = false;
                TimeSpan difference = TimeSpan.Zero;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var w = uow.Waifus.ByWaifuUserId(Context.User.Id);
                    var newAff = u == null ? null : uow.DiscordUsers.GetOrCreate(u);
                    var now = DateTime.UtcNow;
                    if (w?.Affinity?.UserId == u?.Id)
                    {
                        sucess = false;
                    }
                    else if (_affinityCooldowns.AddOrUpdate(Context.User.Id,
                        now,
                        (key, old) => ((difference = now.Subtract(old)) > AffinityLimit) ? now : old) != now)
                    {
                        sucess = false;
                        cooldown = true;
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
                    if (cooldown)
                    {
                        var remaining = AffinityLimit.Subtract(difference);
                        await Context.Channel.SendErrorAsync($"{Context.User.Mention} You must wait **{remaining.Hours} hours and {remaining.Minutes} minutes** in order to change your affinity again.").ConfigureAwait(false);
                    }
                    else
                        await Context.Channel.SendErrorAsync($"{Context.User.Mention} your affinity is already set to that waifu or you're trying to remove your affinity while not having one.").ConfigureAwait(false);
                    return;
                }
                if (u == null)
                    await Context.Channel.SendConfirmAsync("Affinity Reset", $"{Context.User.Mention} Your affinity is reset. You no longer have a person you like.").ConfigureAwait(false);
                else if (oldAff == null)
                    await Context.Channel.SendConfirmAsync("Affinity Set", $"{Context.User.Mention} wants to be {u.Mention}'s waifu. Aww <3").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("Affinity Changed", $"{Context.User.Mention} changed their affinity from {oldAff} to {u.Mention}.\n\n*This is morally questionable.*🤔").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuLeaderboard()
            {
                IList<WaifuInfo> waifus;
                using (var uow = DbHandler.UnitOfWork())
                {
                    waifus = uow.Waifus.GetTop(9);
                }

                if (waifus.Count == 0)
                {
                    await Context.Channel.SendConfirmAsync("No waifus have been claimed yet.").ConfigureAwait(false);
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle("Top Waifus")
                    .WithOkColor();

                for (int i = 0; i < waifus.Count; i++)
                {
                    var w = waifus[i];

                    embed.AddField(efb => efb.WithName("#" + (i + 1) + " - " + w.Price + NadekoBot.BotConfig.CurrencySign).WithValue(w.ToString()).WithIsInline(false));
                }

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuInfo([Remainder]IUser target = null)
            {
                if (target == null)
                    target = Context.User;
                WaifuInfo w;
                IList<WaifuInfo> claims;
                int divorces = 0;
                using (var uow = DbHandler.UnitOfWork())
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

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("Waifu " + w.Waifu + " - \"the " + claimInfo.Title + "\"")
                    .AddField(efb => efb.WithName("Price").WithValue(w.Price.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("Claimed by").WithValue(w.Claimer?.ToString() ?? "No one").WithIsInline(true))
                    .AddField(efb => efb.WithName("Likes").WithValue(w.Affinity?.ToString() ?? "Nobody").WithIsInline(true))
                    .AddField(efb => efb.WithName("Changes Of Heart").WithValue($"{affInfo.Count} - \"the {affInfo.Title}\"").WithIsInline(true))
                    .AddField(efb => efb.WithName("Divorces").WithValue(divorces.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName($"Waifus ({claims.Count})").WithValue(claims.Count == 0 ? "Nobody" : string.Join("\n", claims.Select(x => x.Waifu))).WithIsInline(true));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
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

            private static WaifuProfileTitle GetClaimTitle(ulong userId)
            {
                int count = 0;
                using (var uow = DbHandler.UnitOfWork())
                {
                    count = uow.Waifus.ByClaimerUserId(userId).Count;
                }

                ClaimTitles title = ClaimTitles.Lonely;
                if (count == 0)
                    title = ClaimTitles.Lonely;
                else if (count == 1)
                    title = ClaimTitles.Devoted;
                else if (count < 4)
                    title = ClaimTitles.Rookie;
                else if (count < 6)
                    title = ClaimTitles.Schemer;
                else if (count < 8)
                    title = ClaimTitles.Dilettante;
                else if (count < 10)
                    title = ClaimTitles.Intermediate;
                else if (count < 12)
                    title = ClaimTitles.Seducer;
                else if (count < 15)
                    title = ClaimTitles.Expert;
                else if (count < 17)
                    title = ClaimTitles.Veteran;
                else if (count < 25)
                    title = ClaimTitles.Incubis;
                else if (count < 50)
                    title = ClaimTitles.Harem_King;
                else
                    title = ClaimTitles.Harem_God;

                return new WaifuProfileTitle(count, title.ToString().Replace('_', ' '));
            }

            private static WaifuProfileTitle GetAffinityTitle(ulong userId)
            {
                int count = 0;
                using (var uow = DbHandler.UnitOfWork())
                {
                    count = uow._context.WaifuUpdates.Count(w => w.User.UserId == userId && w.UpdateType == WaifuUpdateType.AffinityChanged);
                }

                AffinityTitles title = AffinityTitles.Pure;
                if (count < 1)
                    title = AffinityTitles.Pure;
                else if (count < 2)
                    title = AffinityTitles.Faithful;
                else if (count < 4)
                    title = AffinityTitles.Defiled;
                else if (count < 7)
                    title = AffinityTitles.Cheater;
                else if (count < 9)
                    title = AffinityTitles.Tainted;
                else if (count < 11)
                    title = AffinityTitles.Corrupted;
                else if (count < 13)
                    title = AffinityTitles.Lewd;
                else if (count < 15)
                    title = AffinityTitles.Sloot;
                else if (count < 17)
                    title = AffinityTitles.Depraved;
                else
                    title = AffinityTitles.Harlot;

                return new WaifuProfileTitle(count, title.ToString().Replace('_', ' '));
            }
        }
    }
}