//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
//using Discord.Commands;
//using NadekoBot.Attributes;
//using NadekoBot.Modules.Utility.Models;
//using Newtonsoft.Json;
//using System.Threading;
//using System;
//using System.Collections.Immutable;

//namespace NadekoBot.Modules.Utility
//{
//    public partial class Utility
//    {
//        [Group]
//        public class PatreonCommands : NadekoSubmodule
//        {
//            [NadekoCommand, Usage, Description, Aliases]
//            public async Task ClaimPatreonRewards()
//            {
//                var patreon = PatreonThingy.Instance;

//                var pledges = (await patreon.GetPledges().ConfigureAwait(false))
//                    .OrderByDescending(x => x.Reward.attributes.amount_cents);

//                if (pledges == null)
//                {
//                    await ReplyErrorLocalized("pledges_loading").ConfigureAwait(false);
//                    return;
//                }

//            }
//        }

//        public class PatreonThingy
//        {
//            public static PatreonThingy _instance = new PatreonThingy();
//            public static PatreonThingy Instance => _instance;

//            private readonly SemaphoreSlim getPledgesLocker = new SemaphoreSlim(1, 1);

//            private ImmutableArray<PatreonUserAndReward> pledges;

//            static PatreonThingy() { }

//            public async Task<ImmutableArray<PatreonUserAndReward>> GetPledges()
//            {
//                try
//                {
//                    await LoadPledges().ConfigureAwait(false);
//                    return pledges;
//                }
//                catch (OperationCanceledException)
//                {
//                    return pledges;
//                }
//            }

//            public async Task LoadPledges()
//            {
//                await getPledgesLocker.WaitAsync(1000).ConfigureAwait(false);
//                try
//                {
//                    var rewards = new List<PatreonPledge>();
//                    var users = new List<PatreonUser>();
//                    using (var http = new HttpClient())
//                    {
//                        http.DefaultRequestHeaders.Clear();
//                        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + NadekoBot.Credentials.PatreonAccessToken);
//                        var data = new PatreonData()
//                        {
//                            Links = new PatreonDataLinks()
//                            {
//                                next = "https://api.patreon.com/oauth2/api/campaigns/334038/pledges"
//                            }
//                        };
//                        do
//                        {
//                            var res = await http.GetStringAsync(data.Links.next)
//                                .ConfigureAwait(false);
//                            data = JsonConvert.DeserializeObject<PatreonData>(res);
//                            var pledgers = data.Data.Where(x => x["type"].ToString() == "pledge");
//                            rewards.AddRange(pledgers.Select(x => JsonConvert.DeserializeObject<PatreonPledge>(x.ToString()))
//                                .Where(x => x.attributes.declined_since == null));
//                            users.AddRange(data.Included
//                                .Where(x => x["type"].ToString() == "user")
//                                .Select(x => JsonConvert.DeserializeObject<PatreonUser>(x.ToString())));
//                        } while (!string.IsNullOrWhiteSpace(data.Links.next));
//                    }
//                    pledges = rewards.Join(users, (r) => r.relationships?.patron?.data?.id, (u) => u.id, (x, y) => new PatreonUserAndReward()
//                    {
//                        User = y,
//                        Reward = x,
//                    }).ToImmutableArray();
//                }
//                finally
//                {
//                    var _ = Task.Run(async () =>
//                    {
//                        await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
//                        getPledgesLocker.Release();
//                    });
                    
//                }
//            }
//        }
//    }
//}
