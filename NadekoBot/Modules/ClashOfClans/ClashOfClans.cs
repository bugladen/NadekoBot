using Discord.Commands;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Classes.ClashOfClans
{
    public enum DestroyStars
    {
        One, Two, Three
    }
    internal class Caller
    {
        public string CallUser { get; }

        public DateTime TimeAdded { get; private set; }

        public bool BaseDestroyed { get; internal set; }

        public int Stars { get; set; } = 3;


        public Caller(string callUser, DateTime timeAdded, bool baseDestroyed)
        {
            CallUser = callUser;
            TimeAdded = timeAdded;
            BaseDestroyed = baseDestroyed;
        }

        public void ResetTime()
        {
            TimeAdded = DateTime.Now;
        }

        public void Destroy()
        {
            BaseDestroyed = true;
        }
    }

    internal class ClashWar
    {
        private static TimeSpan callExpire => new TimeSpan(2, 0, 0);

        public string EnemyClan { get; }
        public int Size { get; }

        private Caller[] bases { get; }
        private CancellationTokenSource[] baseCancelTokens;
        private CancellationTokenSource endTokenSource { get; } = new CancellationTokenSource();
        public event Action<string> OnUserTimeExpired = delegate { };
        public event Action OnWarEnded = delegate { };
        public bool Started { get; set; } = false;

        public ClashWar(string enemyClan, int size, CommandEventArgs e)
        {
            this.EnemyClan = enemyClan;
            this.Size = size;
            this.bases = new Caller[size];
            this.baseCancelTokens = new CancellationTokenSource[size];
        }

        internal void End()
        {
            if (endTokenSource.Token.IsCancellationRequested) return;
            endTokenSource.Cancel();
            OnWarEnded();
        }

        internal void Call(string u, int baseNumber)
        {
            if (baseNumber < 0 || baseNumber >= bases.Length)
                throw new ArgumentException("Invalid base number");
            if (bases[baseNumber] != null)
                throw new ArgumentException("That base is already claimed.");
            for (var i = 0; i < bases.Length; i++)
            {
                if (bases[i]?.BaseDestroyed == false && bases[i]?.CallUser == u)
                    throw new ArgumentException($"@{u} You already claimed a base #{i + 1}. You can't claim a new one.");
            }

            bases[baseNumber] = new Caller(u.Trim(), DateTime.Now, false);
        }

        internal async Task Start()
        {
            if (Started)
                throw new InvalidOperationException();
            try
            {
                Started = true;
                foreach (var b in bases.Where(b => b != null))
                {
                    b.ResetTime();
                }
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () => await ClearArray()).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                await Task.Delay(new TimeSpan(24, 0, 0), endTokenSource.Token).ConfigureAwait(false);
            }
            catch { }
            finally
            {
                End();
            }
        }
        internal int Uncall(string user)
        {
            user = user.Trim();
            for (var i = 0; i < bases.Length; i++)
            {
                if (bases[i]?.CallUser != user) continue;
                bases[i] = null;
                return i;
            }
            throw new InvalidOperationException("You are not participating in that war.");
        }

        private async Task ClearArray()
        {
            while (!endTokenSource.IsCancellationRequested)
            {
                await Task.Delay(5000).ConfigureAwait(false);
                for (var i = 0; i < bases.Length; i++)
                {
                    if (bases[i] == null) continue;
                    if (!bases[i].BaseDestroyed && DateTime.Now - bases[i].TimeAdded >= callExpire)
                    {
                        OnUserTimeExpired(bases[i].CallUser);
                        bases[i] = null;
                    }
                }
            }
        }

        public string ShortPrint() =>
            $"`{EnemyClan}` ({Size} v {Size})";

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"🔰**WAR AGAINST `{EnemyClan}` ({Size} v {Size}) INFO:**");
            if (!Started)
                sb.AppendLine("`not started`");
            for (var i = 0; i < bases.Length; i++)
            {
                if (bases[i] == null)
                {
                    sb.AppendLine($"`{i + 1}.` ❌*unclaimed*");
                }
                else
                {
                    if (bases[i].BaseDestroyed)
                    {
                        sb.AppendLine($"`{i + 1}.` ✅ `{bases[i].CallUser}` {new string('⭐', bases[i].Stars)}");
                    }
                    else
                    {
                        var left = Started ? callExpire - (DateTime.Now - bases[i].TimeAdded) : callExpire;
                        sb.AppendLine($"`{i + 1}.` ✅ `{bases[i].CallUser}` {left.Hours}h {left.Minutes}m {left.Seconds}s left");
                    }
                }

            }
            return sb.ToString();
        }

        internal int FinishClaim(string user, int stars = 3)
        {
            user = user.Trim();
            for (var i = 0; i < bases.Length; i++)
            {
                if (bases[i]?.BaseDestroyed != false || bases[i]?.CallUser != user) continue;
                bases[i].BaseDestroyed = true;
                bases[i].Stars = stars;
                return i;
            }
            throw new InvalidOperationException($"@{user} You are either not participating in that war, or you already destroyed a base.");
        }
    }
}
