using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Threading;
using System.Diagnostics;
using Parse;

namespace NadekoBot {

    public static class SentencesProvider {
        internal static string GetRandomSentence() {
            return "Random ultra long test sentence that i have to type every time.";
        }
    }

    public class TypingGame {
        public static float WORD_VALUE { get; } = 4.5f;
        private Channel channel;
        public string currentSentence;
        public bool IsActive;
        private Stopwatch sw;
    
        public TypingGame(Channel channel) {
            this.channel = channel;
            currentSentence = SentencesProvider.GetRandomSentence();
            IsActive = false;
            sw = new Stopwatch();
        }

        public Channel Channell { get; internal set; }

        internal async Task<bool> Stop() {
            if (!IsActive) return false;
            NadekoBot.client.MessageReceived -= AnswerReceived;
            IsActive = false;
            sw.Stop();
            sw.Reset();
            await channel.Send("Typing contest stopped");
            return true;
        }

        internal async Task Start() {
            IsActive = true;
            var msg = await channel.SendMessage("Starting new typing contest in **3**...");
            await Task.Delay(1000);
            await msg.Edit("Starting new typing contest in **2**...");
            await Task.Delay(1000);
            await msg.Edit("Starting new typing contest in **1**...");
            await Task.Delay(1000);
            await msg.Edit($"**{currentSentence}**");
            sw.Start();
            HandleAnswers();
            int i = (int)(currentSentence.Length / WORD_VALUE * 1.7f);
            while (i > 0) {
                await Task.Delay(1000);
                i--;
                if (!IsActive)
                    break;
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

            if (currentSentence == guess) {
                await channel.Send($"{e.User.Mention} finished in **{sw.Elapsed.Seconds}** seconds, **{ currentSentence.Length / TypingGame.WORD_VALUE / sw.Elapsed.Seconds * 60 }** WPM!");
            }
        }
    }

    class SpeedTyping : DiscordCommand {

        private static Dictionary<ulong, TypingGame> runningContests;

        public SpeedTyping() : base() {
            runningContests = new Dictionary<ulong, TypingGame>();
        }

        public override Func<CommandEventArgs, Task> DoFunc()=>
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
                runningContests.Add(e.Server.Id,tg);
                await tg.Start();
            };

        private Func<CommandEventArgs,Task> QuitFunc() =>
            async e => {
                if (runningContests.ContainsKey(e.User.Server.Id) &&
                    await runningContests[e.User.Server.Id].Stop()) 
                {
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
        }
    }
}
