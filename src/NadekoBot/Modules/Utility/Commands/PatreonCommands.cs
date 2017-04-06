using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Modules.Utility.Models;
using Newtonsoft.Json;
using System.Threading;
using System;
using System.Collections.Immutable;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;
using Discord;
using NLog;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class PatreonCommands : NadekoSubmodule
        {
            private static readonly PatreonThingy patreon;

            static PatreonCommands()
            {
                patreon = PatreonThingy.Instance;
            }
            [NadekoCommand, Usage, Description, Aliases]
            public async Task ClaimPatreonRewards()
            {
                if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.PatreonAccessToken))
                    return;
                if (DateTime.UtcNow.Day < 5)
                {
                    await ReplyErrorLocalized("clpa_too_early").ConfigureAwait(false);
                    return;
                }
                int amount = 0;
                try
                {
                    amount = await patreon.ClaimReward(Context.User.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }

                if (amount > 0)
                {
                    await ReplyConfirmLocalized("clpa_success", amount + NadekoBot.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }
                var helpcmd = Format.Code(NadekoBot.ModulePrefixes[typeof(Help.Help).Name] + "donate");
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(GetText("clpa_fail"))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_already_title")).WithValue(GetText("clpa_fail_already")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_wait_title")).WithValue(GetText("clpa_fail_wait")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_conn_title")).WithValue(GetText("clpa_fail_conn")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_sup_title")).WithValue(GetText("clpa_fail_sup", helpcmd))))
                    .ConfigureAwait(false);
            }
        }

        public class PatreonThingy
        {
            public static PatreonThingy _instance = new PatreonThingy();
            public static PatreonThingy Instance => _instance;

            private readonly SemaphoreSlim getPledgesLocker = new SemaphoreSlim(1, 1);

            public ImmutableArray<PatreonUserAndReward> Pledges { get; private set; }

            private readonly Timer update;
            private readonly SemaphoreSlim claimLockJustInCase = new SemaphoreSlim(1, 1);
            private readonly Logger _log;

            private PatreonThingy()
            {
                if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.PatreonAccessToken))
                    return;
                _log = LogManager.GetCurrentClassLogger();
                update = new Timer(async (_) => await LoadPledges(), null, TimeSpan.Zero, TimeSpan.FromHours(3));
            }

            public async Task LoadPledges()
            {
                await getPledgesLocker.WaitAsync(1000).ConfigureAwait(false);
                try
                {
                    var rewards = new List<PatreonPledge>();
                    var users = new List<PatreonUser>();
                    using (var http = new HttpClient())
                    {
                        http.DefaultRequestHeaders.Clear();
                        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + NadekoBot.Credentials.PatreonAccessToken);
                        var data = new PatreonData()
                        {
                            Links = new PatreonDataLinks()
                            {
                                next = "https://api.patreon.com/oauth2/api/campaigns/334038/pledges"
                            }
                        };
                        do
                        {
                            var res = await http.GetStringAsync(data.Links.next)
                                .ConfigureAwait(false);
                            data = JsonConvert.DeserializeObject<PatreonData>(res);
                            var pledgers = data.Data.Where(x => x["type"].ToString() == "pledge");
                            rewards.AddRange(pledgers.Select(x => JsonConvert.DeserializeObject<PatreonPledge>(x.ToString()))
                                .Where(x => x.attributes.declined_since == null));
                            users.AddRange(data.Included
                                .Where(x => x["type"].ToString() == "user")
                                .Select(x => JsonConvert.DeserializeObject<PatreonUser>(x.ToString())));
                        } while (!string.IsNullOrWhiteSpace(data.Links.next));
                    }
                    Pledges = rewards.Join(users, (r) => r.relationships?.patron?.data?.id, (u) => u.id, (x, y) => new PatreonUserAndReward()
                    {
                        User = y,
                        Reward = x,
                    }).ToImmutableArray();
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
                finally
                {
                    var _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                        getPledgesLocker.Release();
                    });
                }
            }

            public async Task<int> ClaimReward(ulong userId)
            {
                await claimLockJustInCase.WaitAsync();
                var now = DateTime.UtcNow;
                try
                {
                    var data = Pledges.FirstOrDefault(x => x.User.attributes?.social_connections?.discord?.user_id == userId.ToString());

                    if (data == null)
                        return 0;

                    var amount = data.Reward.attributes.amount_cents;

                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var users = uow._context.Set<RewardedUser>();
                        var usr = users.FirstOrDefault(x => x.UserId == userId);

                        if (usr == null)
                        {
                            users.Add(new RewardedUser()
                            {
                                UserId = userId,
                                LastReward = now,
                                AmountRewardedThisMonth = amount,
                            });

                            await CurrencyHandler.AddCurrencyAsync(userId, "Patreon reward - new", amount, uow).ConfigureAwait(false);

                            await uow.CompleteAsync().ConfigureAwait(false);
                            return amount;
                        }

                        if (usr.LastReward.Month != now.Month)
                        {
                            usr.LastReward = now;
                            usr.AmountRewardedThisMonth = amount;

                            await CurrencyHandler.AddCurrencyAsync(userId, "Patreon reward - recurring", amount, uow).ConfigureAwait(false);

                            await uow.CompleteAsync().ConfigureAwait(false);
                            return amount;
                        }

                        if ( usr.AmountRewardedThisMonth < amount)
                        {
                            var toAward = amount - usr.AmountRewardedThisMonth;

                            usr.LastReward = now;
                            usr.AmountRewardedThisMonth = amount;

                            await CurrencyHandler.AddCurrencyAsync(usr.UserId, "Patreon reward - update", toAward, uow).ConfigureAwait(false);

                            await uow.CompleteAsync().ConfigureAwait(false);
                            return toAward;
                        }
                    }
                    return 0;
                }
                finally
                {
                    claimLockJustInCase.Release();
                }
            }
        }
    }
}