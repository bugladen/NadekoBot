using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.ClashOfClans
{
    // todo, just made this compile, it's a complete mess. A lot of the things here should actually be in the actual module. 
    // service should just handle the state, module should print out what happened, so everything that has to do with strings
    // shouldn't be here
    public class ClashOfClansService
    {
        private readonly DiscordShardedClient _client;
        private readonly DbHandler _db;
        private readonly ILocalization _localization;
        private readonly NadekoStrings _strings;
        private readonly Timer checkWarTimer;

        public ConcurrentDictionary<ulong, List<ClashWar>> ClashWars { get; set; }

        public ClashOfClansService(DiscordShardedClient client, DbHandler db, ILocalization localization, NadekoStrings strings)
        {
            _client = client;
            _db = db;
            _localization = localization;
            _strings = strings;

            using (var uow = _db.UnitOfWork)
            {
                ClashWars = new ConcurrentDictionary<ulong, List<ClashWar>>(
                    uow.ClashOfClans
                        .GetAllWars()
                        .Select(cw =>
                        {
                            cw.Channel = _client.GetGuild(cw.GuildId)?
                                                         .GetTextChannel(cw.ChannelId);
                            return cw;
                        })
                        .Where(cw => cw.Channel != null)
                        .GroupBy(cw => cw.GuildId)
                        .ToDictionary(g => g.Key, g => g.ToList()));
            }

            checkWarTimer = new Timer(async _ =>
            {
                foreach (var kvp in ClashWars)
                {
                    foreach (var war in kvp.Value)
                    {
                        try { await CheckWar(TimeSpan.FromHours(2), war).ConfigureAwait(false); } catch { }
                    }
                }
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private async Task CheckWar(TimeSpan callExpire, ClashWar war)
        {
            var Bases = war.Bases;
            for (var i = 0; i < Bases.Count; i++)
            {
                var callUser = Bases[i].CallUser;
                if (callUser == null) continue;
                if ((!Bases[i].BaseDestroyed) && DateTime.UtcNow - Bases[i].TimeAdded >= callExpire)
                {
                    if (Bases[i].Stars != 3)
                        Bases[i].BaseDestroyed = true;
                    else
                        Bases[i] = null;
                    try
                    {
                        SaveWar(war);
                        await war.Channel.SendErrorAsync(_strings.GetText("claim_expired",
                                    _localization.GetCultureInfo(war.Channel.GuildId),
                                    typeof(ClashOfClansService).Name.ToLowerInvariant(),
                                    Format.Bold(Bases[i].CallUser),
                                    ShortPrint(war)));
                    }
                    catch { }
                }
            }
        }

        public Tuple<List<ClashWar>, int> GetWarInfo(IGuild guild, int num)
        {
            List<ClashWar> wars = null;
            ClashWars.TryGetValue(guild.Id, out wars);
            if (wars == null || wars.Count == 0)
            {
                return null;
            }
            // get the number of the war
            else if (num < 1 || num > wars.Count)
            {
                return null;
            }
            num -= 1;
            //get the actual war
            return new Tuple<List<ClashWar>, int>(wars, num);
        }

        public async Task<ClashWar> CreateWar(string enemyClan, int size, ulong serverId, ulong channelId)
        {
            var channel = _client.GetGuild(serverId)?.GetTextChannel(channelId);
            using (var uow = _db.UnitOfWork)
            {
                var cw = new ClashWar
                {
                    EnemyClan = enemyClan,
                    Size = size,
                    Bases = new List<ClashCaller>(size),
                    GuildId = serverId,
                    ChannelId = channelId,
                    Channel = channel,
                };
                cw.Bases.Capacity = size;
                for (int i = 0; i < size; i++)
                {
                    cw.Bases.Add(new ClashCaller()
                    {
                        CallUser = null,
                        SequenceNumber = i,
                    });
                }
                Console.WriteLine(cw.Bases.Capacity);
                uow.ClashOfClans.Add(cw);
                await uow.CompleteAsync();
                return cw;
            }
        }

        public void SaveWar(ClashWar cw)
        {
            if (cw.WarState == StateOfWar.Ended)
            {
                using (var uow = _db.UnitOfWork)
                {
                    uow.ClashOfClans.Remove(cw);
                    uow.CompleteAsync();
                }
                return;
            }

            using (var uow = _db.UnitOfWork)
            {
                uow.ClashOfClans.Update(cw);
                uow.CompleteAsync();
            }
        }

        public void Call(ClashWar cw, string u, int baseNumber)
        {
            if (baseNumber < 0 || baseNumber >= cw.Bases.Count)
                throw new ArgumentException(Localize(cw, "invalid_base_number"));
            if (cw.Bases[baseNumber].CallUser != null && cw.Bases[baseNumber].Stars == 3)
                throw new ArgumentException(Localize(cw, "base_already_claimed"));
            for (var i = 0; i < cw.Bases.Count; i++)
            {
                if (cw.Bases[i]?.BaseDestroyed == false && cw.Bases[i]?.CallUser == u)
                    throw new ArgumentException(Localize(cw, "claimed_other", u, i + 1));
            }

            var cc = cw.Bases[baseNumber];
            cc.CallUser = u.Trim();
            cc.TimeAdded = DateTime.UtcNow;
            cc.BaseDestroyed = false;
        }

        public int FinishClaim(ClashWar cw, string user, int stars = 3)
        {
            user = user.Trim();
            for (var i = 0; i < cw.Bases.Count; i++)
            {
                if (cw.Bases[i]?.BaseDestroyed != false || cw.Bases[i]?.CallUser != user) continue;
                cw.Bases[i].BaseDestroyed = true;
                cw.Bases[i].Stars = stars;
                return i;
            }
            throw new InvalidOperationException(Localize(cw, "not_partic_or_destroyed", user));
        }

        public void FinishClaim(ClashWar cw, int index, int stars = 3)
        {
            if (index < 0 || index > cw.Bases.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            var toFinish = cw.Bases[index];
            if (toFinish.BaseDestroyed != false) throw new InvalidOperationException(Localize(cw, "base_already_destroyed"));
            if (toFinish.CallUser == null) throw new InvalidOperationException(Localize(cw, "base_already_unclaimed"));
            toFinish.BaseDestroyed = true;
            toFinish.Stars = stars;
        }

        public int Uncall(ClashWar cw, string user)
        {
            user = user.Trim();
            for (var i = 0; i < cw.Bases.Count; i++)
            {
                if (cw.Bases[i]?.CallUser != user) continue;
                cw.Bases[i].CallUser = null;
                return i;
            }
            throw new InvalidOperationException(Localize(cw, "not_partic"));
        }

        public string ShortPrint(ClashWar cw) =>
            $"`{cw.EnemyClan}` ({cw.Size} v {cw.Size})";

        public string ToPrettyString(ClashWar cw)
        {
            var sb = new StringBuilder();

            if (cw.WarState == StateOfWar.Created)
                sb.AppendLine("`not started`");
            var twoHours = new TimeSpan(2, 0, 0);
            for (var i = 0; i < cw.Bases.Count; i++)
            {
                if (cw.Bases[i].CallUser == null)
                {
                    sb.AppendLine($"`{i + 1}.` ❌*{Localize(cw, "not_claimed")}*");
                }
                else
                {
                    if (cw.Bases[i].BaseDestroyed)
                    {
                        sb.AppendLine($"`{i + 1}.` ✅ `{cw.Bases[i].CallUser}` {new string('⭐', cw.Bases[i].Stars)}");
                    }
                    else
                    {
                        var left = (cw.WarState == StateOfWar.Started) ? twoHours - (DateTime.UtcNow - cw.Bases[i].TimeAdded) : twoHours;
                        if (cw.Bases[i].Stars == 3)
                        {
                            sb.AppendLine($"`{i + 1}.` ✅ `{cw.Bases[i].CallUser}` {left.Hours}h {left.Minutes}m {left.Seconds}s left");
                        }
                        else
                        {
                            sb.AppendLine($"`{i + 1}.` ✅ `{cw.Bases[i].CallUser}` {left.Hours}h {left.Minutes}m {left.Seconds}s left {new string('⭐', cw.Bases[i].Stars)} {string.Concat(Enumerable.Repeat("🔸", 3 - cw.Bases[i].Stars))}");
                        }
                    }
                }

            }
            return sb.ToString();
        }

        public string Localize(ClashWar cw, string key, params object[] replacements)
        {
            return string.Format(Localize(cw, key), replacements);
        }

        public string Localize(ClashWar cw, string key)
        {
            return _strings.GetText(key,
                _localization.GetCultureInfo(cw.Channel?.GuildId),
                "ClashOfClans".ToLowerInvariant());
        }
    }
}
