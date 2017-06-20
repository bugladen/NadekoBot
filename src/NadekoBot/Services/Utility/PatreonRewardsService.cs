using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Utility.Patreon;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Utility
{
    //todo periodically load from the database, update only on shard 0
    public class PatreonRewardsService
    {
        private readonly SemaphoreSlim getPledgesLocker = new SemaphoreSlim(1, 1);

        public ImmutableArray<PatreonUserAndReward> Pledges { get; private set; }
        public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

        public readonly Timer Updater;
        private readonly SemaphoreSlim claimLockJustInCase = new SemaphoreSlim(1, 1);
        private readonly Logger _log;

        public readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
        private IBotCredentials _creds;
        private readonly DbService _db;
        private readonly CurrencyService _currency;

        public PatreonRewardsService(IBotCredentials creds, DbService db, CurrencyService currency)
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

                        await _currency.AddAsync(userId, "Patreon reward - new", amount, uow).ConfigureAwait(false);

                        await uow.CompleteAsync().ConfigureAwait(false);
                        return amount;
                    }

                    if (usr.LastReward.Month != now.Month)
                    {
                        usr.LastReward = now;
                        usr.AmountRewardedThisMonth = amount;
                        usr.PatreonUserId = data.User.id;

                        await _currency.AddAsync(userId, "Patreon reward - recurring", amount, uow).ConfigureAwait(false);

                        await uow.CompleteAsync().ConfigureAwait(false);
                        return amount;
                    }

                    if (usr.AmountRewardedThisMonth < amount)
                    {
                        var toAward = amount - usr.AmountRewardedThisMonth;

                        usr.LastReward = now;
                        usr.AmountRewardedThisMonth = amount;
                        usr.PatreonUserId = data.User.id;

                        await _currency.AddAsync(usr.UserId, "Patreon reward - update", toAward, uow).ConfigureAwait(false);

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
