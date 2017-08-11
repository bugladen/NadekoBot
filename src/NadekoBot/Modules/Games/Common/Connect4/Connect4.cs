using NadekoBot.Common;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Common.Connect4
{
    public class Connect4Game : IDisposable
    {
        public enum Phase
        {
            Joining, // waiting for second player to join
            P1Move,
            P2Move,
            Ended,
        }

        public enum Field //temporary most likely
        {
            Empty,
            P1,
            P2,
        }

        public enum Result
        {
            Draw,
            CurrentPlayerWon,
            OtherPlayerWon,
        }

        public const int NumberOfColumns = 6;
        public const int NumberOfRows = 7;

        public Phase CurrentPhase { get; private set; } = Phase.Joining;

        //state is bottom to top, left to right
        private readonly Field[] _gameState = new Field[NumberOfRows * NumberOfColumns];
        private readonly (ulong UserId, string Username)?[] _players = new(ulong, string)?[2];

        public ImmutableArray<Field> GameState => _gameState.ToImmutableArray();
        public ImmutableArray<(ulong UserId, string Username)?> Players => _players.ToImmutableArray();

        public string CurrentPlayer => CurrentPhase == Phase.P1Move
            ? _players[0].Value.Username
            : _players[1].Value.Username;

        public string OtherPlayer => CurrentPhase == Phase.P2Move
            ? _players[0].Value.Username
            : _players[1].Value.Username;

        //public event Func<Connect4Game, Task> OnGameStarted;
        public event Func<Connect4Game, Task> OnGameStateUpdated;
        public event Func<Connect4Game, Task> OnGameFailedToStart;
        public event Func<Connect4Game, Result, Task> OnGameEnded;

        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
        private readonly NadekoRandom _rng;

        private Timer _playerTimeoutTimer;

        /* [ ][ ][ ][ ][ ][ ]
         * [ ][ ][ ][ ][ ][ ]
         * [ ][ ][ ][ ][ ][ ]
         * [ ][ ][ ][ ][ ][ ]
         * [ ][ ][ ][ ][ ][ ]
         * [ ][ ][ ][ ][ ][ ]
         * [ ][ ][ ][ ][ ][ ]
         */

        public Connect4Game(ulong userId, string userName)
        {
            _players[0] = (userId, userName);

            _rng = new NadekoRandom();
            for (int i = 0; i < NumberOfColumns * NumberOfRows; i++)
            {
                _gameState[i] = Field.Empty;
            }
        }

        public void Initialize()
        {
            if (CurrentPhase != Phase.Joining)
                return;
            var _ = Task.Run(async () =>
            {
                await Task.Delay(15000).ConfigureAwait(false);
                await _locker.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_players[1] == null)
                    {
                        var __ = OnGameFailedToStart?.Invoke(this);
                        CurrentPhase = Phase.Ended;
                        return;
                    }
                }
                finally { _locker.Release(); }
            });
        }

        public async Task<bool> Join(ulong userId, string userName)
        {
            await _locker.WaitAsync().ConfigureAwait(false);
            try
            {
                if (CurrentPhase != Phase.Joining) //can't join if its not a joining phase
                    return false;

                if (_players[0].Value.UserId == userId) // same user can't join own game
                    return false;

                if (_rng.Next(0, 2) == 0) //rolling from 0-1, if number is 0, join as first player
                {
                    _players[1] = _players[0];
                    _players[0] = (userId, userName);
                }
                else //else join as a second player
                    _players[1] = (userId, userName);

                CurrentPhase = Phase.P1Move; //start the game
                _playerTimeoutTimer = new Timer(async state =>
                {
                    await _locker.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        EndGame(Result.OtherPlayerWon);
                    }
                    finally { _locker.Release(); }
                }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
                var __ = OnGameStateUpdated?.Invoke(this);

                return true;
            }
            finally { _locker.Release(); }
        }

        public async Task<bool> Input(ulong userId, string userName, int inputCol)
        {
            await _locker.WaitAsync().ConfigureAwait(false);
            try
            {
                inputCol -= 1;
                if (CurrentPhase == Phase.Ended || CurrentPhase == Phase.Joining)
                    return false;

                if (!((_players[0].Value.UserId == userId && CurrentPhase == Phase.P1Move)
                    || (_players[1].Value.UserId == userId && CurrentPhase == Phase.P2Move)))
                    return false;

                if (inputCol < 0 || inputCol > NumberOfColumns) //invalid input
                    return false;

                if (IsColumnFull(inputCol)) //can't play there event?
                    return false;

                var start = NumberOfRows * inputCol;
                for (int i = start; i < start + NumberOfRows; i++)
                {
                    if (_gameState[i] == Field.Empty)
                    {
                        _gameState[i] = GetPlayerPiece(userId);
                        break;
                    }
                }

                //check winnning condition
                // ok, i'll go from [0-2] in rows (and through all columns) and check upward if 4 are connected
                
                for (int i = 0; i < NumberOfRows - 3; i++)
                {
                    if (CurrentPhase == Phase.Ended)
                        break;

                    for (int j = 0; j < NumberOfColumns; j++)
                    {
                        if (CurrentPhase == Phase.Ended)
                            break;

                        var first = _gameState[i + j * NumberOfRows];
                        if (first != Field.Empty)
                        {
                            //Console.WriteLine(i + j * NumberOfRows);
                            for (int k = 1; k < 4; k++)
                            {
                                var next = _gameState[i + k + j * NumberOfRows];
                                if (next == first)
                                {
                                    //Console.WriteLine(i + k + j * NumberOfRows);
                                    if (k == 3)
                                        EndGame(Result.CurrentPlayerWon);
                                    else
                                        continue;
                                }
                                else break;
                            }
                        }
                    }
                }

                // i'll go [0-1] in columns (and through all rows) and check to the right if 4 are connected
                for (int i = 0; i < NumberOfColumns - 3; i++)
                {
                    if (CurrentPhase == Phase.Ended)
                        break;

                    for (int j = 0; j < NumberOfRows; j++)
                    {
                        if (CurrentPhase == Phase.Ended)
                            break;

                        var first = _gameState[j + i * NumberOfRows];
                        if (first != Field.Empty)
                        {
                            for (int k = 1; k < 4; k++)
                            {
                                var next = _gameState[j + (i + k) * NumberOfRows];
                                if (next == first)
                                    if (k == 3)
                                        EndGame(Result.CurrentPlayerWon);
                                    else
                                        continue;
                                else break;
                            }
                        }
                    }
                }

                //need to check diagonal now
                for (int col = 0; col < NumberOfColumns; col++)
                {
                    if (CurrentPhase == Phase.Ended)
                        break;

                    for (int row = 0; row < NumberOfRows; row++)
                    {
                        if (CurrentPhase == Phase.Ended)
                            break;

                        var first = _gameState[row + col * NumberOfRows];

                        if (first != Field.Empty)
                        {
                            var same = 1;

                            //top left
                            for (int i = 1; i < 4; i++)
                            {
                                //while going top left, rows are increasing, columns are decreasing
                                var curRow = row + i;
                                var curCol = col - i;

                                //check if current values are in range
                                if (curRow >= NumberOfRows || curRow < 0)
                                    break;
                                if (curCol < 0 || curCol >= NumberOfColumns)
                                    break;

                                var cur = _gameState[curRow + curCol * NumberOfRows];
                                if (cur == first)
                                    same++;
                                else break;
                            }

                            if (same == 4)
                            {
                                Console.WriteLine($"Won top left diagonal starting from {row + col * NumberOfRows}");
                                EndGame(Result.CurrentPlayerWon);
                                break;
                            }

                            same = 1;

                            //top right
                            for (int i = 1; i < 4; i++)
                            {
                                //while going top right, rows are increasing, columns are increasing
                                var curRow = row + i;
                                var curCol = col + i;

                                //check if current values are in range
                                if (curRow >= NumberOfRows || curRow < 0)
                                    break;
                                if (curCol < 0 || curCol >= NumberOfColumns)
                                    break;

                                var cur = _gameState[curRow + curCol * NumberOfRows];
                                if (cur == first)
                                    same++;
                                else break;
                            }

                            if (same == 4)
                            {
                                Console.WriteLine($"Won top right diagonal starting from {row + col * NumberOfRows}");
                                EndGame(Result.CurrentPlayerWon);
                                break;
                            }
                        }
                    }
                }

                //check draw? if it's even possible
                if (_gameState.All(x => x != Field.Empty))
                {
                    EndGame(Result.Draw);
                }

                if (CurrentPhase != Phase.Ended)
                {
                    if (CurrentPhase == Phase.P1Move)
                        CurrentPhase = Phase.P2Move;
                    else
                        CurrentPhase = Phase.P1Move;

                    ResetTimer();
                }
                var _ = OnGameStateUpdated?.Invoke(this);
                return true;
            }
            finally { _locker.Release(); }
        }

        private void ResetTimer()
        {
            _playerTimeoutTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private void EndGame(Result result)
        {
            if (CurrentPhase == Phase.Ended)
                return;
            var _ = OnGameEnded?.Invoke(this, result);
            CurrentPhase = Phase.Ended;
        }

        private Field GetPlayerPiece(ulong userId) => _players[0].Value.UserId == userId
            ? Field.P1
            : Field.P2;

        //column is full if there are no empty fields
        private bool IsColumnFull(int column)
        {
            var start = NumberOfRows * column;
            for (int i = start; i < start + NumberOfRows; i++)
            {
                if (_gameState[i] == Field.Empty)
                    return false;
            }
            return true;
        }

        public void Dispose()
        {
            OnGameFailedToStart = null;
            OnGameStateUpdated = null;
            OnGameEnded = null;
            _playerTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}
