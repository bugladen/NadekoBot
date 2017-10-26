using NadekoBot.Common.Collections;
using System.Collections.Generic;

namespace NadekoBot.Core.Services.Database.Models
{
    public class Poll
    {
        public ulong GuildId { get; set; }
        public string Question { get; set; }
        public IndexedCollection<PollAnswer> Answers { get; set; }
        public HashSet<PollVote> Votes { get; set; }
    }

    public class PollAnswer : IIndexed
    {
        public int Index { get; set; }
        public string Text { get; set; }
    }
}
