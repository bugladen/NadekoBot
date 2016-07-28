using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
//using Manatee.Json.Serialization;

namespace NadekoBot.Classes.ClashOfClans
{
    public enum DestroyStars
    {
        One, Two, Three
    }
    public enum WarState
    {
        Started, Ended, Created
    }
    [System.Serializable]
    internal class Caller
    {
        public string CallUser { get; set; }

        public DateTime TimeAdded { get; set; }

        public bool BaseDestroyed { get; set; }

        public int Stars { get; set; } = 3;

        public Caller() { }

        public Caller(string callUser, DateTime timeAdded, bool baseDestroyed)
        {
            CallUser = callUser;
            TimeAdded = timeAdded;
            BaseDestroyed = baseDestroyed;
        }

        public void ResetTime()
        {
            TimeAdded = DateTime.UtcNow;
        }

        public void Destroy()
        {
            BaseDestroyed = true;
        }
    }

    internal class ClashWar
    {
        private static TimeSpan callExpire => new TimeSpan(2, 0, 0);

        public string EnemyClan { get; set; }
        public int Size { get; set; }

        public Caller[] Bases { get; set; }
        public WarState WarState { get; set; } = WarState.Created;
        //public bool Started { get; set; } = false;
        public DateTime StartedAt { get; set; }
        //public bool Ended { get; private set; } = false;

        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }

        [JsonIgnore]
        public Discord.Channel Channel { get; internal set; }

        /// <summary>
        /// This init is purely for the deserialization
        /// </summary>
        public ClashWar() { }

        public ClashWar(string enemyClan, int size, ulong serverId, ulong channelId)
        {
            this.EnemyClan = enemyClan;
            this.Size = size;
            this.Bases = new Caller[size];
            this.ServerId = serverId;
            this.ChannelId = channelId;
            this.Channel = NadekoBot.Client.Servers.FirstOrDefault(s => s.Id == serverId)?.TextChannels.FirstOrDefault(c => c.Id == channelId);
        }

        internal void End()
        {
            //Ended = true;
            WarState = WarState.Ended;
        }

        internal void Call(string u, int baseNumber)
        {
            if (baseNumber < 0 || baseNumber >= Bases.Length)
                throw new ArgumentException("Invalid base number");
            if (Bases[baseNumber] != null)
                throw new ArgumentException("That base is already claimed.");
            for (var i = 0; i < Bases.Length; i++)
            {
                if (Bases[i]?.BaseDestroyed == false && Bases[i]?.CallUser == u)
                    throw new ArgumentException($"@{u} You already claimed base #{i + 1}. You can't claim a new one.");
            }

            Bases[baseNumber] = new Caller(u.Trim(), DateTime.UtcNow, false);
        }

        internal void Start()
        {
            if (WarState == WarState.Started)
                throw new InvalidOperationException("War already started");
            //if (Started)
            //    throw new InvalidOperationException();
            //Started = true;
            WarState = WarState.Started;
            StartedAt = DateTime.UtcNow;
            foreach (var b in Bases.Where(b => b != null))
            {
                b.ResetTime();
            }
        }

        internal int Uncall(string user)
        {
            user = user.Trim();
            for (var i = 0; i < Bases.Length; i++)
            {
                if (Bases[i]?.CallUser != user) continue;
                Bases[i] = null;
                return i;
            }
            throw new InvalidOperationException("You are not participating in that war.");
        }

        public string ShortPrint() =>
            $"`{EnemyClan}` ({Size} v {Size})";

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"🔰**WAR AGAINST `{EnemyClan}` ({Size} v {Size}) INFO:**");
            if (WarState == WarState.Created)
                sb.AppendLine("`not started`");
            for (var i = 0; i < Bases.Length; i++)
            {
                if (Bases[i] == null)
                {
                    sb.AppendLine($"`{i + 1}.` ❌*unclaimed*");
                }
                else
                {
                    if (Bases[i].BaseDestroyed)
                    {
                        sb.AppendLine($"`{i + 1}.` ✅ `{Bases[i].CallUser}` {new string('⭐', Bases[i].Stars)}");
                    }
                    else
                    {
                        var left = (WarState == WarState.Started) ? callExpire - (DateTime.UtcNow - Bases[i].TimeAdded) : callExpire;
                        sb.AppendLine($"`{i + 1}.` ✅ `{Bases[i].CallUser}` {left.Hours}h {left.Minutes}m {left.Seconds}s left");
                    }
                }

            }
            return sb.ToString();
        }

        internal int FinishClaim(string user, int stars = 3)
        {
            user = user.Trim();
            for (var i = 0; i < Bases.Length; i++)
            {
                if (Bases[i]?.BaseDestroyed != false || Bases[i]?.CallUser != user) continue;
                Bases[i].BaseDestroyed = true;
                Bases[i].Stars = stars;
                return i;
            }
            throw new InvalidOperationException($"@{user} You are either not participating in that war, or you already destroyed a base.");
        }
    }
}
