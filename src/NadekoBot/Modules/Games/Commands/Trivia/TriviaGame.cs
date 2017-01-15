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
        private Logger _log { get; }

        public IGuild guild { get; }
        public ITextChannel channel { get; }

        private int QuestionDurationMiliseconds { get; } = 30000;
        private int HintTimeoutMiliseconds { get; } = 6000;
        public bool ShowHints { get; } = true;
        private CancellationTokenSource triviaCancelSource { get; set; }

        public TriviaQuestion CurrentQuestion { get; private set; }
        public HashSet<TriviaQuestion> oldQuestions { get; } = new HashSet<TriviaQuestion>();

        public ConcurrentDictionary<IGuildUser, int> Users { get; } = new ConcurrentDictionary<IGuildUser, int>();

        public bool GameActive { get; private set; } = false;
        public bool ShouldStopGame { get; private set; }

        public int WinRequirement { get; } = 10;

        public TriviaGame(IGuild guild, ITextChannel channel, bool showHints, int winReq)
        {
            this._log = LogManager.GetCurrentClassLogger();

            this.ShowHints = showHints;
            this.guild = guild;
            this.channel = channel;
            this.WinRequirement = winReq;
        }

        public async Task StartGame()
        {
            while (!ShouldStopGame)
            {
                // reset the cancellation source
                triviaCancelSource = new CancellationTokenSource();

                // load question
                CurrentQuestion = TriviaQuestionPool.Instance.GetRandomQuestion(oldQuestions);
                if (CurrentQuestion == null || 
                    string.IsNullOrWhiteSpace(CurrentQuestion.Answer) || 
                    string.IsNullOrWhiteSpace(CurrentQuestion.Question))
                {
                    await channel.SendErrorAsync("Trivia Game", "Failed loading a question.").ConfigureAwait(false);
                    return;
                }
                oldQuestions.Add(CurrentQuestion); //add it to exclusion list so it doesn't show up again

                EmbedBuilder questionEmbed;
                IUserMessage questionMessage;
                try
                {
                    questionEmbed = new EmbedBuilder().WithOkColor()
                        .WithTitle("Trivia Game")
                        .AddField(eab => eab.WithName("Category").WithValue(CurrentQuestion.Category))
                        .AddField(eab => eab.WithName("Question").WithValue(CurrentQuestion.Question));

                    questionMessage = await channel.EmbedAsync(questionEmbed).ConfigureAwait(false);
                }
                catch (HttpException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || 
                                               ex.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                                               ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
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
                        await Task.Delay(HintTimeoutMiliseconds, triviaCancelSource.Token).ConfigureAwait(false);
                        if (ShowHints)
                            try
                            {
                                await questionMessage.ModifyAsync(m => m.Embed = questionEmbed.WithFooter(efb => efb.WithText(CurrentQuestion.GetHint())).Build())
                                    .ConfigureAwait(false);
                            }
                            catch (HttpException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                break;
                            }
                            catch (Exception ex) { _log.Warn(ex); }

                        //timeout
                        await Task.Delay(QuestionDurationMiliseconds - HintTimeoutMiliseconds, triviaCancelSource.Token).ConfigureAwait(false);

                    }
                    catch (TaskCanceledException) { } //means someone guessed the answer
                }
                finally
                {
                    GameActive = false;
                    NadekoBot.Client.MessageReceived -= PotentialGuess;
                }
                if (!triviaCancelSource.IsCancellationRequested)
                    try { await channel.SendErrorAsync("Trivia Game", $"**Time's up!** The correct answer was **{CurrentQuestion.Answer}**").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                await Task.Delay(2000).ConfigureAwait(false);
            }
        }

        public async Task EnsureStopped()
        {
            ShouldStopGame = true;

            await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName("Trivia Game Ended"))
                    .WithTitle("Final Results")
                    .WithDescription(GetLeaderboard())).ConfigureAwait(false);
        }

        public async Task StopGame()
        {
            var old = ShouldStopGame;
            ShouldStopGame = true;
            if (!old)
                try { await channel.SendConfirmAsync("Trivia Game", "Stopping after this question.").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
        }

        private async Task PotentialGuess(SocketMessage imsg)
        {
            try
            {
                if (imsg.Author.IsBot)
                    return;

                var umsg = imsg as SocketUserMessage;
                if (umsg == null)
                    return;

                var textChannel = umsg.Channel as ITextChannel;
                if (textChannel == null || textChannel.Guild != guild)
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
                    try { await channel.SendConfirmAsync("Trivia Game", $"{guildUser.Mention} guessed it and WON the game! The answer was: **{CurrentQuestion.Answer}**").ConfigureAwait(false); } catch { }
                    var reward = NadekoBot.BotConfig.TriviaCurrencyReward;
                    if (reward > 0)
                        await CurrencyHandler.AddCurrencyAsync(guildUser, "Won trivia", reward, true).ConfigureAwait(false);
                    return;
                }
                await channel.SendConfirmAsync("Trivia Game", $"{guildUser.Mention} guessed it! The answer was: **{CurrentQuestion.Answer}**").ConfigureAwait(false);

            }
            catch (Exception ex) { _log.Warn(ex); }
        }

        public string GetLeaderboard()
        {
            if (Users.Count == 0)
                return "No results.";

            var sb = new StringBuilder();

            foreach (var kvp in Users.OrderByDescending(kvp => kvp.Value))
            {
                sb.AppendLine($"**{kvp.Key.Username}** has {kvp.Value} points".ToString().SnPl(kvp.Value));
            }

            return sb.ToString();
        }
    }
}