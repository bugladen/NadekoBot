using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Games.Services;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Core.Common;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class TicTacToeCommands : NadekoSubmodule<GamesService>
        {
            private readonly SemaphoreSlim _sem = new SemaphoreSlim(1, 1);
            private readonly DiscordSocketClient _client;

            public TicTacToeCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NadekoOptions(typeof(TicTacToe.Options))]
            public async Task TicTacToe(params string[] args)
            {
                var (options, _) = OptionsParser.Default.ParseFrom(new TicTacToe.Options(), args);
                var channel = (ITextChannel)Context.Channel;

                await _sem.WaitAsync(1000);
                try
                {
                    if (_service.TicTacToeGames.TryGetValue(channel.Id, out TicTacToe game))
                    {
                        var _ = Task.Run(async () =>
                        {
                            await game.Start((IGuildUser)Context.User);
                        });
                        return;
                    }
                    game = new TicTacToe(base._strings, this._client, channel, (IGuildUser)Context.User, options);
                    _service.TicTacToeGames.Add(channel.Id, game);
                    await ReplyConfirmLocalized("ttt_created").ConfigureAwait(false);

                    game.OnEnded += (g) =>
                    {
                        _service.TicTacToeGames.Remove(channel.Id);
                        _sem.Dispose();
                    };
                }
                finally
                {
                    _sem.Release();
                }
            }
        }
    }
}