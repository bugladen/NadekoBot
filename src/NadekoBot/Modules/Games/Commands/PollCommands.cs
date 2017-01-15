using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PollCommands : ModuleBase
        {
            public static ConcurrentDictionary<ulong, Poll> ActivePolls = new ConcurrentDictionary<ulong, Poll>();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public Task Poll([Remainder] string arg = null)
                => InternalStartPoll(arg, isPublic: false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public Task PublicPoll([Remainder] string arg = null)
                => InternalStartPoll(arg, isPublic: true);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task PollStats()
            {
                Games.Poll poll;
                if (!ActivePolls.TryGetValue(Context.Guild.Id, out poll))
                    return;

                await Context.Channel.EmbedAsync(poll.GetStats("Current Poll Results"));
            }

            private async Task InternalStartPoll(string arg, bool isPublic = false)
            {
                var channel = (ITextChannel)Context.Channel;

                if (!(Context.User as IGuildUser).GuildPermissions.ManageChannels)
                    return;
                if (string.IsNullOrWhiteSpace(arg) || !arg.Contains(";"))
                    return;
                var data = arg.Split(';');
                if (data.Length < 3)
                    return;

                var poll = new Poll(Context.Message, data[0], data.Skip(1), isPublic: isPublic);
                if (ActivePolls.TryAdd(channel.Guild.Id, poll))
                {
                    await poll.StartPoll().ConfigureAwait(false);
                }
                else
                    await channel.SendErrorAsync("Poll is already running on this server.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task Pollend()
            {
                var channel = (ITextChannel)Context.Channel;

                Poll poll;
                ActivePolls.TryRemove(channel.Guild.Id, out poll);
                await poll.StopPoll().ConfigureAwait(false);
            }
        }

        public class Poll
        {
            private readonly IUserMessage originalMessage;
            private readonly IGuild guild;
            private string[] Answers { get; }
            private ConcurrentDictionary<ulong, int> participants = new ConcurrentDictionary<ulong, int>();
            private readonly string question;
            private DateTime started;
            private CancellationTokenSource pollCancellationSource = new CancellationTokenSource();
            public bool IsPublic { get; }

            public Poll(IUserMessage umsg, string question, IEnumerable<string> enumerable, bool isPublic = false)
            {
                this.originalMessage = umsg;
                this.guild = ((ITextChannel)umsg.Channel).Guild;
                this.question = question;
                this.Answers = enumerable as string[] ?? enumerable.ToArray();
                this.IsPublic = isPublic;
            }

            public EmbedBuilder GetStats(string title)
            {
                var results = participants.GroupBy(kvp => kvp.Value)
                                    .ToDictionary(x => x.Key, x => x.Sum(kvp => 1))
                                    .OrderByDescending(kvp => kvp.Value)
                                    .ToArray();

                var eb = new EmbedBuilder().WithTitle(title);

                var sb = new StringBuilder()
                    .AppendLine(Format.Bold(question))
                    .AppendLine();

                var totalVotesCast = 0;
                if (results.Length == 0)
                {
                    sb.AppendLine("No votes cast.");
                }
                else
                {
                    for (int i = 0; i < results.Length; i++)
                    {
                        var result = results[i];
                        sb.AppendLine($"`{i + 1}.` {Format.Bold(Answers[result.Key - 1])} with {Format.Bold(result.Value.ToString())} votes.");
                        totalVotesCast += result.Value;
                    }
                }


                eb.WithDescription(sb.ToString())
                  .WithFooter(efb => efb.WithText(totalVotesCast + " total votes cast."));

                return eb;
            }

            public async Task StartPoll()
            {
                started = DateTime.Now;
                NadekoBot.Client.MessageReceived += Vote;
                var msgToSend = $"📃**{originalMessage.Author.Username}** has created a poll which requires your attention:\n\n**{question}**\n";
                var num = 1;
                msgToSend = Answers.Aggregate(msgToSend, (current, answ) => current + $"`{num++}.` **{answ}**\n");
                if (!IsPublic)
                    msgToSend += "\n**Private Message me with the corresponding number of the answer.**";
                else
                    msgToSend += "\n**Send a Message here with the corresponding number of the answer.**";
                await originalMessage.Channel.SendConfirmAsync(msgToSend).ConfigureAwait(false);
            }

            public async Task StopPoll()
            {
                NadekoBot.Client.MessageReceived -= Vote;
                await originalMessage.Channel.EmbedAsync(GetStats("POLL CLOSED")).ConfigureAwait(false);
            }

            private async Task Vote(SocketMessage imsg)
            {
                try
                {
                    // has to be a user message
                    var msg = imsg as SocketUserMessage;
                    if (msg == null || msg.Author.IsBot)
                        return;

                    // has to be an integer
                    int vote;
                    if (!int.TryParse(imsg.Content, out vote))
                        return;
                    if (vote < 1 || vote > Answers.Length)
                        return;

                    IMessageChannel ch;
                    if (IsPublic)
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
                        if (!IsPublic)
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
}