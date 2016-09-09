using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        public static ConcurrentDictionary<IGuild, Poll> ActivePolls = new ConcurrentDictionary<IGuild, Poll>();

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task Poll(IUserMessage umsg, [Remainder] string arg = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (!(umsg.Author as IGuildUser).GuildPermissions.ManageChannels)
                return;
            if (string.IsNullOrWhiteSpace(arg) || !arg.Contains(";"))
                return;
            var data = arg.Split(';');
            if (data.Length < 3)
                return;

            var poll = new Poll(umsg, data[0], data.Skip(1));
            if (ActivePolls.TryAdd(channel.Guild, poll))
            {
                await poll.StartPoll().ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task Pollend(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (!(umsg.Author as IGuildUser).GuildPermissions.ManageChannels)
                return;
            Poll poll;
            ActivePolls.TryGetValue(channel.Guild, out poll);
            await poll.StopPoll(channel).ConfigureAwait(false);
        }
    }

    public class Poll
    {
        private readonly IUserMessage umsg;
        private readonly string[] answers;
        private ConcurrentDictionary<IUser, int> participants = new ConcurrentDictionary<IUser, int>();
        private readonly string question;
        private DateTime started;
        private CancellationTokenSource pollCancellationSource = new CancellationTokenSource();

        public Poll(IUserMessage umsg, string question, IEnumerable<string> enumerable)
        {
            this.umsg = umsg;
            this.question = question;
            this.answers = enumerable as string[] ?? enumerable.ToArray();
        }

        public async Task StartPoll()
        {
            started = DateTime.Now;
            NadekoBot.Client.MessageReceived += Vote;
            var msgToSend = $@"📃**{umsg.Author.Username}** has created a poll which requires your attention:

**{question}**\n";
            var num = 1;
            msgToSend = answers.Aggregate(msgToSend, (current, answ) => current + $"`{num++}.` **{answ}**\n");
            msgToSend += "\n**Private Message me with the corresponding number of the answer.**";
            await umsg.Channel.SendMessageAsync(msgToSend).ConfigureAwait(false);
        }

        public async Task StopPoll(IGuildChannel ch)
        {
            NadekoBot.Client.MessageReceived -= Vote;
            try
            {
                var results = participants.GroupBy(kvp => kvp.Value)
                                .ToDictionary(x => x.Key, x => x.Sum(kvp => 1))
                                .OrderBy(kvp => kvp.Value);

                var totalVotesCast = results.Sum(kvp => kvp.Value);
                if (totalVotesCast == 0)
                {
                    await umsg.Channel.SendMessageAsync("📄 **No votes have been cast.**").ConfigureAwait(false);
                    return;
                }
                var closeMessage = $"--------------**POLL CLOSED**--------------\n" +
                                   $"📄 , here are the results:\n";
                closeMessage = results.Aggregate(closeMessage, (current, kvp) => current + $"`{kvp.Key}.` **[{answers[kvp.Key - 1]}]**" +
                                                                                 $" has {kvp.Value} votes." +
                                                                                 $"({kvp.Value * 1.0f / totalVotesCast * 100}%)\n");

                await umsg.Channel.SendMessageAsync($"📄 **Total votes cast**: {totalVotesCast}\n{closeMessage}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in poll game {ex}");
            }
        }

        private Task Vote(IMessage imsg)
        {
            // has to be a user message
            var msg = imsg as IUserMessage;
            if (msg == null)
                return Task.CompletedTask;
            // channel must be private
            IPrivateChannel ch;
            if ((ch = msg.Channel as IPrivateChannel) == null)
                return Task.CompletedTask;

            // has to be an integer
            int vote;
            if (!int.TryParse(msg.Content, out vote)) return Task.CompletedTask;

            var t = Task.Run(async () =>
            {
                try
                {
                    
                    if (vote < 1 || vote > answers.Length)
                        return;
                    if (participants.TryAdd(msg.Author, vote))
                    {
                        await (ch as ITextChannel).SendMessageAsync($"Thanks for voting **{msg.Author.Username}**.").ConfigureAwait(false);
                    }
                }
                catch { }
            });
            return Task.CompletedTask;
        }
    }
}