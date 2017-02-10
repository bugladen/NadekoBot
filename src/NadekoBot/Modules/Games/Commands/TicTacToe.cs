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
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        //todo timeout
        [Group]
        public class TicTacToeCommands : ModuleBase
        {
            //channelId/game
            private static readonly Dictionary<ulong, TicTacToe> _games = new Dictionary<ulong, TicTacToe>();
            private readonly Logger _log;

            public TicTacToeCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
            }

            private readonly SemaphoreSlim sem = new SemaphoreSlim(1, 1);
            private readonly object tttLockObj = new object();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task TicTacToe()
            {
                var channel = (ITextChannel)Context.Channel;

                await sem.WaitAsync(1000);
                try
                {
                    TicTacToe game;
                    if (_games.TryGetValue(channel.Id, out game))
                    {
                        var _ = Task.Run(async () =>
                        {
                            await game.Start((IGuildUser)Context.User);
                        });
                        return;
                    }
                    game = new TicTacToe(channel, (IGuildUser)Context.User);
                    _games.Add(channel.Id, game);
                    await Context.Channel.SendConfirmAsync($"{Context.User.Mention} Created a TicTacToe game.").ConfigureAwait(false);

                    game.OnEnded += (g) =>
                    {
                        _games.Remove(channel.Id);
                    };
                }
                finally
                {
                    sem.Release();
                }
            }
        }

        public class TicTacToe
        {
            enum Phase
            {
                Starting,
                Started,
                Ended
            }

            private readonly ITextChannel _channel;
            private readonly Logger _log;
            private readonly IGuildUser[] _users;
            private readonly int?[,] _state;
            private Phase _phase;
            int curUserIndex = 0;
            private readonly SemaphoreSlim moveLock;

            private IGuildUser _winner = null;

            private readonly string[] numbers = { ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:" };

            public Action<TicTacToe> OnEnded;

            private IUserMessage previousMessage = null;
            private Timer timeoutTimer;

            public TicTacToe(ITextChannel channel, IGuildUser firstUser)
            {
                _channel = channel;
                _users = new IGuildUser[2] { firstUser, null };
                _state = new int?[3, 3] {
                    { null, null, null },
                    { null, null, null },
                    { null, null, null },
                };

                _log = LogManager.GetCurrentClassLogger();
                _log.Warn($"User {firstUser} created a TicTacToe game.");
                _phase = Phase.Starting;
                moveLock = new SemaphoreSlim(1, 1);

                timeoutTimer = new Timer(async (_) =>
                {
                    await moveLock.WaitAsync();
                    try
                    {
                        if (_phase == Phase.Ended)
                            return;

                        _phase = Phase.Ended;
                        if (_users[1] != null)
                        {
                            _winner = _users[curUserIndex ^= 1];
                            var del = previousMessage?.DeleteAsync();
                            try
                            {
                                await _channel.EmbedAsync(GetEmbed("Time Expired!")).ConfigureAwait(false);
                                await del.ConfigureAwait(false);
                            }
                            catch { }
                        }

                        OnEnded?.Invoke(this);
                    }
                    catch { }
                    finally
                    {
                        moveLock.Release();
                    }
                }, null, 15000, Timeout.Infinite);
            }

            public string GetState()
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _state.GetLength(0); i++)
                {
                    for (int j = 0; j < _state.GetLength(1); j++)
                    {
                        sb.Append(_state[i, j] == null ? numbers[i * 3 + j] : GetIcon(_state[i, j]));
                        if (j < _state.GetLength(1) - 1)
                            sb.Append("┃");
                    }
                    if (i < _state.GetLength(0) - 1)
                        sb.AppendLine("\n──────────");
                }

                return sb.ToString();
            }

            public EmbedBuilder GetEmbed(string title = null)
            {
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithDescription(Environment.NewLine + GetState())
                    .WithAuthor(eab => eab.WithName($"{_users[0]} vs {_users[1]}"));

                if (!string.IsNullOrWhiteSpace(title))
                    embed.WithTitle(title);

                if (_winner == null)
                {
                    if (_phase == Phase.Ended)
                        embed.WithFooter(efb => efb.WithText($"No moves left!"));
                    else
                        embed.WithFooter(efb => efb.WithText($"{_users[curUserIndex]}'s move"));
                }
                else
                    embed.WithFooter(efb => efb.WithText($"{_winner} Won!"));

                return embed;
            }

            private static string GetIcon(int? val)
            {
                switch (val)
                {
                    case 0:
                        return "❌";
                    case 1:
                        return "⭕";
                    case 2:
                        return "❎";
                    case 3:
                        return "🅾";
                    default:
                        return "⬛";
                }
            }

            public async Task Start(IGuildUser user)
            {
                if (_phase == Phase.Started || _phase == Phase.Ended)
                {
                    await _channel.SendErrorAsync(user.Mention + " TicTacToe Game is already running in this channel.").ConfigureAwait(false);
                    return;
                }
                else if (_users[0] == user)
                {
                    await _channel.SendErrorAsync(user.Mention + " You can't play against yourself.").ConfigureAwait(false);
                    return;
                }

                _users[1] = user;
                _log.Warn($"User {user} joined a TicTacToe game.");

                _phase = Phase.Started;

                NadekoBot.Client.MessageReceived += Client_MessageReceived;

                previousMessage = await _channel.EmbedAsync(GetEmbed("Game Started")).ConfigureAwait(false);
            }

            private bool IsDraw()
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (_state[i, j] == null)
                            return false;
                    }
                }
                return true;
            }

            private Task Client_MessageReceived(Discord.WebSocket.SocketMessage msg)
            {
                var _ = Task.Run(async () =>
                {
                    await moveLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var curUser = _users[curUserIndex];
                        if (_phase == Phase.Ended || msg.Author?.Id != curUser.Id)
                            return;

                        int index;
                        if (int.TryParse(msg.Content, out index) &&
                            --index >= 0 &&
                            index <= 9 &&
                            _state[index / 3, index % 3] == null)
                        {
                            _state[index / 3, index % 3] = curUserIndex;

                            // i'm lazy
                            if (_state[index / 3, 0] == _state[index / 3, 1] && _state[index / 3, 1] == _state[index / 3, 2])
                            {
                                _state[index / 3, 0] = curUserIndex + 2;
                                _state[index / 3, 1] = curUserIndex + 2;
                                _state[index / 3, 2] = curUserIndex + 2;

                                _phase = Phase.Ended;
                            }
                            else if (_state[0, index % 3] == _state[1, index % 3] && _state[1, index % 3] == _state[2, index % 3])
                            {
                                _state[0, index % 3] = curUserIndex + 2;
                                _state[1, index % 3] = curUserIndex + 2;
                                _state[2, index % 3] = curUserIndex + 2;

                                _phase = Phase.Ended;
                            }
                            else if (curUserIndex == _state[0, 0] && _state[0, 0] == _state[1, 1] && _state[1, 1] == _state[2, 2])
                            {
                                _state[0, 0] = curUserIndex + 2;
                                _state[1, 1] = curUserIndex + 2;
                                _state[2, 2] = curUserIndex + 2;

                                _phase = Phase.Ended;
                            }
                            else if (curUserIndex == _state[0, 2] && _state[0, 2] == _state[1, 1] && _state[1, 1] == _state[2, 0])
                            {
                                _state[0, 2] = curUserIndex + 2;
                                _state[1, 1] = curUserIndex + 2;
                                _state[2, 0] = curUserIndex + 2;

                                _phase = Phase.Ended;
                            }
                            string reason = "";

                            if (_phase == Phase.Ended) // if user won, stop receiving moves
                            {
                                reason = "Matched three!";
                                _winner = _users[curUserIndex];
                                NadekoBot.Client.MessageReceived -= Client_MessageReceived;
                                OnEnded?.Invoke(this);
                            }
                            else if (IsDraw())
                            {
                                reason = "A draw!";
                                _phase = Phase.Ended;
                                NadekoBot.Client.MessageReceived -= Client_MessageReceived;
                                OnEnded?.Invoke(this);
                            }
                            
                            var sendstate = Task.Run(async () =>
                            {
                                var del1 = msg.DeleteAsync();
                                var del2 = previousMessage?.DeleteAsync();
                                try { previousMessage = await _channel.EmbedAsync(GetEmbed(reason)); } catch { }
                                try { await del1; } catch { }
                                try { await del2; } catch { }
                            });
                            curUserIndex ^= 1;

                            timeoutTimer.Change(15000, Timeout.Infinite);
                        }
                    }
                    finally
                    {
                        moveLock.Release();
                    }
                });

                return Task.CompletedTask;
            }
        }
    }
}