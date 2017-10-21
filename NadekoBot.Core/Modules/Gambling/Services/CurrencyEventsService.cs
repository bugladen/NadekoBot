using Discord;
using Discord.Commands;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Common.CurrencyEvents;
using NadekoBot.Core.Services;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling.Services
{
    public class CurrencyEventsService : INService, IUnloadableService
    {
        public ConcurrentDictionary<ulong, List<ReactionEvent>> ReactionEvents { get; }

        public SneakyEvent SneakyEvent { get; private set; } = null;
        private SemaphoreSlim _sneakyLock = new SemaphoreSlim(1, 1);

        public CurrencyEventsService()
        {
            ReactionEvents = new ConcurrentDictionary<ulong, List<ReactionEvent>>();
        }

        public async Task<bool> StartSneakyEvent(SneakyEvent ev, IUserMessage msg, ICommandContext ctx)
        {
            await _sneakyLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (SneakyEvent != null)
                    return false;

                SneakyEvent = ev;
                ev.OnEnded += () => SneakyEvent = null;
                var _ = SneakyEvent.Start(msg, ctx).ConfigureAwait(false);
            }
            finally
            {
                _sneakyLock.Release();
            }
            return true;
        }

        public async Task Unload()
        {
            foreach (var kvp in ReactionEvents)
            {
                foreach (var ev in kvp.Value)
                {
                    try { await ev.Stop().ConfigureAwait(false); } catch { }
                }
            }
            ReactionEvents.Clear();

            await _sneakyLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await SneakyEvent.Stop().ConfigureAwait(false);
            }
            finally
            {
                _sneakyLock.Release();
            }
        }
    }
}
