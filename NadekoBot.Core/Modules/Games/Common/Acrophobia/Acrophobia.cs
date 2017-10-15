using NadekoBot.Common;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Common.Acrophobia
{
    /// <summary>
    /// Platform-agnostic acrophobia game
    /// </summary>
    public class Acrophobia : IDisposable
    {
        private const int VotingPhaseLength = 30;

        public enum Phase
        {
            Submission,
            Voting,
            Ended
        }

        public enum UserInputResult
        {
            Submitted,
            SubmissionFailed,
            Voted,
            VotingFailed,
            Failed
        }

        public int SubmissionPhaseLength { get; }

        public Phase CurrentPhase { get; private set; } = Phase.Submission;
        public ImmutableArray<char> StartingLetters { get; private set; }

        private readonly Dictionary<AcrophobiaUser, int> submissions = new Dictionary<AcrophobiaUser, int>();
        private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        private readonly NadekoRandom _rng;

        public event Func<Acrophobia, Task> OnStarted = delegate { return Task.CompletedTask; };
        public event Func<Acrophobia, ImmutableArray<KeyValuePair<AcrophobiaUser, int>>, Task> OnVotingStarted = delegate { return Task.CompletedTask; };
        public event Func<string, Task> OnUserVoted = delegate { return Task.CompletedTask; };
        public event Func<Acrophobia, ImmutableArray<KeyValuePair<AcrophobiaUser, int>>, Task> OnEnded = delegate { return Task.CompletedTask; };

        private readonly HashSet<ulong> _usersWhoVoted = new HashSet<ulong>();

        public Acrophobia(int submissionPhaseLength = 30)
        {
            _rng = new NadekoRandom();
            SubmissionPhaseLength = submissionPhaseLength;
            InitializeStartingLetters();
        }

        public async Task Run()
        {
            await OnStarted(this).ConfigureAwait(false);
            await Task.Delay(SubmissionPhaseLength * 1000);
            await locker.WaitAsync().ConfigureAwait(false);
            try
            {
                if (submissions.Count == 0)
                {
                    CurrentPhase = Phase.Ended;
                    await OnVotingStarted(this, ImmutableArray.Create<KeyValuePair<AcrophobiaUser, int>>()).ConfigureAwait(false);
                    return;
                }
                if (submissions.Count == 1)
                {
                    CurrentPhase = Phase.Ended;
                    await OnVotingStarted(this, submissions.ToArray().ToImmutableArray()).ConfigureAwait(false);
                    return;
                }

                CurrentPhase = Phase.Voting;

                await OnVotingStarted(this, submissions.ToArray().ToImmutableArray()).ConfigureAwait(false);
            }
            finally { locker.Release(); }

            await Task.Delay(VotingPhaseLength * 1000);
            await locker.WaitAsync().ConfigureAwait(false);
            try
            {
                CurrentPhase = Phase.Ended;
                await OnEnded(this, submissions.ToArray().ToImmutableArray()).ConfigureAwait(false) ;
            }
            finally { locker.Release(); }
        }

        private void InitializeStartingLetters()
        {
            var wordCount = _rng.Next(3, 6);

            var lettersArr = new char[wordCount];

            for (int i = 0; i < wordCount; i++)
            {
                var randChar = (char)_rng.Next(65, 91);
                lettersArr[i] = randChar == 'X' ? (char)_rng.Next(65, 88) : randChar;
            }
            StartingLetters = lettersArr.ToImmutableArray();
        }

        public async Task<bool> UserInput(ulong userId, string userName, string input)
        {
            var user = new AcrophobiaUser(userId, userName, input.ToLowerInvariant().ToTitleCase());

            await locker.WaitAsync();
            try
            {
                switch (CurrentPhase)
                {
                    case Phase.Submission:
                        if (submissions.ContainsKey(user) || !IsValidAnswer(input))
                            break;

                        submissions.Add(user, 0);
                        return true;
                    case Phase.Voting:
                        AcrophobiaUser toVoteFor;
                        if (!int.TryParse(input, out var index)
                            || --index < 0
                            || index >= submissions.Count
                            || (toVoteFor = submissions.ToArray()[index].Key).UserId == user.UserId
                            || !_usersWhoVoted.Add(userId))
                            break;
                        ++submissions[toVoteFor];
                        var _ = Task.Run(() => OnUserVoted(userName));
                        return true;
                    default:
                        break;
                }
                return false;
            }
            finally
            {
                locker.Release();
            }
        }

        private bool IsValidAnswer(string input)
        {
            input = input.ToUpperInvariant();

            var inputWords = input.Split(' ');

            if (inputWords.Length != StartingLetters.Length) // number of words must be the same as the number of the starting letters
                return false;

            for (int i = 0; i < StartingLetters.Length; i++)
            {
                var letter = StartingLetters[i];

                if (!inputWords[i].StartsWith(letter.ToString())) // all first letters must match
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            this.CurrentPhase = Phase.Ended;
            OnStarted = null;
            OnEnded = null;
            OnUserVoted = null;
            OnVotingStarted = null;
            _usersWhoVoted.Clear();
            submissions.Clear();
            locker.Dispose();
        }
    }
}
