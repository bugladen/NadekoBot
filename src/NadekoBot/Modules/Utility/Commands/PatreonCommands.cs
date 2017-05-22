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
            //todo rename patreon thingy and move it to be a service, or a part of utility service
            private readonly PatreonThingy patreon;
            private readonly IBotCredentials _creds;
            private readonly BotConfig _config;
            private readonly DbHandler _db;
            private readonly CurrencyHandler _currency;

            public PatreonCommands(IBotCredentials creds, BotConfig config, DbHandler db, CurrencyHandler currency)
            {
                _creds = creds;
                _config = config;
                _db = db;
                _currency = currency;
                patreon = PatreonThingy.GetInstance(creds, db, currency);                
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task PatreonRewardsReload()
            {
                await patreon.LoadPledges().ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("👌").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task ClaimPatreonRewards()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
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
                    await ReplyConfirmLocalized("clpa_success", amount + _config.CurrencySign).ConfigureAwait(false);
                    return;
                }
                var rem = (patreon.Interval - (DateTime.UtcNow - patreon.LastUpdate));
                var helpcmd = Format.Code(NadekoBot.Prefix + "donate");
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(GetText("clpa_fail"))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_already_title")).WithValue(GetText("clpa_fail_already")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_wait_title")).WithValue(GetText("clpa_fail_wait")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_conn_title")).WithValue(GetText("clpa_fail_conn")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_sup_title")).WithValue(GetText("clpa_fail_sup", helpcmd)))
                    .WithFooter(efb => efb.WithText(GetText("clpa_next_update", rem))))
                    .ConfigureAwait(false);
            }
        }

        public class PatreonThingy
        {
            //todo quickly hacked while rewriting, fix this
            private static PatreonThingy _instance = null;
            public static PatreonThingy GetInstance(IBotCredentials creds, DbHandler db, CurrencyHandler cur) 
                => _instance ?? (_instance = new PatreonThingy(creds, db, cur));

            private readonly SemaphoreSlim getPledgesLocker = new SemaphoreSlim(1, 1);

            public ImmutableArray<PatreonUserAndReward> Pledges { get; private set; }
            public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

            public readonly Timer Updater;
            private readonly SemaphoreSlim claimLockJustInCase = new SemaphoreSlim(1, 1);
            private readonly Logger _log;

            public readonly TimeSpan Interval = TimeSpan.FromHours(1);
            private IBotCredentials _creds;
            private readonly DbHandler _db;
            private readonly CurrencyHandler _currency;

            static PatreonThingy() { }
            private PatreonThingy(IBotCredentials creds, DbHandler db, CurrencyHandler currency)
            {
                _creds = creds;
                _db = db;
                _currency = currency;
                if (string.IsNullOrWhiteSpace(creds.PatreonAccessToken))
                    return;
                _log = LogManager.GetCurrentClassLogger();
                Updater = new Timer(async (_) => await LoadPledges(), null, TimeSpan.Zero, Interval);
            }

            public async Task LoadPledges()
            {
                LastUpdate = DateTime.UtcNow;
                await getPledgesLocker.WaitAsync(1000).ConfigureAwait(false);
                try
                {
                    var rewards = new List<PatreonPledge>();
                    var users = new List<PatreonUser>();
                    using (var http = new HttpClient())
                    {
                        http.DefaultRequestHeaders.Clear();
                        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _creds.PatreonAccessToken);
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

                    using (var uow = _db.UnitOfWork)
                    {
                        var users = uow._context.Set<RewardedUser>();
                        var usr = users.FirstOrDefault(x => x.PatreonUserId == data.User.id);

                        if (usr == null)
                        {
                            users.Add(new RewardedUser()
                            {
                                UserId = userId,
                                PatreonUserId = data.User.id,
                                LastReward = now,
                                AmountRewardedThisMonth = amount,
                            });

                            await _currency.AddCurrencyAsync(userId, "Patreon reward - new", amount, uow).ConfigureAwait(false);

                            await uow.CompleteAsync().ConfigureAwait(false);
                            return amount;
                        }

                        if (usr.LastReward.Month != now.Month)
                        {
                            usr.LastReward = now;
                            usr.AmountRewardedThisMonth = amount;
                            usr.PatreonUserId = data.User.id;

                            await _currency.AddCurrencyAsync(userId, "Patreon reward - recurring", amount, uow).ConfigureAwait(false);

                            await uow.CompleteAsync().ConfigureAwait(false);
                            return amount;
                        }

                        if ( usr.AmountRewardedThisMonth < amount)
                        {
                            var toAward = amount - usr.AmountRewardedThisMonth;

                            usr.LastReward = now;
                            usr.AmountRewardedThisMonth = amount;
                            usr.PatreonUserId = data.User.id;

                            await _currency.AddCurrencyAsync(usr.UserId, "Patreon reward - update", toAward, uow).ConfigureAwait(false);

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