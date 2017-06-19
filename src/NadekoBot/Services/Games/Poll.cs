using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Games
{
    //todo 75 rewrite
    public class Poll
    {
        private readonly IUserMessage _originalMessage;
        private readonly IGuild _guild;
        private string[] answers { get; }
        private readonly ConcurrentDictionary<ulong, int> _participants = new ConcurrentDictionary<ulong, int>();
        private readonly string _question;
        private readonly DiscordSocketClient _client;
        private readonly NadekoStrings _strings;
        private bool running = false;
        private HashSet<ulong> _guildUsers;

        public event Action<ulong> OnEnded = delegate { };

        public bool IsPublic { get; }

        public Poll(DiscordSocketClient client, NadekoStrings strings, IUserMessage umsg, string question, IEnumerable<string> enumerable, bool isPublic = false)
        {
            _client = client;
            _strings = strings;

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
            var msgToSend = GetText("poll_created", Format.Bold(_originalMessage.Author.Username)) + "\n\n" + Format.Bold(_question) + "\n";
            var num = 1;
            msgToSend = answers.Aggregate(msgToSend, (current, answ) => current + $"`{num++}.` **{answ}**\n");
            if (!IsPublic)
                msgToSend += "\n" + Format.Bold(GetText("poll_vote_private"));
            else
                msgToSend += "\n" + Format.Bold(GetText("poll_vote_public"));

            if (!IsPublic)
                _guildUsers = new HashSet<ulong>((await _guild.GetUsersAsync().ConfigureAwait(false)).Select(x => x.Id));

            await _originalMessage.Channel.SendConfirmAsync(msgToSend).ConfigureAwait(false);
            running = true;
        }

        public async Task StopPoll()
        {
            running = false;
            OnEnded(_guild.Id);
            await _originalMessage.Channel.EmbedAsync(GetStats("POLL CLOSED")).ConfigureAwait(false);
        }

        public async Task<bool> TryVote(IUserMessage msg)
        {
            // has to be a user message
            if (msg == null || msg.Author.IsBot || !running)
                return false;

            // has to be an integer
            if (!int.TryParse(msg.Content, out int vote))
                return false;
            if (vote < 1 || vote > answers.Length)
                return false;

            IMessageChannel ch;
            if (IsPublic)
            {
                //if public, channel must be the same the poll started in
                if (_originalMessage.Channel.Id != msg.Channel.Id)
                    return false;
                ch = msg.Channel;
            }
            else
            {
                //if private, channel must be dm channel
                if ((ch = msg.Channel as IDMChannel) == null)
                    return false;

                // user must be a member of the guild this poll is in
                if (!_guildUsers.Contains(msg.Author.Id))
                    return false;
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
                return true;
            }
            return false;
        }

        private string GetText(string key, params object[] replacements)
            => _strings.GetText(key,
                _guild.Id,
                "Games".ToLowerInvariant(),
                replacements);
    }
}
