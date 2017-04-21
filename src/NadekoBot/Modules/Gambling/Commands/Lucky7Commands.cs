using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;

namespace NadekoBot.Modules.Gambling
{
    //public partial class Gambling
    //{
    //    [Group]
    //    public class Lucky7Commands : NadekoSubmodule
    //    {
    //        [NadekoCommand, Usage, Description, Aliases]
    //        [RequireContext(ContextType.Guild)]
    //        [OwnerOnly]
    //        public async Task Lucky7Test(uint tests)
    //        {
    //            if (tests <= 0)
    //                return;

    //            var dict = new Dictionary<float, int>();
    //            var totalWon = 0;
    //            for (var i = 0; i < tests; i++)
    //            {
    //                var g = new Lucky7Game(10);
    //                while (!g.Ended)
    //                {
    //                    if (g.CurrentPosition == 0)
    //                        g.Stay();
    //                    else
    //                        g.Move();
    //                }
    //                totalWon += (int)(g.CurrentMultiplier * g.Bet);
    //                if (!dict.ContainsKey(g.CurrentMultiplier))
    //                    dict.Add(g.CurrentMultiplier, 0);

    //                dict[g.CurrentMultiplier] ++;

    //            }

    //            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
    //                .WithTitle("Move Or Stay test")
    //                .WithDescription(string.Join("\n",
    //                    dict.Select(x => $"x{x.Key} occured {x.Value} times {x.Value * 1.0f / tests * 100:F2}%")))
    //                .WithFooter(
    //                    efb => efb.WithText($"Total Bet: {tests * 10} | Payout: {totalWon} | {totalWon *1.0f / tests * 10}%")));
    //        }

    //        private static readonly ConcurrentDictionary<ulong, Lucky7Game> _games =
    //            new ConcurrentDictionary<ulong, Lucky7Game>();

    //        [NadekoCommand, Usage, Description, Aliases]
    //        [RequireContext(ContextType.Guild)]
    //        public async Task Lucky7(int bet)
    //        {
    //            if (bet < 4)
    //                return;
    //            var game = new Lucky7Game(bet);
    //            if (!_games.TryAdd(Context.User.Id, game))
    //            {
    //                await ReplyAsync("You're already betting on move or stay.").ConfigureAwait(false);
    //                return;
    //            }

    //            if (!await CurrencyHandler.RemoveCurrencyAsync(Context.User, "MoveOrStay bet", bet, false))
    //            {
    //                _games.TryRemove(Context.User.Id, out game);
    //                await ReplyConfirmLocalized("not_enough", CurrencySign).ConfigureAwait(false);
    //                return;
    //            }
    //            await Context.Channel.EmbedAsync(GetGameState(game),
    //                string.Format("{0} rolled {1}.", Context.User, game.Rolled)).ConfigureAwait(false);
    //        }

    //        public enum MoveOrStay
    //        {
    //            Move = 1,
    //            M = 1,
    //            Stay = 2,
    //            S = 2
    //        }

    //        [NadekoCommand, Usage, Description, Aliases]
    //        [RequireContext(ContextType.Guild)]
    //        public async Task Lucky7(MoveOrStay action)
    //        {
    //            Lucky7Game game;
    //            if (!_games.TryGetValue(Context.User.Id, out game))
    //            {
    //                await ReplyAsync("You're not betting on move or stay.").ConfigureAwait(false);
    //                return;
    //            }

    //            if (action == MoveOrStay.Move)
    //            {
    //                game.Move();
    //                await Context.Channel.EmbedAsync(GetGameState(game),
    //                    string.Format("{0} rolled {1}.", Context.User, game.Rolled)).ConfigureAwait(false);
    //                if (game.Ended)
    //                    _games.TryRemove(Context.User.Id, out game);
    //            }
    //            else if (action == MoveOrStay.Stay)
    //            {
    //                var won = game.Stay();
    //                await CurrencyHandler.AddCurrencyAsync(Context.User, "MoveOrStay stay", won, false)
    //                    .ConfigureAwait(false);
    //                _games.TryRemove(Context.User.Id, out game);
    //                await ReplyAsync(string.Format("You've finished with {0}",
    //                        won + CurrencySign))
    //                    .ConfigureAwait(false);
    //            }


    //        }

    //        private EmbedBuilder GetGameState(Lucky7Game game)
    //        {
    //            var arr = Lucky7Game.Winnings.ToArray();
    //            var sb = new StringBuilder();
    //            for (var i = 0; i < arr.Length; i++)
    //            {
    //                if (i == game.CurrentPosition)
    //                {
    //                    sb.Append("[" + arr[i] + "]");
    //                }
    //                else
    //                {
    //                    sb.Append(arr[i].ToString());
    //                }

    //                if (i != arr.Length - 1)
    //                    sb.Append(' ');
    //            }

    //            return new EmbedBuilder().WithOkColor()
    //                .WithTitle("Lucky7")
    //                .WithDescription(sb.ToString())
    //                .AddField(efb => efb.WithName("Bet")
    //                    .WithValue(game.Bet.ToString())
    //                    .WithIsInline(true))
    //                .AddField(efb => efb.WithName("Current Value")
    //                    .WithValue((game.CurrentMultiplier * game.Bet).ToString(_cultureInfo))
    //                    .WithIsInline(true));
    //        }
    //    }

    //    public class Lucky7Game
    //    {
    //        public int Bet { get; }
    //        public bool Ended { get; private set; }
    //        public int PreviousPosition { get; private set; }
    //        public int CurrentPosition { get; private set; } = -1;
    //        public int Rolled { get; private set; }
    //        public float CurrentMultiplier => Winnings[CurrentPosition];
    //        private readonly NadekoRandom _rng = new NadekoRandom();

    //        public static readonly ImmutableArray<float> Winnings = new[]
    //        {
    //            1.2f, 0.8f, 0.75f, 0.90f, 0.7f, 0.5f, 1.8f, 0f, 0f
    //        }.ToImmutableArray();

    //        public Lucky7Game(int bet)
    //        {
    //            Bet = bet;
    //            Move();
    //        }

    //        public void Move()
    //        {
    //            if (Ended)
    //                return;
    //            PreviousPosition = CurrentPosition;
    //            Rolled = _rng.Next(1, 4);
    //            CurrentPosition += Rolled;

    //            if (CurrentPosition >= 6)
    //                Ended = true;
    //        }

    //        public int Stay()
    //        {
    //            if (Ended)
    //                return 0;

    //            Ended = true;
    //            return (int) (CurrentMultiplier * Bet);
    //        }
    //    }
    //}
}
