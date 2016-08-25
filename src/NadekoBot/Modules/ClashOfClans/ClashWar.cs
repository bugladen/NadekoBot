using Discord;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

namespace NadekoBot.Classes.ClashOfClans
{
    public class ClashWar
    {
        public enum DestroyStars
        {
            One, Two, Three
        }
        public enum StateOfWar
        {
            Started, Ended, Created
        }

        public class Caller
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
        private static TimeSpan callExpire => new TimeSpan(2, 0, 0);

        public string EnemyClan { get; set; }
        public int Size { get; set; }

        public Caller[] Bases { get; set; }
        public StateOfWar WarState { get; set; } = StateOfWar.Created;
        //public bool Started { get; set; } = false;
        public DateTime StartedAt { get; set; }
        //public bool Ended { get; private set; } = false;

        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }

        [JsonIgnore]
        public ITextChannel Channel { get; internal set; }

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
            this.Channel = NadekoBot.Client.GetGuildsAsync() //nice api you got here volt, 
                                    .GetAwaiter() //especially like how getguildsasync isn't async at all internally. 
                                    .GetResult() //But hey, lib has to be async kek
                                    .FirstOrDefault(s => s.Id == serverId)? // srsly
                                    .GetChannelsAsync() //wtf is this
                                    .GetAwaiter() // oh i know, its the implementation detail
                                    .GetResult() // and makes library look consistent
                                    .FirstOrDefault(c => c.Id == channelId) // its not common sense to make library work like this.
                                        as ITextChannel; // oh and don't forget to cast it to this arbitrary bullshit 
        }

        public void End()
        {
            //Ended = true;
            WarState = StateOfWar.Ended;
        }

        public void Call(string u, int baseNumber)
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

        public void Start()
        {
            if (WarState == StateOfWar.Started)
                throw new InvalidOperationException("War already started");
            //if (Started)
            //    throw new InvalidOperationException();
            //Started = true;
            WarState = StateOfWar.Started;
            StartedAt = DateTime.UtcNow;
            foreach (var b in Bases.Where(b => b != null))
            {
                b.ResetTime();
            }
        }

        public int Uncall(string user)
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
            if (WarState == StateOfWar.Created)
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
                        var left = (WarState == StateOfWar.Started) ? callExpire - (DateTime.UtcNow - Bases[i].TimeAdded) : callExpire;
                        sb.AppendLine($"`{i + 1}.` ✅ `{Bases[i].CallUser}` {left.Hours}h {left.Minutes}m {left.Seconds}s left");
                    }
                }

            }
            return sb.ToString();
        }

        public int FinishClaim(string user, int stars = 3)
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
