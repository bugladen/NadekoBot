using Discord;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Trivia
{
    public class TriviaGame
    {
        private readonly SemaphoreSlim _guessLock = new SemaphoreSlim(1, 1);
        private readonly Logger _log;

        public IGuild Guild { get; }
        public ITextChannel Channel { get; }

        private int questionDurationMiliseconds { get; } = 30000;
        private int hintTimeoutMiliseconds { get; } = 6000;
        public bool ShowHints { get; }
        public bool IsPokemon { get; }
        private CancellationTokenSource triviaCancelSource { get; set; }

        public TriviaQuestion CurrentQuestion { get; private set; }
        public HashSet<TriviaQuestion> OldQuestions { get; } = new HashSet<TriviaQuestion>();

        public ConcurrentDictionary<IGuildUser, int> Users { get; } = new ConcurrentDictionary<IGuildUser, int>();

        public bool GameActive { get; private set; }
        public bool ShouldStopGame { get; private set; }

        public int WinRequirement { get; }

        public TriviaGame(IGuild guild, ITextChannel channel, bool showHints, int winReq, bool isPokemon)
        {
            _log = LogManager.GetCurrentClassLogger();

            ShowHints = showHints;
            Guild = guild;
            Channel = channel;
            WinRequirement = winReq;
            IsPokemon = isPokemon;
        }

        private string GetText(string key, params object[] replacements) =>
            NadekoTopLevelModule.GetTextStatic(key,
                NadekoBot.Localization.GetCultureInfo(Channel.GuildId),
                typeof(Games).Name.ToLowerInvariant(),
                replacements);

        public async Task StartGame()
        {
            while (!ShouldStopGame)
            {
                // reset the cancellation source
                triviaCancelSource = new CancellationTokenSource();

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
                        .AddField(eab => eab.WithName(GetText("question")).WithValue(CurrentQuestion.Question))
                        .WithImageUrl(CurrentQuestion.ImageUrl);

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
                    NadekoBot.Client.MessageReceived += PotentialGuess;

                    //allow people to guess
                    GameActive = true;
                    try
                    {
                        //hint
                        await Task.Delay(hintTimeoutMiliseconds, triviaCancelSource.Token).ConfigureAwait(false);
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
                        await Task.Delay(questionDurationMiliseconds - hintTimeoutMiliseconds, triviaCancelSource.Token).ConfigureAwait(false);

                    }
                    catch (TaskCanceledException) { } //means someone guessed the answer
                }
                finally
                {
                    GameActive = false;
                    NadekoBot.Client.MessageReceived -= PotentialGuess;
                }
                if (!triviaCancelSource.IsCancellationRequested)
                {
                    try
                    {
                        await Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                            .WithTitle(GetText("trivia_game"))
                            .WithDescription(GetText("trivia_times_up", Format.Bold(CurrentQuestion.Answer)))
                            .WithImageUrl(CurrentQuestion.AnswerImageUrl))
                            .ConfigureAwait(false);
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

        private async Task PotentialGuess(SocketMessage imsg)
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
                    if (GameActive && CurrentQuestion.IsAnswerCorrect(umsg.Content) && !triviaCancelSource.IsCancellationRequested)
                    {
                        Users.AddOrUpdate(guildUser, 1, (gu, old) => ++old);
                        guess = true;
                    }
                }
                finally { _guessLock.Release(); }
                if (!guess) return;
                triviaCancelSource.Cancel();


                if (Users[guildUser] == WinRequirement)
                {
                    ShouldStopGame = true;
                    try
                    {
                        await Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithTitle(GetText("trivia_game"))
                            .WithDescription(GetText("trivia_win",
                                guildUser.Mention,
                                Format.Bold(CurrentQuestion.Answer)))
                            .WithImageUrl(CurrentQuestion.AnswerImageUrl))
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                    var reward = NadekoBot.BotConfig.TriviaCurrencyReward;
                    if (reward > 0)
                        await CurrencyHandler.AddCurrencyAsync(guildUser, "Won trivia", reward, true).ConfigureAwait(false);
                    return;
                }

                await Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("trivia_game"))
                    .WithDescription(GetText("trivia_guess", guildUser.Mention, Format.Bold(CurrentQuestion.Answer)))
                    .WithImageUrl(CurrentQuestion.AnswerImageUrl))
                    .ConfigureAwait(false);
            }
            catch (Exception ex) { _log.Warn(ex); }
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