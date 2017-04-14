using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSharp.Processing;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PollCommands : NadekoSubmodule
        {
            public static ConcurrentDictionary<ulong, Poll> ActivePolls = new ConcurrentDictionary<ulong, Poll>();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public Task Poll([Remainder] string arg = null)
                => InternalStartPoll(arg, false);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public Task PublicPoll([Remainder] string arg = null)
                => InternalStartPoll(arg, true);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task PollStats()
            {
                Poll poll;
                if (!ActivePolls.TryGetValue(Context.Guild.Id, out poll))
                    return;

                await Context.Channel.EmbedAsync(poll.GetStats(GetText("current_poll_results")));
            }

            private async Task InternalStartPoll(string arg, bool isPublic = false)
            {
                var channel = (ITextChannel)Context.Channel;

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
                    await ReplyErrorLocalized("poll_already_running").ConfigureAwait(false);
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
            private readonly IUserMessage _originalMessage;
            private readonly IGuild _guild;
            private string[] answers { get; }
            private readonly ConcurrentDictionary<ulong, int> _participants = new ConcurrentDictionary<ulong, int>();
            private readonly string _question;
            public bool IsPublic { get; }

            public Poll(IUserMessage umsg, string question, IEnumerable<string> enumerable, bool isPublic = false)
            {
                _originalMessage = umsg;
                _guild = ((ITextChannel)umsg.Channel).Guild;
                _question = question;
                answers = enumerable as string[] ?? enumerable.ToArray();
                IsPublic = isPublic;
            }

            public EmbedBuilder GetStats(string title)
            {
                var results = _participants.GroupBy(kvp => kvp.Value)
                                    .ToDictionary(x => x.Key, x => x.Sum(kvp => 1))
                                    .OrderByDescending(kvp => kvp.Value)
                                    .ToArray();

                var eb = new EmbedBuilder().WithTitle(title);

                var sb = new StringBuilder()
                    .AppendLine(Format.Bold(_question))
                    .AppendLine();

                var totalVotesCast = 0;
                if (results.Length == 0)
                {
                    sb.AppendLine(GetText("no_votes_cast"));
                }
                else
                {
                    for (int i = 0; i < results.Length; i++)
                    {
                        var result = results[i];
                        sb.AppendLine(GetText("poll_result", 
                            result.Key,
                            Format.Bold(answers[result.Key - 1]), 
                            Format.Bold(result.Value.ToString())));
                        totalVotesCast += result.Value;
                    }
                }


                eb.WithDescription(sb.ToString())
                  .WithFooter(efb => efb.WithText(GetText("x_votes_cast", totalVotesCast)));

                return eb;
            }

            public async Task StartPoll()
            {
                NadekoBot.Client.MessageReceived += Vote;
                var msgToSend = GetText("poll_created", Format.Bold(_originalMessage.Author.Username)) + "\n\n" + Format.Bold(_question) + "\n";
                var num = 1;
                msgToSend = answers.Aggregate(msgToSend, (current, answ) => current + $"`{num++}.` **{answ}**\n");
                if (!IsPublic)
                    msgToSend += "\n" + Format.Bold(GetText("poll_vote_private"));
                else
                    msgToSend += "\n" + Format.Bold(GetText("poll_vote_public"));
                await _originalMessage.Channel.SendConfirmAsync(msgToSend).ConfigureAwait(false);
            }

            public async Task StopPoll()
            {
                NadekoBot.Client.MessageReceived -= Vote;
                await _originalMessage.Channel.EmbedAsync(GetStats("POLL CLOSED")).ConfigureAwait(false);
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
                    if (vote < 1 || vote > answers.Length)
                        return;

                    IMessageChannel ch;
                    if (IsPublic)
                    {
                        //if public, channel must be the same the poll started in
                        if (_originalMessage.Channel.Id != imsg.Channel.Id)
                            return;
                        ch = imsg.Channel;
                    }
                    else
                    {
                        //if private, channel must be dm channel
                        if ((ch = msg.Channel as IDMChannel) == null)
                            return;

                        // user must be a member of the guild this poll is in
                        var guildUsers = await _guild.GetUsersAsync().ConfigureAwait(false);
                        if (guildUsers.All(u => u.Id != imsg.Author.Id))
                            return;
                    }

                    //user can vote only once
                    if (_participants.TryAdd(msg.Author.Id, vote))
                    {
                        if (!IsPublic)
                        {
                            await ch.SendConfirmAsync(GetText("thanks_for_voting", Format.Bold(msg.Author.Username))).ConfigureAwait(false);
                        }
                        else
                        {
                            var toDelete = await ch.SendConfirmAsync(GetText("poll_voted", Format.Bold(msg.Author.ToString()))).ConfigureAwait(false);
                            toDelete.DeleteAfter(5);
                        }
                    }
                }
                catch { }
            }

            private string GetText(string key, params object[] replacements)
                => NadekoTopLevelModule.GetTextStatic(key,
                    NadekoBot.Localization.GetCultureInfo(_guild.Id),
                    typeof(Games).Name.ToLowerInvariant(),
                    replacements);
        }
    }
}