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

            public async Task SneakyGameStatusEvent(ICommandContext context, int? arg)
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

            public async Task FlowerReactionEvent(ICommandContext context, int amount)
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
        public abstract Task Start(IUserMessage msg, ICommandContext channel, int amount);
    }

    public class FlowerReactionEvent : CurrencyEvent
    {
        private readonly ConcurrentHashSet<ulong> _flowerReactionAwardedUsers = new ConcurrentHashSet<ulong>();
        private readonly Logger _log;

        private IUserMessage StartingMessage { get; set; }

        private CancellationTokenSource Source { get; }
        private CancellationToken CancelToken { get; }

        public FlowerReactionEvent()
        {
            _log = LogManager.GetCurrentClassLogger();
            Source = new CancellationTokenSource();
            CancelToken = Source.Token;
        }

        private async Task End()
        {
            if(StartingMessage != null)
                await StartingMessage.DeleteAsync().ConfigureAwait(false);

            if(!Source.IsCancellationRequested)
                Source.Cancel();

            NadekoBot.Client.MessageDeleted -= MessageDeletedEventHandler;
        }

        private Task MessageDeletedEventHandler(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel) {
            if (StartingMessage?.Id == msg.Id)
            {
                _log.Warn("Stopping flower reaction event because message is deleted.");
                var __ = Task.Run(End);
            }

            return Task.CompletedTask;
        }

        public override async Task Start(IUserMessage umsg, ICommandContext context, int amount)
        {
            StartingMessage = umsg;
            NadekoBot.Client.MessageDeleted += MessageDeletedEventHandler;

            try { await StartingMessage.AddReactionAsync("🌸").ConfigureAwait(false); }
            catch
            {
                try { await StartingMessage.AddReactionAsync("🌸").ConfigureAwait(false); }
                catch
                {
                    try { await StartingMessage.DeleteAsync().ConfigureAwait(false); }
                    catch { return; }
                }
            }
            using (StartingMessage.OnReaction(async (r) =>
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
                    await Task.Delay(TimeSpan.FromHours(24), CancelToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    
                }
                if (CancelToken.IsCancellationRequested)
                    return;

                _log.Warn("Stopping flower reaction event because it expired.");
                await End();
                
            }
        }
    }
}
