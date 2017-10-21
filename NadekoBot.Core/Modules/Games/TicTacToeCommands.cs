using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Modules.Games.Services;
using NadekoBot.Modules.Games.Common;

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
            public async Task TicTacToe()
            {
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
                    game = new TicTacToe(base._strings, this._client, channel, (IGuildUser)Context.User);
                    _service.TicTacToeGames.Add(channel.Id, game);
                    await ReplyConfirmLocalized("ttt_created").ConfigureAwait(false);

                    game.OnEnded += (g) =>
                    {
                        _service.TicTacToeGames.Remove(channel.Id);
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