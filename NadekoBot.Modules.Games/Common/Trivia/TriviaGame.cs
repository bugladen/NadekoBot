using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Impl;
using NLog;

namespace NadekoBot.Modules.Games.Common.Trivia
{
    public class TriviaGame
    {
        private readonly SemaphoreSlim _guessLock = new SemaphoreSlim(1, 1);
        private readonly Logger _log;
        private readonly NadekoStrings _strings;
        private readonly DiscordSocketClient _client;
        private readonly IBotConfigProvider _bc;
        private readonly CurrencyService _cs;

        public IGuild Guild { get; }
        public ITextChannel Channel { get; }

        private readonly int _questionDurationMiliseconds = 30000;
        private readonly int _hintTimeoutMiliseconds = 6000;
        public bool ShowHints { get; }
        public bool IsPokemon { get; }
        private CancellationTokenSource _triviaCancelSource;

        public TriviaQuestion CurrentQuestion { get; private set; }
        public HashSet<TriviaQuestion> OldQuestions { get; } = new HashSet<TriviaQuestion>();

        public ConcurrentDictionary<IGuildUser, int> Users { get; } = new ConcurrentDictionary<IGuildUser, int>();

        public bool GameActive { get; private set; }
        public bool ShouldStopGame { get; private set; }

        public int WinRequirement { get; }

        public TriviaGame(NadekoStrings strings, DiscordSocketClient client, IBotConfigProvider bc,
            CurrencyService cs, IGuild guild, ITextChannel channel,
            bool showHints, int winReq, bool isPokemon)
        {
            _log = LogManager.GetCurrentClassLogger();
            _strings = strings;
            _client = client;
            _bc = bc;
            _cs = cs;

            ShowHints = showHints;
            Guild = guild;
            Channel = channel;
            WinRequirement = winReq;
            IsPokemon = isPokemon;
        }

        private string GetText(string key, params object[] replacements) =>
            _strings.GetText(key,
                Channel.GuildId,
                typeof(Games).Name.ToLowerInvariant(),
                replacements);

        public async Task StartGame()
        {
            while (!ShouldStopGame)
            {
                // reset the cancellation source
                _triviaCancelSource = new CancellationTokenSource();

                // load question
                CurrentQuestion = TriviaQuestionPool.Instance.GetRandomQuestion(OldQuestions, IsPokemon);
                if (string.IsNullOrWhiteSpace(CurrentQuestion?.Answer) || string.IsNullOrWhiteSpace(CurrentQuestion.Question))
                {
                    await Channel.SendErrorAsync(GetText("trivia_game"), GetText("failed_loading_question")).ConfigureAwait(false);
                    return;
                }
                OldQuestions.Add(CurrentQuestion); //add it to exclusion list so it doesn't show up again

                EmbedBuilder questionEmbed;
                IUserMessage questionMessage;
                try
                {
                    questionEmbed = new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("trivia_game"))
                        .AddField(eab => eab.WithName(GetText("category")).WithValue(CurrentQuestion.Category))
                        .AddField(eab => eab.WithName(GetText("question")).WithValue(CurrentQuestion.Question));
                    if (Uri.IsWellFormedUriString(CurrentQuestion.ImageUrl, UriKind.Absolute))
                        questionEmbed.WithImageUrl(CurrentQuestion.ImageUrl);

                    questionMessage = await Channel.EmbedAsync(questionEmbed).ConfigureAwait(false);
                }
                catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound ||
                                               ex.HttpCode == System.Net.HttpStatusCode.Forbidden ||
                                               ex.HttpCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await Task.Delay(2000).ConfigureAwait(false);
                    continue;
                }

                //receive messages
                try
                {
                    _client.MessageReceived += PotentialGuess;

                    //allow people to guess
                    GameActive = true;
                    try
                    {
                        //hint
                        await Task.Delay(_hintTimeoutMiliseconds, _triviaCancelSource.Token).ConfigureAwait(false);
                        if (ShowHints)
                            try
                            {
                                await questionMessage.ModifyAsync(m => m.Embed = questionEmbed.WithFooter(efb => efb.WithText(CurrentQuestion.GetHint())).Build())
                                    .ConfigureAwait(false);
                            }
                            catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound || ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                break;
                            }
                            catch (Exception ex) { _log.Warn(ex); }

                        //timeout
                        await Task.Delay(_questionDurationMiliseconds - _hintTimeoutMiliseconds, _triviaCancelSource.Token).ConfigureAwait(false);

                    }
                    catch (TaskCanceledException) { } //means someone guessed the answer
                }
                finally
                {
                    GameActive = false;
                    _client.MessageReceived -= PotentialGuess;
                }
                if (!_triviaCancelSource.IsCancellationRequested)
                {
                    try
                    {
                        var embed = new EmbedBuilder().WithErrorColor()
                            .WithTitle(GetText("trivia_game"))
                            .WithDescription(GetText("trivia_times_up", Format.Bold(CurrentQuestion.Answer)));
                        if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                            embed.WithImageUrl(CurrentQuestion.AnswerImageUrl);

                        await Channel.EmbedAsync(embed).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        public async Task EnsureStopped()
        {
            ShouldStopGame = true;

            await Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName("Trivia Game Ended"))
                    .WithTitle("Final Results")
                    .WithDescription(GetLeaderboard())).ConfigureAwait(false);
        }

        public async Task StopGame()
        {
            var old = ShouldStopGame;
            ShouldStopGame = true;
            if (!old)
                try { await Channel.SendConfirmAsync(GetText("trivia_game"), GetText("trivia_stopping")).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
        }

        private Task PotentialGuess(SocketMessage imsg)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (imsg.Author.IsBot)
                        return;

                    var umsg = imsg as SocketUserMessage;

                    var textChannel = umsg?.Channel as ITextChannel;
                    if (textChannel == null || textChannel.Guild != Guild)
                        return;

                    var guildUser = (IGuildUser)umsg.Author;

                    var guess = false;
                    await _guessLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        if (GameActive && CurrentQuestion.IsAnswerCorrect(umsg.Content) && !_triviaCancelSource.IsCancellationRequested)
                        {
                            Users.AddOrUpdate(guildUser, 1, (gu, old) => ++old);
                            guess = true;
                        }
                    }
                    finally { _guessLock.Release(); }
                    if (!guess) return;
                    _triviaCancelSource.Cancel();


                    if (Users[guildUser] == WinRequirement)
                    {
                        ShouldStopGame = true;
                        try
                        {
                            var embedS = new EmbedBuilder().WithOkColor()
                                .WithTitle(GetText("trivia_game"))
                                .WithDescription(GetText("trivia_win",
                                    guildUser.Mention,
                                    Format.Bold(CurrentQuestion.Answer)));
                            if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                                embedS.WithImageUrl(CurrentQuestion.AnswerImageUrl);
                            await Channel.EmbedAsync(embedS).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                        var reward = _bc.BotConfig.TriviaCurrencyReward;
                        if (reward > 0)
                            await _cs.AddAsync(guildUser, "Won trivia", reward, true).ConfigureAwait(false);
                        return;
                    }
                    var embed = new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("trivia_game"))
                        .WithDescription(GetText("trivia_guess", guildUser.Mention, Format.Bold(CurrentQuestion.Answer)));
                    if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                        embed.WithImageUrl(CurrentQuestion.AnswerImageUrl);
                    await Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            });
            return Task.CompletedTask;
        }

        public string GetLeaderboard()
        {
            if (Users.Count == 0)
                return GetText("no_results");

            var sb = new StringBuilder();

            foreach (var kvp in Users.OrderByDescending(kvp => kvp.Value))
            {
                sb.AppendLine(GetText("trivia_points", Format.Bold(kvp.Key.ToString()), kvp.Value).SnPl(kvp.Value));
            }

            return sb.ToString();
        }
    }
}