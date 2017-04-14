using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Threading;
using NLog;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEvents : NadekoSubmodule
        {
            public enum CurrencyEvent
            {
                FlowerReaction,
                SneakyGameStatus
            }
            //flower reaction event
            private static readonly ConcurrentHashSet<ulong> _sneakyGameAwardedUsers = new ConcurrentHashSet<ulong>();


            private static readonly char[] _sneakyGameStatusChars = Enumerable.Range(48, 10)
                .Concat(Enumerable.Range(65, 26))
                .Concat(Enumerable.Range(97, 26))
                .Select(x => (char)x)
                .ToArray();

            private static string _secretCode = string.Empty;

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartEvent(CurrencyEvent e, int arg = -1)
            {
                switch (e)
                {
                    case CurrencyEvent.FlowerReaction:
                        await FlowerReactionEvent(Context, arg).ConfigureAwait(false);
                        break;
                    case CurrencyEvent.SneakyGameStatus:
                        await SneakyGameStatusEvent(Context, arg).ConfigureAwait(false);
                        break;
                }
            }

            public async Task SneakyGameStatusEvent(CommandContext context, int? arg)
            {
                int num;
                if (arg == null || arg < 5)
                    num = 60;
                else
                    num = arg.Value;

                if (_secretCode != string.Empty)
                    return;
                var rng = new NadekoRandom();

                for (var i = 0; i < 5; i++)
                {
                    _secretCode += _sneakyGameStatusChars[rng.Next(0, _sneakyGameStatusChars.Length)];
                }
                
                await NadekoBot.Client.SetGameAsync($"type {_secretCode} for " + NadekoBot.BotConfig.CurrencyPluralName)
                    .ConfigureAwait(false);
                try
                {
                    var title = GetText("sneakygamestatus_title");
                    var desc = GetText("sneakygamestatus_desc", Format.Bold(100.ToString()) + CurrencySign, Format.Bold(num.ToString()));
                    await context.Channel.SendConfirmAsync(title, desc).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }


                NadekoBot.Client.MessageReceived += SneakyGameMessageReceivedEventHandler;
                await Task.Delay(num * 1000);
                NadekoBot.Client.MessageReceived -= SneakyGameMessageReceivedEventHandler;

                var cnt = _sneakyGameAwardedUsers.Count;
                _sneakyGameAwardedUsers.Clear();
                _secretCode = string.Empty;

                await NadekoBot.Client.SetGameAsync(GetText("sneakygamestatus_end", cnt))
                    .ConfigureAwait(false);
            }

            private static Task SneakyGameMessageReceivedEventHandler(SocketMessage arg)
            {
                if (arg.Content == _secretCode &&
                    _sneakyGameAwardedUsers.Add(arg.Author.Id))
                {
                    var _ = Task.Run(async () =>
                    {
                        await CurrencyHandler.AddCurrencyAsync(arg.Author, "Sneaky Game Event", 100, false)
                            .ConfigureAwait(false);

                        try { await arg.DeleteAsync(new RequestOptions() { RetryMode = RetryMode.AlwaysFail }).ConfigureAwait(false); }
                        catch
                        {
                            // ignored
                        }
                    });
                }

                return Task.Delay(0);
            }

            public async Task FlowerReactionEvent(CommandContext context, int amount)
            {
                if (amount <= 0)
                    amount = 100;

                var title = GetText("flowerreaction_title");
                var desc = GetText("flowerreaction_desc", "🌸", Format.Bold(amount.ToString()) + CurrencySign);
                var footer = GetText("flowerreaction_footer", 24);
                var msg = await context.Channel.SendConfirmAsync(title,
                        desc, footer: footer)
                    .ConfigureAwait(false);

                await new FlowerReactionEvent().Start(msg, context, amount);
            }
        }
    }

    public abstract class CurrencyEvent
    {
        public abstract Task Start(IUserMessage msg, CommandContext channel, int amount);
    }

    public class FlowerReactionEvent : CurrencyEvent
    {
        private readonly ConcurrentHashSet<ulong> _flowerReactionAwardedUsers = new ConcurrentHashSet<ulong>();
        private readonly Logger _log;

        private IUserMessage msg { get; set; }

        private CancellationTokenSource source { get; }
        private CancellationToken cancelToken { get; }

        public FlowerReactionEvent()
        {
            _log = LogManager.GetCurrentClassLogger();
            source = new CancellationTokenSource();
            cancelToken = source.Token;
        }

        private async Task End()
        {
            if(msg != null)
                await msg.DeleteAsync().ConfigureAwait(false);

            if(!source.IsCancellationRequested)
                source.Cancel();

            NadekoBot.Client.MessageDeleted -= MessageDeletedEventHandler;
        }

        private Task MessageDeletedEventHandler(ulong id, Optional<SocketMessage> _) {
            if (msg?.Id == id)
            {
                _log.Warn("Stopping flower reaction event because message is deleted.");
                var __ = Task.Run(End);
            }

            return Task.CompletedTask;
        }

        public override async Task Start(IUserMessage umsg, CommandContext context, int amount)
        {
            msg = umsg;
            NadekoBot.Client.MessageDeleted += MessageDeletedEventHandler;

            try { await msg.AddReactionAsync("🌸").ConfigureAwait(false); }
            catch
            {
                try { await msg.AddReactionAsync("🌸").ConfigureAwait(false); }
                catch
                {
                    try { await msg.DeleteAsync().ConfigureAwait(false); }
                    catch { return; }
                }
            }
            using (msg.OnReaction(async (r) =>
            {
                try
                {
                    if (r.Emoji.Name == "🌸" && r.User.IsSpecified && ((DateTime.UtcNow - r.User.Value.CreatedAt).TotalDays > 5) && _flowerReactionAwardedUsers.Add(r.User.Value.Id))
                    {
                        await CurrencyHandler.AddCurrencyAsync(r.User.Value, "Flower Reaction Event", amount, false)
                            .ConfigureAwait(false);
                    }
                }
                catch
                {
                    // ignored
                }
            }))
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), cancelToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    
                }
                if (cancelToken.IsCancellationRequested)
                    return;

                _log.Warn("Stopping flower reaction event because it expired.");
                await End();
                
            }
        }
    }
}
