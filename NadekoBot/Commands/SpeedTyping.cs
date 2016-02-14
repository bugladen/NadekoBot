using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Diagnostics;

namespace NadekoBot.Commands {

    public static class SentencesProvider {
        internal static string GetRandomSentence() {
            var data = Classes.DBHandler.Instance.GetAllRows<Classes._DataModels.TypingArticle>();
            try {
                return data.ToList()[new Random().Next(0, data.Count())].Text;
            } catch  {
                return "Failed retrieving data from parse. Owner didn't add any articles to type using `typeadd`.";
            }
        }
    }

    public class TypingGame {
        public static float WORD_VALUE { get; } = 4.5f;
        private Channel channel;
        public string currentSentence;
        public bool IsActive;
        private Stopwatch sw;
        private List<ulong> finishedUserIds;

        public TypingGame(Channel channel) {
            this.channel = channel;
            IsActive = false;
            sw = new Stopwatch();
            finishedUserIds = new List<ulong>();
        }

        public Channel Channell { get; internal set; }

        internal async Task<bool> Stop() {
            if (!IsActive) return false;
            NadekoBot.client.MessageReceived -= AnswerReceived;
            finishedUserIds.Clear();
            IsActive = false;
            sw.Stop();
            sw.Reset();
            await channel.Send("Typing contest stopped");
            return true;
        }

        internal async Task Start() {
            if (IsActive) return; // can't start running game
            IsActive = true;
            currentSentence = SentencesProvider.GetRandomSentence();
            int i = (int)(currentSentence.Length / WORD_VALUE * 1.7f);
            await channel.SendMessage($":clock2: Next contest will last for {i} seconds. Type the bolded text as fast as you can.");


            var msg = await channel.SendMessage("Starting new typing contest in **3**...");
            await Task.Delay(1000);
            await msg.Edit("Starting new typing contest in **2**...");
            await Task.Delay(1000);
            await msg.Edit("Starting new typing contest in **1**...");
            await Task.Delay(1000);
            await msg.Edit($":book:**{currentSentence.Replace(" ", " \x200B")}**:book:");
            sw.Start();
            HandleAnswers();

            while (i > 0) {
                await Task.Delay(1000);
                i--;
                if (!IsActive)
                    return;
            }

            await Stop();
            await Start();
        }

        private void HandleAnswers() {
            NadekoBot.client.MessageReceived += AnswerReceived;
        }

        private async void AnswerReceived(object sender, MessageEventArgs e) {
            if (e.Channel == null || e.Channel.Id != channel.Id) return;

            var guess = e.Message.RawText;

            var distance = currentSentence.LevenshteinDistance(guess);
            var decision = Judge(distance, guess.Length);
            if (decision && !finishedUserIds.Contains(e.User.Id)) {
                finishedUserIds.Add(e.User.Id);
                await channel.Send($"{e.User.Mention} finished in **{sw.Elapsed.Seconds}** seconds with { distance } errors, **{ currentSentence.Length / TypingGame.WORD_VALUE / sw.Elapsed.Seconds * 60 }** WPM!");
                if (finishedUserIds.Count % 2 == 0) {
                    await e.Send($":exclamation: `A lot of people finished, here is the text for those still typing:`\n\n:book:**{currentSentence}**:book:");
                }

            }
        }

        private bool Judge(int errors, int textLength) => errors <= textLength / 25;

    }

    class SpeedTyping : DiscordCommand {

        private static Dictionary<ulong, TypingGame> runningContests;

        public SpeedTyping() : base() {
            runningContests = new Dictionary<ulong, TypingGame>();
        }

        public override Func<CommandEventArgs, Task> DoFunc() =>
            async e => {
                if (runningContests.ContainsKey(e.User.Server.Id) && runningContests[e.User.Server.Id].IsActive) {
                    await e.Send($"Contest already running in { runningContests[e.User.Server.Id].Channell.Mention } channel.");
                    return;
                }
                if (runningContests.ContainsKey(e.User.Server.Id) && !runningContests[e.User.Server.Id].IsActive) {
                    await runningContests[e.User.Server.Id].Start();
                    return;
                }
                var tg = new TypingGame(e.Channel);
                runningContests.Add(e.Server.Id, tg);
                await tg.Start();
            };

        private Func<CommandEventArgs, Task> QuitFunc() =>
            async e => {
                if (runningContests.ContainsKey(e.User.Server.Id) &&
                    await runningContests[e.User.Server.Id].Stop()) {
                    runningContests.Remove(e.User.Server.Id);
                    return;
                }
                await e.Send("No contest to stop on this channel.");
            };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("typestart")
                .Description("Starts a typing contest.")
                .Do(DoFunc());

            cgb.CreateCommand("typestop")
                .Description("Stops a typing contest on the current channel.")
                .Do(QuitFunc());

            cgb.CreateCommand("typeadd")
                .Description("Adds a new article to the typing contest. Owner only.")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async e => {
                    if (e.User.Id != NadekoBot.OwnerID || string.IsNullOrWhiteSpace(e.GetArg("text"))) return;

                    Classes.DBHandler.Instance.InsertData(new Classes._DataModels.TypingArticle {
                        Text = e.GetArg("text"),
                        DateAdded = DateTime.Now
                    });

                    await e.Send("Added new article for typing game.");
                });

            //todo add user submissions
        }
    }
}
