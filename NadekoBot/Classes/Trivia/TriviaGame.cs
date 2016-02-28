using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Classes.Trivia {
    class TriviaGame {
        private readonly object _guessLock = new object();

        private Server _server { get; }
        private Channel _channel { get; }

        private int QuestionDurationMiliseconds { get; } = 30000;
        private int HintTimeoutMiliseconds { get; } = 6000;
        private CancellationTokenSource triviaCancelSource { get; set; }

        public TriviaQuestion CurrentQuestion { get; private set; }
        public List<TriviaQuestion> oldQuestions { get; } = new List<TriviaQuestion>();

        public ConcurrentDictionary<User, int> users { get; } = new ConcurrentDictionary<User, int>();

        public bool GameActive { get; private set; } = false;
        public bool ShouldStopGame { get; private set; }

        public int WinRequirement { get; } = 10;

        public TriviaGame(CommandEventArgs e) {
            _server = e.Server;
            _channel = e.Channel;
            Task.Run(() => StartGame());
        }

        private async Task StartGame() {
            while (!ShouldStopGame) {
                // reset the cancellation source
                triviaCancelSource = new CancellationTokenSource();
                var token = triviaCancelSource.Token;
                // load question
                CurrentQuestion = TriviaQuestionPool.Instance.GetRandomQuestion(oldQuestions);
                if (CurrentQuestion == null) {
                    await _channel.SendMessage($":exclamation: Failed loading a trivia question");
                    End().Wait();
                    return;
                }
                oldQuestions.Add(CurrentQuestion); //add it to exclusion list so it doesn't show up again
                                                   //sendquestion
                await _channel.SendMessage($":question: **{CurrentQuestion.Question}**");

                //receive messages
                NadekoBot.client.MessageReceived += PotentialGuess;

                //allow people to guess
                GameActive = true;

                try {
                    //hint
                    await Task.Delay(HintTimeoutMiliseconds, token);
                    await _channel.SendMessage($":exclamation:**Hint:** {CurrentQuestion.GetHint()}");

                    //timeout
                    await Task.Delay(QuestionDurationMiliseconds - HintTimeoutMiliseconds, token);

                } catch (TaskCanceledException) {
                    Console.WriteLine("Trivia cancelled");
                }
                GameActive = false;
                if (!triviaCancelSource.IsCancellationRequested)
                    await _channel.Send($":clock2: :question: **Time's up!** The correct answer was **{CurrentQuestion.Answer}**");
                NadekoBot.client.MessageReceived -= PotentialGuess;
                // load next question if game is still running
                await Task.Delay(2000);
            }
            await End();
        }

        private async Task End() {
            ShouldStopGame = true;
            await _channel.SendMessage("**Trivia game ended**\n"+GetLeaderboard());
            TriviaGame throwAwayValue;
            Commands.Trivia.runningTrivias.TryRemove(_server, out throwAwayValue);
        }

        public async Task StopGame() {
            if (!ShouldStopGame)
                await _channel.SendMessage(":exclamation: Trivia will stop after this question.");
            ShouldStopGame = true;
        }

        private async void PotentialGuess(object sender, MessageEventArgs e) {
            try {
                if (e.Channel.IsPrivate) return;
                if (e.Server != _server) return;

                bool guess = false;
                lock (_guessLock) {
                    if (GameActive && CurrentQuestion.IsAnswerCorrect(e.Message.Text) && !triviaCancelSource.IsCancellationRequested) {
                        users.TryAdd(e.User, 0); //add if not exists
                        users[e.User]++; //add 1 point to the winner
                        guess = true;
                    }
                }
                if (guess) {
                    triviaCancelSource.Cancel();
                    await _channel.SendMessage($"☑️ {e.User.Mention} guessed it! The answer was: **{CurrentQuestion.Answer}**");
                    if (users[e.User] == WinRequirement) {
                        ShouldStopGame = true;
                        await _channel.Send($":exclamation: We have a winner! Its {e.User.Mention}.");
                        // add points to the winner
                        await FlowersHandler.AddFlowersAsync(e.User, "Won Trivia", 2);
                    }
                }
            }
            catch { }
        }

        public string GetLeaderboard() {
            if (users.Count == 0)
                return "";
            
            string str = "**Leaderboard:**\n-----------\n";

            if (users.Count > 1)
                users.OrderBy(kvp => kvp.Value);

            foreach (var kvp in users) {
                str += $"**{kvp.Key.Name}** has {kvp.Value} points".ToString().SnPl(kvp.Value) + Environment.NewLine;
            }

            return str;
        }
    }
}
