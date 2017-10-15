using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Common.Connect4;
using NadekoBot.Modules.Games.Services;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class Connect4Commands : NadekoSubmodule<GamesService>
        {
            private readonly DiscordSocketClient _client;
            
            private readonly string[] numbers = new string[] { ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:"};

            public Connect4Commands(DiscordSocketClient client)
            {
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Connect4()
            {
                var newGame = new Connect4Game(Context.User.Id, Context.User.ToString());
                Connect4Game game;
                if ((game = _service.Connect4Games.GetOrAdd(Context.Channel.Id, newGame)) != newGame)
                {
                    if (game.CurrentPhase != Connect4Game.Phase.Joining)
                        return;

                    newGame.Dispose();
                    //means game already exists, try to join
                    var joined = await game.Join(Context.User.Id, Context.User.ToString()).ConfigureAwait(false);
                    return;
                }

                game.OnGameStateUpdated += Game_OnGameStateUpdated;
                game.OnGameFailedToStart += Game_OnGameFailedToStart;
                game.OnGameEnded += Game_OnGameEnded;
                _client.MessageReceived += _client_MessageReceived;

                game.Initialize();

                await ReplyConfirmLocalized("connect4_created").ConfigureAwait(false);

                Task _client_MessageReceived(SocketMessage arg)
                {
                    if (Context.Channel.Id != arg.Channel.Id)
                        return Task.CompletedTask;

                    var _ = Task.Run(async () =>
                    {
                        bool success = false;
                        if (int.TryParse(arg.Content, out var col))
                        {
                            success = await game.Input(arg.Author.Id, arg.Author.ToString(), col).ConfigureAwait(false);
                        }

                        if (success)
                            try { await arg.DeleteAsync().ConfigureAwait(false); } catch { }
                        else
                        {
                            if (game.CurrentPhase == Connect4Game.Phase.Joining
                                || game.CurrentPhase == Connect4Game.Phase.Ended)
                            {
                                return;
                            }
                            RepostCounter++;
                            if (RepostCounter == 0)
                                try { msg = await Context.Channel.SendMessageAsync("", embed: (Embed)msg.Embeds.First()); } catch { }
                        }
                    });
                    return Task.CompletedTask;
                }

                Task Game_OnGameFailedToStart(Connect4Game arg)
                {
                    if (_service.Connect4Games.TryRemove(Context.Channel.Id, out var toDispose))
                    {
                        _client.MessageReceived -= _client_MessageReceived;
                        toDispose.Dispose();
                    }
                    return ErrorLocalized("connect4_failed_to_start");
                }

                Task Game_OnGameEnded(Connect4Game arg, Connect4Game.Result result)
                {
                    if (_service.Connect4Games.TryRemove(Context.Channel.Id, out var toDispose))
                    {
                        _client.MessageReceived -= _client_MessageReceived;
                        toDispose.Dispose();
                    }

                    string title;
                    if (result == Connect4Game.Result.CurrentPlayerWon)
                    {
                        title = GetText("connect4_won", Format.Bold(arg.CurrentPlayer), Format.Bold(arg.OtherPlayer));
                    }
                    else if (result == Connect4Game.Result.OtherPlayerWon)
                    {
                        title = GetText("connect4_won", Format.Bold(arg.OtherPlayer), Format.Bold(arg.CurrentPlayer));
                    }
                    else
                        title = GetText("connect4_draw");

                    return msg.ModifyAsync(x => x.Embed = new EmbedBuilder()
                        .WithTitle(title)
                        .WithDescription(GetGameStateText(game))
                        .WithOkColor()
                        .Build());
                }
            }

            private IUserMessage msg;

            private int _repostCounter = 0;
            private int RepostCounter
            {
                get => _repostCounter;
                set
                {
                    if (value < 0 || value > 7)
                        _repostCounter = 0;
                    else _repostCounter = value;
                }
            }

            private async Task Game_OnGameStateUpdated(Connect4Game game)
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"{game.CurrentPlayer} vs {game.OtherPlayer}")
                    .WithDescription(GetGameStateText(game))
                    .WithOkColor();


                if (msg == null)
                    msg = await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                else
                    await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
            }

            private string GetGameStateText(Connect4Game game)
            {
                var sb = new StringBuilder();

                if (game.CurrentPhase == Connect4Game.Phase.P1Move ||
                    game.CurrentPhase == Connect4Game.Phase.P2Move)
                    sb.AppendLine(GetText("connect4_player_to_move", Format.Bold(game.CurrentPlayer)));

                for (int i = Connect4Game.NumberOfRows; i > 0; i--)
                {
                    for (int j = 0; j < Connect4Game.NumberOfColumns; j++)
                    {
                        //Console.WriteLine(i + (j * Connect4Game.NumberOfRows) - 1);
                        var cur = game.GameState[i + (j * Connect4Game.NumberOfRows) - 1];

                        if (cur == Connect4Game.Field.Empty)
                            sb.Append("⚫"); //black circle
                        else if (cur == Connect4Game.Field.P1)
                            sb.Append("🔴"); //red circle
                        else
                            sb.Append("🔵"); //blue circle
                    }
                    sb.AppendLine();
                }

                for (int i = 0; i < Connect4Game.NumberOfColumns; i++)
                {
                    sb.Append(/*new string(' ', 1 + ((i + 1) / 2)) + */numbers[i]);
                }
                return sb.ToString();
            }
        }
    }
}
