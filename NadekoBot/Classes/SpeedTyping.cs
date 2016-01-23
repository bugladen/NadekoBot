using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Extensions;

namespace NadekoBot.Classes {

    public class TypingGame {
        private Channel channel;

        public TypingGame(Channel channel) {
            this.channel = channel;
        }

        public Channel Channell { get; internal set; }

        internal bool Stop() {
            throw new NotImplementedException();
        }

        internal void Start() {
            throw new NotImplementedException();
        }
    }

    class SpeedTyping : DiscordCommand {

        private static Dictionary<ulong, TypingGame> runningContests;

        public SpeedTyping() : base() {
            runningContests = new Dictionary<ulong, TypingGame>();
        }

        public override Func<CommandEventArgs, Task> DoFunc()=>
            async e => {
                if (runningContests.ContainsKey(e.User.Server.Id)) {
                    await e.Send($"Contest already running in { runningContests[e.User.Server.Id].Channell.Mention } channel.");
                    return;
                }
                var tg = new TypingGame(e.Channel);
                runningContests.Add(e.Server.Id,tg);
                await e.Send("Starting new typing contest!");
                tg.Start();
            };

        private Func<CommandEventArgs,Task> QuitFunc() =>
            async e => {
                if (runningContests.ContainsKey(e.User.Server.Id) &&
                    runningContests[e.User.Server.Id].Stop()) 
                {
                    runningContests.Remove(e.User.Server.Id);
                    await e.Send("Typing contest stopped");
                    return;
                }
                await e.Send("No contest to stop on this channel.");
            };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("typing contest")
                .Description("Starts a typing contest.")
                .Do(DoFunc());

            cgb.CreateCommand("typing stop")
                .Description("Stops a typing contest on the current channel.")
                .Do(QuitFunc());
        }
    }
}
