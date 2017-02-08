using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class TicTacToeCommands : ModuleBase
        {
            //channelId/game
            private static readonly ConcurrentDictionary<ulong, TicTacToe> _openGames = new ConcurrentDictionary<ulong, TicTacToe>();
            private readonly Logger _log;

            public TicTacToeCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Ttt(IGuildUser secondUser)
            {
                var channel = (ITextChannel)Context.Channel;


                TicTacToe game;
                if (_openGames.TryRemove(channel.Id, out game)) // joining open game
                {
                    if (!game.Join((IGuildUser)Context.User))
                    {
                        await Context.Channel.SendErrorAsync("You can't play against yourself. Game stopped.").ConfigureAwait(false);
                        return;
                    }
                    var _ = Task.Run(() => game.Start());
                    _log.Warn($"User {Context.User} joined a TicTacToe game.");
                    return;
                }
                game = new TicTacToe(channel, (IGuildUser)Context.User);
                if (_openGames.TryAdd(Context.Channel.Id, game))
                {
                    _log.Warn($"User {Context.User} created a TicTacToe game.");
                    await Context.Channel.SendConfirmAsync("Tic Tac Toe game created. Waiting for another user.").ConfigureAwait(false);
                }
            }
        }

        public class TicTacToe
        {

            private readonly ITextChannel _channel;
            private readonly Logger _log;
            private readonly IGuildUser[] _users;
            private readonly int?[,] _state;

            public TicTacToe(ITextChannel channel, IGuildUser firstUser)
            {
                _channel = channel;
                _users = new IGuildUser[2] { firstUser, null };
                _state = new int?[3, 3] {
                    { null, null, null },
                    { null, 1, 1 },
                    { 0, null, 0 },
                };

                _log = LogManager.GetCurrentClassLogger();
            }

            public string GetState()
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _state.GetLength(0); i++)
                {
                    for (int j = 0; j < _state.GetLength(1); j++)
                    {
                        sb.Append(GetIcon(_state[i, j]));
                        if (j < _state.GetLength(1) - 1)
                            sb.Append("┃");
                    }
                    if (i < _state.GetLength(0) - 1)
                        sb.AppendLine("\n──────────");
                }

                return sb.ToString();
            }

            public EmbedBuilder GetEmbed() =>
                new EmbedBuilder()
                    .WithOkColor()
                    .WithDescription(GetState())
                    .WithAuthor(eab => eab.WithName("Tic Tac Toe"))
                    .WithTitle($"{_users[0]} vs {_users[1]}");

            private static string GetIcon(int? val)
            {
                switch (val)
                {
                    case 0:
                        return "❌";
                    case 1:
                        return "⭕";
                    default:
                        return "⬛";
                }
            }

            public Task Start()
            {
                return Task.CompletedTask;
            }

            public void Join(IGuildUser user)
            {
                
            }
        }
    }
}
