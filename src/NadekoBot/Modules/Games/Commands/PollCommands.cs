using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
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

        [NadekoCommand, Usage, Description, Aliases]
        [RequirePermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public Task Poll(IUserMessage umsg, [Remainder] string arg = null)
            => InternalStartPoll(umsg, arg, isPublic: false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequirePermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public Task PublicPoll(IUserMessage umsg, [Remainder] string arg = null)
            => InternalStartPoll(umsg, arg, isPublic: true);

        private async Task InternalStartPoll(IUserMessage umsg, string arg, bool isPublic = false)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (!(umsg.Author as IGuildUser).GuildPermissions.ManageChannels)
                return;
            if (string.IsNullOrWhiteSpace(arg) || !arg.Contains(";"))
                return;
            var data = arg.Split(';');
            if (data.Length < 3)
                return;

            var poll = new Poll(umsg, data[0], data.Skip(1), isPublic: isPublic);
            if (ActivePolls.TryAdd(channel.Guild, poll))
            {
                await poll.StartPoll().ConfigureAwait(false);
            }
            else
                await channel.SendErrorAsync("Poll is already running on this server.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequirePermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async Task Pollend(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            Poll poll;
            ActivePolls.TryRemove(channel.Guild, out poll);
            await poll.StopPoll().ConfigureAwait(false);
        }
    }

    public class Poll
    {
        private readonly IUserMessage originalMessage;
        private readonly IGuild guild;
        private readonly string[] answers;
        private ConcurrentDictionary<ulong, int> participants = new ConcurrentDictionary<ulong, int>();
        private readonly string question;
        private DateTime started;
        private CancellationTokenSource pollCancellationSource = new CancellationTokenSource();
        private readonly bool isPublic;

        public Poll(IUserMessage umsg, string question, IEnumerable<string> enumerable, bool isPublic = false)
        {
            this.originalMessage = umsg;
            this.guild = ((ITextChannel)umsg.Channel).Guild;
            this.question = question;
            this.answers = enumerable as string[] ?? enumerable.ToArray();
            this.isPublic = isPublic;
        }

        public async Task StartPoll()
        {
            started = DateTime.Now;
            NadekoBot.Client.MessageReceived += Vote;
            var msgToSend = $"📃**{originalMessage.Author.Username}** has created a poll which requires your attention:\n\n**{question}**\n";
            var num = 1;
            msgToSend = answers.Aggregate(msgToSend, (current, answ) => current + $"`{num++}.` **{answ}**\n");
            if (!isPublic)
                msgToSend += "\n**Private Message me with the corresponding number of the answer.**";
            else
                msgToSend += "\n**Send a Message here with the corresponding number of the answer.**";
            await originalMessage.Channel.SendConfirmAsync(msgToSend).ConfigureAwait(false);
        }

        public async Task StopPoll()
        {
            NadekoBot.Client.MessageReceived -= Vote;
            try
            {
                var results = participants.GroupBy(kvp => kvp.Value)
                                .ToDictionary(x => x.Key, x => x.Sum(kvp => 1))
                                .OrderByDescending(kvp => kvp.Value);

                var totalVotesCast = results.Sum(kvp => kvp.Value);
                if (totalVotesCast == 0)
                {
                    await originalMessage.Channel.SendMessageAsync("📄 **No votes have been cast.**").ConfigureAwait(false);
                    return;
                }
                var closeMessage = $"--------------**POLL CLOSED**--------------\n" +
                                   $"📄 , here are the results:\n";
                closeMessage = results.Aggregate(closeMessage, (current, kvp) => current + $"`{kvp.Key}.` **[{answers[kvp.Key - 1]}]**" +
                                                                                 $" has {kvp.Value} votes." +
                                                                                 $"({kvp.Value * 1.0f / totalVotesCast * 100}%)\n");

                await originalMessage.Channel.SendConfirmAsync($"📄 **Total votes cast**: {totalVotesCast}\n{closeMessage}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in poll game {ex}");
            }
        }

        private async void Vote(IMessage imsg)
        {
            try
            {
                // has to be a user message
                var msg = imsg as IUserMessage;
                if (msg == null || msg.Author.IsBot)
                    return;

                // has to be an integer
                int vote;
                if (!int.TryParse(imsg.Content, out vote))
                    return;
                if (vote < 1 || vote > answers.Length)
                    return;

                IMessageChannel ch;
                if (isPublic)
                {
                    //if public, channel must be the same the poll started in
                    if (originalMessage.Channel.Id != imsg.Channel.Id)
                        return;
                    ch = imsg.Channel;
                }
                else
                {
                    //if private, channel must be dm channel
                    if ((ch = msg.Channel as IDMChannel) == null)
                        return;

                    // user must be a member of the guild this poll is in
                    var guildUsers = await guild.GetUsersAsync().ConfigureAwait(false);
                    if (!guildUsers.Any(u => u.Id == imsg.Author.Id))
                        return;
                }

                //user can vote only once
                if (participants.TryAdd(msg.Author.Id, vote))
                {
                    if (!isPublic)
                    {
                        await ch.SendConfirmAsync($"Thanks for voting **{msg.Author.Username}**.").ConfigureAwait(false);
                    }
                    else
                    {
                        var toDelete = await ch.SendConfirmAsync($"{msg.Author.Mention} cast their vote.").ConfigureAwait(false);
                        toDelete.DeleteAfter(5);
                    }
                }
            }
            catch { }
        }
    }
}