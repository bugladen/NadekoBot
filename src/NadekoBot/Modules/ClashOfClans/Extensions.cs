using Discord;
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (baseNumber < 0 || baseNumber >= cw.Bases.Capacity)
                throw new ArgumentException("Invalid base number");
            if (cw.Bases[baseNumber] != null)
                throw new ArgumentException("That base is already claimed.");
            for (var i = 0; i < cw.Bases.Capacity; i++)
            {
                if (cw.Bases[i]?.BaseDestroyed == false && cw.Bases[i]?.CallUser == u)
                    throw new ArgumentException($"@{u} You already claimed base #{i + 1}. You can't claim a new one.");
            }

            cw.Bases[baseNumber] = new ClashCaller() {
                CallUser = u.Trim(),
                TimeAdded = DateTime.UtcNow,
                BaseDestroyed = false
            };
        }

        public static void Start(this ClashWar cw)
        {
            if (cw.WarState == StateOfWar.Started)
                throw new InvalidOperationException("War already started");
            //if (Started)
            //    throw new InvalidOperationException();
            //Started = true;
            cw.WarState = StateOfWar.Started;
            cw.StartedAt = DateTime.UtcNow;
            foreach (var b in cw.Bases.Where(b => b != null))
            {
                b.ResetTime();
            }
        }

        public static int Uncall(this ClashWar cw, string user)
        {
            user = user.Trim();
            for (var i = 0; i < cw.Bases.Capacity; i++)
            {
                if (cw.Bases[i]?.CallUser != user) continue;
                cw.Bases[i] = null;
                return i;
            }
            throw new InvalidOperationException("You are not participating in that war.");
        }

        public static string ShortPrint(this ClashWar cw) =>
            $"`{cw.EnemyClan}` ({cw.Size} v {cw.Size})";

        public static string ToPrettyString(this ClashWar cw)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"🔰**WAR AGAINST `{cw.EnemyClan}` ({cw.Size} v {cw.Size}) INFO:**");
            if (cw.WarState == StateOfWar.Created)
                sb.AppendLine("`not started`");
            var twoHours = new TimeSpan(2, 0, 0);
            for (var i = 0; i < cw.Bases.Capacity; i++)
            {
                if (cw.Bases[i] == null)
                {
                    sb.AppendLine($"`{i + 1}.` ❌*unclaimed*");
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
                        sb.AppendLine($"`{i + 1}.` ✅ `{cw.Bases[i].CallUser}` {left.Hours}h {left.Minutes}m {left.Seconds}s left");
                    }
                }

            }
            return sb.ToString();
        }

        public static int FinishClaim(this ClashWar cw, string user, int stars = 3)
        {
            user = user.Trim();
            for (var i = 0; i < cw.Bases.Capacity; i++)
            {
                if (cw.Bases[i]?.BaseDestroyed != false || cw.Bases[i]?.CallUser != user) continue;
                cw.Bases[i].BaseDestroyed = true;
                cw.Bases[i].Stars = stars;
                return i;
            }
            throw new InvalidOperationException($"@{user} You are either not participating in that war, or you already destroyed a base.");
        }
    }
}