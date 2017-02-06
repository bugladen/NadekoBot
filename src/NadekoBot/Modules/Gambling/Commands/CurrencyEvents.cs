using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Services.Database;
using System.Threading;
using NLog;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEvents : ModuleBase
        {
            public enum CurrencyEvent
            {
                FlowerReaction,
                SneakyGameStatus
            }
            //flower reaction event
            public static readonly ConcurrentHashSet<ulong> _sneakyGameAwardedUsers = new ConcurrentHashSet<ulong>();


            private static readonly char[] _sneakyGameStatusChars = Enumerable.Range(48, 10)
                .Concat(Enumerable.Range(65, 26))
                .Concat(Enumerable.Range(97, 26))
                .Select(x => (char)x)
                .ToArray();

            private static string _secretCode = String.Empty;

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartEvent(CurrencyEvent e, int arg = -1)
            {
                var channel = (ITextChannel)Context.Channel;
                try
                {
                    switch (e)
                    {
                        case CurrencyEvent.FlowerReaction:
                            await FlowerReactionEvent(Context).ConfigureAwait(false);
                            break;
                        case CurrencyEvent.SneakyGameStatus:
                            await SneakyGameStatusEvent(Context, arg).ConfigureAwait(false);
                            break;
                        default:
                            break;
                    }
                }
                catch { }
            }

            public static async Task SneakyGameStatusEvent(CommandContext Context, int? arg)
            {
                int num;
                if (arg == null || arg < 5)
                    num = 60;
                else
                    num = arg.Value;

                if (_secretCode != String.Empty)
                    return;
                var rng = new NadekoRandom();

                for (int i = 0; i < 5; i++)
                {
                    _secretCode += _sneakyGameStatusChars[rng.Next(0, _sneakyGameStatusChars.Length)];
                }

                var game = NadekoBot.Client.Game?.Name;
                await NadekoBot.Client.SetGameAsync($"type {_secretCode} for " + NadekoBot.BotConfig.CurrencyPluralName)
                    .ConfigureAwait(false);
                try
                {
                    await Context.Channel.SendConfirmAsync($"SneakyGameStatus event started",
                        $"Users must type a secret code to get 100 currency.\n" +
                        $"Lasts {num} seconds. Don't tell anyone. Shhh.")
                        .ConfigureAwait(false);
                }
                catch { }


                NadekoBot.Client.MessageReceived += SneakyGameMessageReceivedEventHandler;
                await Task.Delay(num * 1000);
                NadekoBot.Client.MessageReceived -= SneakyGameMessageReceivedEventHandler;

                var cnt = _sneakyGameAwardedUsers.Count;
                _sneakyGameAwardedUsers.Clear();
                _secretCode = String.Empty;

                await NadekoBot.Client.SetGameAsync($"SneakyGame event ended. {cnt} users received a reward.")
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
                        catch { }
                    });
                }

                return Task.Delay(0);
            }

            public static Task FlowerReactionEvent(CommandContext Context) =>
                new FlowerReactionEvent().Start(Context);
        }
    }

    public abstract class CurrencyEvent
    {
        public abstract Task Start(CommandContext channel);
    }

    public class FlowerReactionEvent : CurrencyEvent
    {
        public readonly ConcurrentHashSet<ulong> _flowerReactionAwardedUsers = new ConcurrentHashSet<ulong>();
        private readonly Logger _log;

        private IUserMessage msg { get; set; } = null;

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
                Task.Run(() => End());
            }

            return Task.CompletedTask;
        }

        public override async Task Start(CommandContext context)
        {
            msg = await context.Channel.SendConfirmAsync("Flower reaction event started!",
                    "Add 🌸 reaction to this message to get 100" + NadekoBot.BotConfig.CurrencySign,
                    footer: "This event is active for up to 24 hours.")
                                               .ConfigureAwait(false);

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
                        try { await CurrencyHandler.AddCurrencyAsync(r.User.Value, "Flower Reaction Event", 100, false).ConfigureAwait(false); } catch { }
                    }
                }
                catch { }
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
