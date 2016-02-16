using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Discord;
using System.Threading;

namespace NadekoBot.Modules {
    internal class PollCommand : DiscordCommand {

        public static ConcurrentDictionary<Server, Poll> ActivePolls = new ConcurrentDictionary<Server, Poll>();

        public override Func<CommandEventArgs, Task> DoFunc() {
            throw new NotImplementedException();
        }

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(">poll")
                  .Description("Creates a poll, only person who has manage server permission can do it.\n**Usage**: >poll Question?;Answer1;Answ 2;A_3")
                  .Parameter("allargs", ParameterType.Unparsed)
                  .Do(e => {
                      if (!e.User.ServerPermissions.ManageChannels)
                          return;
                      if (ActivePolls.ContainsKey(e.Server))
                          return;
                      var arg = e.GetArg("allargs");
                      if (string.IsNullOrWhiteSpace(arg) || !arg.Contains(";"))
                          return;
                      var data = arg.Split(';');
                      if (data.Length < 3)
                          return;

                      new Poll(e, data[0], data.Skip(1));
                  });
            cgb.CreateCommand(">pollend")
                  .Description("Stops active poll on this server and prints the results in this channel.")
                  .Do(async e => {
                      if (!e.User.ServerPermissions.ManageChannels)
                          return;
                      if (!ActivePolls.ContainsKey(e.Server))
                          return;
                      await ActivePolls[e.Server].StopPoll(e.Channel);
                  });
        }
    }

    internal class Poll {
        private CommandEventArgs e;
        private string[] answers;
        private ConcurrentDictionary<User, int> participants = new ConcurrentDictionary<User, int>();
        private string question;
        private DateTime started;
        private CancellationTokenSource pollCancellationSource = new CancellationTokenSource();

        public Poll(CommandEventArgs e, string v, IEnumerable<string> enumerable) {
            this.e = e;
            this.question = v;
            this.answers = enumerable.ToArray();

            if (PollCommand.ActivePolls.TryAdd(e.Server, this)) {
                Task.Factory.StartNew(async () => await StartPoll());
            }
        }

        private async Task StartPoll() {
            started = DateTime.Now;
            NadekoBot.client.MessageReceived += Vote;
            var msgToSend =
                    $"📃**{e.User.Name}** from **{e.Server.Name}** server has created a poll which requires your attention:\n\n" +
                    $"**{question}**\n";
            int num = 1;
            foreach (var answ in answers) {
                msgToSend += $"`{num++}.` **{answ}**\n";
            }
            msgToSend += "\n**Private Message me with the corresponding number of the answer.**";
            await e.Channel.SendMessage(msgToSend);
        }

        public async Task StopPoll(Channel ch) {
            NadekoBot.client.MessageReceived -= Vote;
            Poll throwaway;
            PollCommand.ActivePolls.TryRemove(e.Server, out throwaway);
            try {
                var results = participants.GroupBy(kvp => kvp.Value)
                                .ToDictionary(x => x.Key, x => x.Sum(kvp => 1))
                                .OrderBy(kvp => kvp.Value);

                int totalVotesCast = results.Sum(kvp => kvp.Value);
                if (totalVotesCast == 0) {
                    await ch.SendMessage("📄 **No votes have been cast.**");
                    return;
                }
                var closeMessage = $"--------------**POLL CLOSED**--------------\n" +
                                   $"📄 , here are the results:\n";
                foreach (var kvp in results) {
                    closeMessage += $"`{kvp.Key}.` **[{answers[kvp.Key - 1]}]** has {kvp.Value} votes.({kvp.Value * 1.0f / totalVotesCast * 100}%)\n";
                }

                await ch.SendMessage($"📄 **Total votes cast**: {totalVotesCast}\n{closeMessage}");
            } catch (Exception ex) {
                Console.WriteLine($"Error in poll game {ex}");
            }
        }

        private async void Vote(object sender, MessageEventArgs e) {
            if (!e.Channel.IsPrivate)
                return;
            if (participants.ContainsKey(e.User))
                return;

            int vote;
            if (int.TryParse(e.Message.Text, out vote)) {
                if (vote < 1 || vote > answers.Length)
                    return;
                if (participants.TryAdd(e.User, vote)) {
                    await e.User.SendMessage($"Thanks for voting **{e.User.Name}**.");
                }
            }
        }
    }
}