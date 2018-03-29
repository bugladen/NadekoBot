using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Core.Modules.Gambling.Common.Events;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NLog;

namespace NadekoBot.Core.Modules.Gambling.Common.CurrencyEvents
{
    public class NotRaidEvent
    {
        private DiscordSocketClient _client;
        private ICurrencyService _cs;
        private IBotConfigProvider _bc;
        private SocketGuild g;
        private SocketTextChannel ch;
        private EventOptions opts;
        private Func<Event.Type, EventOptions, long, EmbedBuilder> embed;
        private IUserMessage _msg;
        private readonly long _amount;
        public bool PotEmptied { get; private set; } = false;
        private readonly ConcurrentQueue<ulong> _toAward = new ConcurrentQueue<ulong>();

        public long PotSize { get; }

        private readonly bool _isPotLimited;
        private readonly Logger _log;
        private readonly Timer _t;

        //public event Func<ulong, Task> OnEnded;

        public NotRaidEvent(DiscordSocketClient client, ICurrencyService cs, IBotConfigProvider bc,
            SocketGuild g, SocketTextChannel ch, EventOptions opts,
            Func<Event.Type, EventOptions, long, EmbedBuilder> embed)
        {
            _client = client;
            _cs = cs;
            _bc = bc;
            this.g = g;
            this.ch = ch;
            this.opts = opts;
            this.embed = embed;
            _amount = opts.Amount;
            PotSize = opts.PotSize;
            _isPotLimited = PotSize > 0;
            _log = LogManager.GetCurrentClassLogger();

            _t = new Timer(OnTimerTick, null, Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(2));
        }

        private async void OnTimerTick(object state)
        {
            var potEmpty = PotEmptied;
            List<ulong> toAward = new List<ulong>();
            while (_toAward.TryDequeue(out var x))
            {
                toAward.Add(x);
            }

            if (!toAward.Any())
                return;

            try
            {
                await _cs.AddBulkAsync(toAward,
                    toAward.Select(x => "Reaction Event"),
                    toAward.Select(x => _amount),
                    gamble: true);

                if (_isPotLimited)
                {
                    await _msg.ModifyAsync(m =>
                    {
                        m.Embed = GetEmbed(PotSize).Build();
                    }, new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
                }

                _log.Info("Awarded {0} users {1} currency.{2}",
                    toAward.Count,
                    _amount,
                    _isPotLimited ? $" {PotSize} left." : "");

                if (potEmpty)
                {
                    var _ = Stop();
                }

            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        private EmbedBuilder GetEmbed(long pot)
        {
            return embed(Event.Type.Reaction, opts, pot);
        }

        public async Task Start()
        {
            _msg = await ch.EmbedAsync(GetEmbed(opts.PotSize));
        }

        public async Task Stop()
        {
            await Task.Yield();
            var _ = _msg.DeleteAsync();
        }
    }
}
