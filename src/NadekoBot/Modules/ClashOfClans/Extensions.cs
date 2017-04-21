using NadekoBot.Services.Database.Models;
using System;
using System.Linq;
using System.Text;
using static NadekoBot.Services.Database.Models.ClashWar;

namespace NadekoBot.Modules.ClashOfClans
{
    public static class Extensions
    {
        public static void ResetTime(this ClashCaller c)
        {
            c.TimeAdded = DateTime.UtcNow;
        }

        public static void Destroy(this ClashCaller c)
        {
            c.BaseDestroyed = true;
        }

        public static void End(this ClashWar cw)
        {
            //Ended = true;
            cw.WarState = StateOfWar.Ended;
        }

        public static void Call(this ClashWar cw, string u, int baseNumber)
        {
            if (baseNumber < 0 || baseNumber >= cw.Bases.Count)
                throw new ArgumentException(cw.Localize("invalid_base_number"));
            if (cw.Bases[baseNumber].CallUser != null && cw.Bases[baseNumber].Stars == 3)
                throw new ArgumentException(cw.Localize("base_already_claimed"));
            for (var i = 0; i < cw.Bases.Count; i++)
            {
                if (cw.Bases[i]?.BaseDestroyed == false && cw.Bases[i]?.CallUser == u)
                    throw new ArgumentException(cw.Localize("claimed_other", u, i + 1));
            }

            var cc = cw.Bases[baseNumber];
            cc.CallUser = u.Trim();
            cc.TimeAdded = DateTime.UtcNow;
            cc.BaseDestroyed = false;
        }

        public static void Start(this ClashWar cw)
        {
            if (cw.WarState == StateOfWar.Started)
                throw new InvalidOperationException("war_already_started");
            //if (Started)
            //    throw new InvalidOperationException();
            //Started = true;
            cw.WarState = StateOfWar.Started;
            cw.StartedAt = DateTime.UtcNow;
            foreach (var b in cw.Bases.Where(b => b.CallUser != null))
            {
                b.ResetTime();
            }
        }

        public static int Uncall(this ClashWar cw, string user)
        {
            user = user.Trim();
            for (var i = 0; i < cw.Bases.Count; i++)
            {
                if (cw.Bases[i]?.CallUser != user) continue;
                cw.Bases[i].CallUser = null;
                return i;
            }
            throw new InvalidOperationException(cw.Localize("not_partic"));
        }

        public static string ShortPrint(this ClashWar cw) =>
            $"`{cw.EnemyClan}` ({cw.Size} v {cw.Size})";

        public static string ToPrettyString(this ClashWar cw)
        {
            var sb = new StringBuilder();
            
            if (cw.WarState == StateOfWar.Created)
                sb.AppendLine("`not started`");
            var twoHours = new TimeSpan(2, 0, 0);
            for (var i = 0; i < cw.Bases.Count; i++)
            {
                if (cw.Bases[i].CallUser == null)
                {
                    sb.AppendLine($"`{i + 1}.` ❌*{cw.Localize("not_claimed")}*");
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

        public static int FinishClaim(this ClashWar cw, string user, int stars = 3)
        {
            user = user.Trim();
            for (var i = 0; i < cw.Bases.Count; i++)
            {
                if (cw.Bases[i]?.BaseDestroyed != false || cw.Bases[i]?.CallUser != user) continue;
                cw.Bases[i].BaseDestroyed = true;
                cw.Bases[i].Stars = stars;
                return i;
            }
            throw new InvalidOperationException(cw.Localize("not_partic_or_destroyed", user));
        }

        public static void FinishClaim(this ClashWar cw, int index, int stars = 3)
        {
            if (index < 0 || index > cw.Bases.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            var toFinish = cw.Bases[index];
            if (toFinish.BaseDestroyed != false) throw new InvalidOperationException(cw.Localize("base_already_destroyed"));
            if (toFinish.CallUser == null) throw new InvalidOperationException(cw.Localize("base_already_unclaimed"));
            toFinish.BaseDestroyed = true;
            toFinish.Stars = stars;
        }

        public static string Localize(this ClashWar cw, string key)
        {
            return NadekoTopLevelModule.GetTextStatic(key,
                NadekoBot.Localization.GetCultureInfo(cw.Channel?.GuildId),
                typeof(ClashOfClans).Name.ToLowerInvariant());
        }

        public static string Localize(this ClashWar cw, string key, params object[] replacements)
        {
            return string.Format(cw.Localize(key), replacements);
        }
    }
}