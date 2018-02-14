using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Modules.Utility.Common.Patreon;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using Newtonsoft.Json;
using NLog;
using NadekoBot.Extensions;
using NadekoBot.Core.Common.Caching;

namespace NadekoBot.Modules.Utility.Services
{
    public class PatreonRewardsService : INService, IUnloadableService
    {
        private readonly SemaphoreSlim getPledgesLocker = new SemaphoreSlim(1, 1);

        private readonly FactoryCache<PatreonUserAndReward[]> _pledges;
        public PatreonUserAndReward[] Pledges => _pledges.GetValue();

        public readonly Timer Updater;
        private readonly SemaphoreSlim claimLockJustInCase = new SemaphoreSlim(1, 1);
        private readonly Logger _log;

        public readonly TimeSpan Interval = TimeSpan.FromMinutes(3);
        private readonly IBotCredentials _creds;
        private readonly DbService _db;
        private readonly ICurrencyService _currency;
        private readonly IDataCache _cache;
        private readonly string _key;
        private readonly IBotConfigProvider _bc;

        public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

        public PatreonRewardsService(IBotCredentials creds, DbService db, 
            ICurrencyService currency,
            DiscordSocketClient client, IDataCache cache, IBotConfigProvider bc)
        {
            _log = LogManager.GetCurrentClassLogger();
            _creds = creds;
            _db = db;
            _currency = currency;
            _cache = cache;
            _key = _creds.RedisKey() + "_patreon_rewards";
            _bc = bc;
            
            _pledges = new FactoryCache<PatreonUserAndReward[]>(() =>
            {
                var r = _cache.Redis.GetDatabase();
                var data = r.StringGet(_key);
                if (data.IsNullOrEmpty)
                    return null;
                else
                {
                    return JsonConvert.DeserializeObject<PatreonUserAndReward[]>(data);
                }
            }, TimeSpan.FromSeconds(20));

            if(client.ShardId == 0)
                Updater = new Timer(async _ => await RefreshPledges(),
                    null, TimeSpan.Zero, Interval);
        }

        public async Task RefreshPledges()
        {
            if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                return;

            LastUpdate = DateTime.UtcNow;
            await getPledgesLocker.WaitAsync().ConfigureAwait(false);
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
                            next = $"https://api.patreon.com/oauth2/api/campaigns/{_creds.PatreonCampaignId}/pledges"
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
                        if (data.Included != null)
                        {
                            users.AddRange(data.Included
                                .Where(x => x["type"].ToString() == "user")
                                .Select(x => JsonConvert.DeserializeObject<PatreonUser>(x.ToString())));
                        }
                    } while (!string.IsNullOrWhiteSpace(data.Links.next));
                }
                var db = _cache.Redis.GetDatabase();
                var toSet = JsonConvert.SerializeObject(rewards.Join(users, (r) => r.relationships?.patron?.data?.id, (u) => u.id, (x, y) => new PatreonUserAndReward()
                {
                    User = y,
                    Reward = x,
                }).ToArray());

                db.StringSet(_key, toSet);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
            finally
            {
                getPledgesLocker.Release();
            }
            
        }

        public async Task<int> ClaimReward(ulong userId)
        {
            await claimLockJustInCase.WaitAsync();
            var now = DateTime.UtcNow;
            try
            {
                var data = Pledges?.FirstOrDefault(x => x.User.attributes?.social_connections?.discord?.user_id == userId.ToString());

                if (data == null)
                    return 0;

                var amount = (int)(data.Reward.attributes.amount_cents * _bc.BotConfig.PatreonCurrencyPerCent);

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

                        await _currency.AddAsync(userId, "Patreon reward - new", amount, gamble: true).ConfigureAwait(false);

                        await uow.CompleteAsync().ConfigureAwait(false);
                        return amount;
                    }

                    if (usr.LastReward.Month != now.Month)
                    {
                        usr.LastReward = now;
                        usr.AmountRewardedThisMonth = amount;
                        usr.PatreonUserId = data.User.id;

                        await _currency.AddAsync(userId, "Patreon reward - recurring", amount, gamble: true).ConfigureAwait(false);

                        await uow.CompleteAsync().ConfigureAwait(false);
                        return amount;
                    }

                    if (usr.AmountRewardedThisMonth < amount)
                    {
                        var toAward = amount - usr.AmountRewardedThisMonth;

                        usr.LastReward = now;
                        usr.AmountRewardedThisMonth = amount;
                        usr.PatreonUserId = data.User.id;

                        await _currency.AddAsync(usr.UserId, "Patreon reward - update", toAward, gamble: true).ConfigureAwait(false);

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

        public Task Unload()
        {
            Updater?.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }
    }
}
