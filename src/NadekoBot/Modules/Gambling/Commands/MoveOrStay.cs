using System.Collections.Concurrent;
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
    //    public class MoveOrStayCommands : NadekoSubmodule
    //    {
    //        [NadekoCommand, Usage, Description, Aliases]
    //        [RequireContext(ContextType.Guild)]
    //        [OwnerOnly]
    //        public async Task MoveOrStayTest()
    //        {
    //            //test 1, just stop on second one

    //            //test 2, stop when winning

    //        }

    //        private static readonly ConcurrentDictionary<ulong, MoveOrStayGame> _games = new ConcurrentDictionary<ulong, MoveOrStayGame>();

    //        [NadekoCommand, Usage, Description, Aliases]
    //        [RequireContext(ContextType.Guild)]
    //        public async Task MoveOrStay(int bet)
    //        {
    //            if (bet < 4)
    //                return;
    //            var game = new MoveOrStayGame(bet);
    //            if (!_games.TryAdd(Context.User.Id, game))
    //            {
    //                await ReplyAsync("You're already betting on move or stay.").ConfigureAwait(false);
    //                return;
    //            }

    //            if (!await CurrencyHandler.RemoveCurrencyAsync(Context.User, "MoveOrStay bet", bet, false))
    //            {
    //                await ReplyAsync("You don't have enough money");
    //                return;
    //            }
    //            await Context.Channel.EmbedAsync(GetGameState(game), 
    //                string.Format("{0} rolled {1}.", Context.User, game.Rolled)).ConfigureAwait(false);
    //        }

    //        public enum Mors
    //        {
    //            Move = 1,
    //            M = 1,
    //            Stay = 2,
    //            S = 2
    //        }

    //        [NadekoCommand, Usage, Description, Aliases]
    //        [RequireContext(ContextType.Guild)]
    //        public async Task MoveOrStay(Mors action)
    //        {
    //            MoveOrStayGame game;
    //            if (!_games.TryGetValue(Context.User.Id, out game))
    //            {
    //                await ReplyAsync("You're not betting on move or stay.").ConfigureAwait(false);
    //                return;
    //            }

    //            if (action == Mors.Move)
    //            {
    //                game.Move();
    //                await Context.Channel.EmbedAsync(GetGameState(game), string.Format("{0} rolled {1}.", Context.User, game.Rolled)).ConfigureAwait(false); if (game.Ended)
    //                    _games.TryRemove(Context.User.Id, out game);
    //            }
    //            else if (action == Mors.Stay)
    //            {
    //                var won = game.Stay();
    //                await CurrencyHandler.AddCurrencyAsync(Context.User, "MoveOrStay stay", won, false)
    //                    .ConfigureAwait(false);
    //                _games.TryRemove(Context.User.Id, out game);
    //                await ReplyAsync(string.Format("You've finished with {0}", 
    //                    won + CurrencySign))
    //                    .ConfigureAwait(false);
    //            }

                
    //        }

    //        private EmbedBuilder GetGameState(MoveOrStayGame game)
    //        {
    //            var arr = MoveOrStayGame.Winnings.ToArray();
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
    //                .WithTitle("Move or Stay")
    //                .WithDescription(sb.ToString())
    //                .AddField(efb => efb.WithName("Bet")
    //                                    .WithValue(game.Bet.ToString())
    //                                    .WithIsInline(true))
    //                .AddField(efb => efb.WithName("Current Value")
    //                                    .WithValue((game.CurrentMultiplier * game.Bet).ToString(_cultureInfo))
    //                                    .WithIsInline(true));
    //        }
    //    }

    //    public class MoveOrStayGame
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
    //            1.5f, 0.75f, 1f, 0.5f, 0.25f, 0.25f, 2.5f, 0f, 0f
    //        }.ToImmutableArray();

    //        public MoveOrStayGame(int bet)
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

    //            if (CurrentPosition >= 7)
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
