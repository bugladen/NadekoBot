using Discord.Commands;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Games.Common.Hangman;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class HangmanCommands : NadekoSubmodule<GamesService>
        {
            private readonly DiscordSocketClient _client;

            public HangmanCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Hangmanlist()
            {
                await Context.Channel.SendConfirmAsync(Format.Code(GetText("hangman_types", Prefix)) + "\n" + string.Join("\n", _service.TermPool.Data.Keys));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Hangman([Remainder]string type = "random")
            {
                var hm = new Hangman(type, _service.TermPool);

                if (!_service.HangmanGames.TryAdd(Context.Channel.Id, hm))
                {
                    hm.Dispose();
                    await ReplyErrorLocalized("hangman_running").ConfigureAwait(false);
                    return;
                }
                hm.OnGameEnded += Hm_OnGameEnded;
                hm.OnGuessFailed += Hm_OnGuessFailed;
                hm.OnGuessSucceeded += Hm_OnGuessSucceeded;
                hm.OnLetterAlreadyUsed += Hm_OnLetterAlreadyUsed;
                _client.MessageReceived += _client_MessageReceived;

                try
                {
                    await Context.Channel.SendConfirmAsync(GetText("hangman_game_started") + $" ({hm.TermType})",
                        hm.ScrambledWord + "\n" + hm.GetHangman())
                        .ConfigureAwait(false);
                }
                catch { }

                await hm.EndedTask.ConfigureAwait(false);

                _client.MessageReceived -= _client_MessageReceived;
                _service.HangmanGames.TryRemove(Context.Channel.Id, out _);
                hm.Dispose();

                Task _client_MessageReceived(SocketMessage msg)
                {
                    var _ = Task.Run(() =>
                    {
                        if (Context.Channel.Id == msg.Channel.Id)
                            return hm.Input(msg.Author.Id, msg.Author.ToString(), msg.Content);
                        else
                            return Task.CompletedTask;
                    });
                    return Task.CompletedTask;
                }
            }

            Task Hm_OnGameEnded(Hangman game, string winner)
            {
                if (winner == null)
                {
                    var loseEmbed = new EmbedBuilder().WithTitle($"Hangman Game ({game.TermType}) - Ended")
                                             .WithDescription(Format.Bold("You lose."))
                                             .AddField(efb => efb.WithName("It was").WithValue(game.Term.Word.ToTitleCase()))
                                             .WithFooter(efb => efb.WithText(string.Join(" ", game.PreviousGuesses)))
                                             .WithErrorColor();

                    if (Uri.IsWellFormedUriString(game.Term.ImageUrl, UriKind.Absolute))
                        loseEmbed.WithImageUrl(game.Term.ImageUrl);

                    return Context.Channel.EmbedAsync(loseEmbed);
                }

                var winEmbed = new EmbedBuilder().WithTitle($"Hangman Game ({game.TermType}) - Ended")
                                             .WithDescription(Format.Bold($"{winner} Won."))
                                             .AddField(efb => efb.WithName("It was").WithValue(game.Term.Word.ToTitleCase()))
                                             .WithFooter(efb => efb.WithText(string.Join(" ", game.PreviousGuesses)))
                                             .WithOkColor();

                if (Uri.IsWellFormedUriString(game.Term.ImageUrl, UriKind.Absolute))
                    winEmbed.WithImageUrl(game.Term.ImageUrl);

                return Context.Channel.EmbedAsync(winEmbed);
            }

            private Task Hm_OnLetterAlreadyUsed(Hangman game, string user, char guess)
            {
                return Context.Channel.SendErrorAsync($"Hangman Game ({game.TermType})", $"{user} Letter `{guess}` has already been used. You can guess again in 3 seconds.\n" + game.ScrambledWord + "\n" + game.GetHangman(),
                                    footer: string.Join(" ", game.PreviousGuesses));
            }

            private Task Hm_OnGuessSucceeded(Hangman game, string user, char guess)
            {
                return Context.Channel.SendConfirmAsync($"Hangman Game ({game.TermType})", $"{user} guessed a letter `{guess}`!\n" + game.ScrambledWord + "\n" + game.GetHangman(),
                    footer: string.Join(" ", game.PreviousGuesses));
            }

            private Task Hm_OnGuessFailed(Hangman game, string user, char guess)
            {
                return Context.Channel.SendErrorAsync($"Hangman Game ({game.TermType})", $"{user} Letter `{guess}` does not exist. You can guess again in 3 seconds.\n" + game.ScrambledWord + "\n" + game.GetHangman(),
                                    footer: string.Join(" ", game.PreviousGuesses));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task HangmanStop()
            {
                if (_service.HangmanGames.TryRemove(Context.Channel.Id, out var removed))
                {
                    await removed.Stop().ConfigureAwait(false);
                    await ReplyConfirmLocalized("hangman_stopped").ConfigureAwait(false);
                }
            }
        }
    }
}