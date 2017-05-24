using NadekoBot.Services.Database.Models;
using System;
using System.Linq;

namespace NadekoBot.Services.ClashOfClans
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
    }
}