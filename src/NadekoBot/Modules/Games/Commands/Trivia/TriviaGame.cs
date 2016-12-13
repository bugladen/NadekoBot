using Discord;
using Discord.Net;
using NadekoBot.Extensions;
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
        public bool ShowHints { get; set; } = true;
        private CancellationTokenSource triviaCancelSource { get; set; }

        public TriviaQuestion CurrentQuestion { get; private set; }
        public HashSet<TriviaQuestion> oldQuestions { get; } = new HashSet<TriviaQuestion>();

        public ConcurrentDictionary<IGuildUser, int> Users { get; } = new ConcurrentDictionary<IGuildUser, int>();

        public bool GameActive { get; private set; } = false;
        public bool ShouldStopGame { get; private set; }

        public int WinRequirement { get; } = 10;

        public TriviaGame(IGuild guild, ITextChannel channel, bool showHints, int winReq = 10)
        {
            _log = LogManager.GetCurrentClassLogger();
            ShowHints = showHints;
            this.guild = guild;
            this.channel = channel;
            WinRequirement = winReq;
            Task.Run(async () => { try { await StartGame().ConfigureAwait(false); } catch { } });
        }

        private async Task StartGame()
        {
            while (!ShouldStopGame)
            {
                // reset the cancellation source
                triviaCancelSource = new CancellationTokenSource();
                var token = triviaCancelSource.Token;
                // load question
                CurrentQuestion = TriviaQuestionPool.Instance.GetRandomQuestion(oldQuestions);
                if (CurrentQuestion == null)
                {
                    try { await channel.SendErrorAsync($":exclamation: Failed loading a trivia question.").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                    await End().ConfigureAwait(false);
                    return;
                }
                oldQuestions.Add(CurrentQuestion); //add it to exclusion list so it doesn't show up again
                                                   //sendquestion
                try { await channel.SendConfirmAsync($":question: Question",$"**{CurrentQuestion.Question}**").ConfigureAwait(false); }
                catch (HttpException ex) when (ex.StatusCode  == System.Net.HttpStatusCode.NotFound || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    break;
                }
                catch (Exception ex) { _log.Warn(ex); }

                //receive messages
                NadekoBot.Client.MessageReceived += PotentialGuess;

                //allow people to guess
                GameActive = true;

                try
                {
                    //hint
                    await Task.Delay(HintTimeoutMiliseconds, token).ConfigureAwait(false);
                    if (ShowHints)
                        try { await channel.SendConfirmAsync($":exclamation: Hint", CurrentQuestion.GetHint()).ConfigureAwait(false); }
                        catch (HttpException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            break;
                        }
                        catch (Exception ex) { _log.Warn(ex); }

                    //timeout
                    await Task.Delay(QuestionDurationMiliseconds - HintTimeoutMiliseconds, token).ConfigureAwait(false);

                }
                catch (TaskCanceledException) { } //means someone guessed the answer
                GameActive = false;
                if (!triviaCancelSource.IsCancellationRequested)
                    try { await channel.SendConfirmAsync($":clock2: :question: **Time's up!** The correct answer was **{CurrentQuestion.Answer}**").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                NadekoBot.Client.MessageReceived -= PotentialGuess;
                // load next question if game is still running
                await Task.Delay(2000).ConfigureAwait(false);
            }
            try { NadekoBot.Client.MessageReceived -= PotentialGuess; } catch { }
            GameActive = false;
            await End().ConfigureAwait(false);
        }

        public async Task End()
        {
            ShouldStopGame = true;
            TriviaGame throwaway;
            Games.TriviaCommands.RunningTrivias.TryRemove(channel.Guild.Id, out throwaway);
            try
            {
                await channel.EmbedAsync(new EmbedBuilder().WithColor(NadekoBot.OkColor)
                      .WithTitle("Leaderboard")
                      .WithDescription(GetLeaderboard())
                      .Build(), "Trivia game ended.").ConfigureAwait(false);
            }
            catch { }
        }

        public async Task StopGame()
        {
            if (!ShouldStopGame)
                try { await channel.SendConfirmAsync(":exclamation: Trivia will stop after this question.").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
            ShouldStopGame = true;
        }

        private Task PotentialGuess(IMessage imsg)
        {
            if (imsg.Author.IsBot)
                return Task.CompletedTask;
            var umsg = imsg as IUserMessage;
            if (umsg == null)
                return Task.CompletedTask;
            var t = Task.Run(async () =>
            {
                try
                {
                    if (!(umsg.Channel is IGuildChannel && umsg.Channel is ITextChannel)) return;
                    if ((umsg.Channel as ITextChannel).Guild != guild) return;
                    if (umsg.Author.Id == NadekoBot.Client.GetCurrentUser().Id) return;

                    var guildUser = umsg.Author as IGuildUser;

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
                    try { await channel.SendConfirmAsync($"☑️ {guildUser.Mention} guessed it! The answer was: **{CurrentQuestion.Answer}**").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                    if (Users[guildUser] != WinRequirement) return;
                    ShouldStopGame = true;
                    await channel.SendConfirmAsync($":exclamation: We have a winner! It's {guildUser.Mention}.").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            });
            return Task.CompletedTask;
        }

        public string GetLeaderboard()
        {
            if (Users.Count == 0)
                return "";

            var sb = new StringBuilder();

            foreach (var kvp in Users.OrderByDescending(kvp => kvp.Value))
            {
                sb.AppendLine($"**{kvp.Key.Username}** has {kvp.Value} points".ToString().SnPl(kvp.Value));
            }

            return sb.ToString();
        }
    }
}
