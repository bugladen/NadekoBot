using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Commands
{
    internal class PollCommand : DiscordCommand
    {

        public static ConcurrentDictionary<Server, Poll> ActivePolls = new ConcurrentDictionary<Server, Poll>();

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "poll")
                  .Description($"Creates a poll, only person who has manage server permission can do it. | `{Prefix}poll Question?;Answer1;Answ 2;A_3`")
                  .Parameter("allargs", ParameterType.Unparsed)
                  .Do(async e =>
                  {
                      await Task.Run(async () =>
                      {
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

                          var poll = new Poll(e, data[0], data.Skip(1));
                          if (PollCommand.ActivePolls.TryAdd(e.Server, poll))
                          {
                              await poll.StartPoll().ConfigureAwait(false);
                          }
                      }).ConfigureAwait(false);
                  });
            cgb.CreateCommand(Module.Prefix + "pollend")
                  .Description($"Stops active poll on this server and prints the results in this channel. | `{Prefix}pollend`")
                  .Do(async e =>
                  {
                      if (!e.User.ServerPermissions.ManageChannels)
                          return;
                      if (!ActivePolls.ContainsKey(e.Server))
                          return;
                      await ActivePolls[e.Server].StopPoll(e.Channel).ConfigureAwait(false);
                  });
        }

        public PollCommand(DiscordModule module) : base(module) { }
    }

    internal class Poll
    {
        private readonly CommandEventArgs e;
        private readonly string[] answers;
        private ConcurrentDictionary<User, int> participants = new ConcurrentDictionary<User, int>();
        private readonly string question;
        private DateTime started;
        private CancellationTokenSource pollCancellationSource = new CancellationTokenSource();

        public Poll(CommandEventArgs e, string question, IEnumerable<string> enumerable)
        {
            this.e = e;
            this.question = question;
            this.answers = enumerable as string[] ?? enumerable.ToArray();
        }

        public async Task StartPoll()
        {
            started = DateTime.Now;
            NadekoBot.Client.MessageReceived += Vote;
            var msgToSend =
                    $"📃**{e.User.Name}** from **{e.Server.Name}** server has created a poll which requires your attention:\n\n" +
                    $"**{question}**\n";
            var num = 1;
            msgToSend = answers.Aggregate(msgToSend, (current, answ) => current + $"`{num++}.` **{answ}**\n");
            msgToSend += "\n**Private Message me with the corresponding number of the answer.**";
            await e.Channel.SendMessage(msgToSend).ConfigureAwait(false);
        }

        public async Task StopPoll(Channel ch)
        {
            NadekoBot.Client.MessageReceived -= Vote;
            Poll throwaway;
            PollCommand.ActivePolls.TryRemove(e.Server, out throwaway);
            try
            {
                var results = participants.GroupBy(kvp => kvp.Value)
                                .ToDictionary(x => x.Key, x => x.Sum(kvp => 1))
                                .OrderBy(kvp => kvp.Value);

                var totalVotesCast = results.Sum(kvp => kvp.Value);
                if (totalVotesCast == 0)
                {
                    await ch.SendMessage("📄 **No votes have been cast.**").ConfigureAwait(false);
                    return;
                }
                var closeMessage = $"--------------**POLL CLOSED**--------------\n" +
                                   $"📄 , here are the results:\n";
                closeMessage = results.Aggregate(closeMessage, (current, kvp) => current + $"`{kvp.Key}.` **[{answers[kvp.Key - 1]}]**" +
                                                                                 $" has {kvp.Value} votes." +
                                                                                 $"({kvp.Value * 1.0f / totalVotesCast * 100}%)\n");

                await ch.SendMessage($"📄 **Total votes cast**: {totalVotesCast}\n{closeMessage}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in poll game {ex}");
            }
        }

        private async void Vote(object sender, MessageEventArgs e)
        {
            try
            {
                if (!e.Channel.IsPrivate)
                    return;
                if (participants.ContainsKey(e.User))
                    return;

                int vote;
                if (!int.TryParse(e.Message.Text, out vote)) return;
                if (vote < 1 || vote > answers.Length)
                    return;
                if (participants.TryAdd(e.User, vote))
                {
                    await e.User.SendMessage($"Thanks for voting **{e.User.Name}**.").ConfigureAwait(false);
                }
            }
            catch { }
        }
    }
}