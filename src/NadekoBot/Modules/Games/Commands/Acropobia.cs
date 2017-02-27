using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class Acropobia : NadekoSubmodule
        {
            //channelId, game
            public static ConcurrentDictionary<ulong, AcrophobiaGame> AcrophobiaGames { get; } = new ConcurrentDictionary<ulong, AcrophobiaGame>();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Acro(int time = 60)
            {
                if (time < 10 || time > 120)
                    return;
                var channel = (ITextChannel)Context.Channel;

                var game = new AcrophobiaGame(channel, time);
                if (AcrophobiaGames.TryAdd(channel.Id, game))
                {
                    try
                    {
                        await game.Run();
                    }
                    finally
                    {
                        game.EnsureStopped();
                        AcrophobiaGames.TryRemove(channel.Id, out game);
                    }
                }
                else
                {
                    await ReplyErrorLocalized("acro_running").ConfigureAwait(false);
                }
            }
        }

        public enum AcroPhase
        {
            Submitting,
            Idle, // used to wait for some other actions while transitioning through phases
            Voting
        }

        public class AcrophobiaGame
        {
            private readonly ITextChannel _channel;
            private readonly int _time;
            private readonly NadekoRandom _rng;
            private readonly ImmutableArray<char> _startingLetters;
            private readonly CancellationTokenSource _source;
            private AcroPhase phase { get; set; } = AcroPhase.Submitting;

            private readonly ConcurrentDictionary<string, IGuildUser> _submissions = new ConcurrentDictionary<string, IGuildUser>();
            public IReadOnlyDictionary<string, IGuildUser> Submissions => _submissions;

            private readonly ConcurrentHashSet<ulong> _usersWhoSubmitted = new ConcurrentHashSet<ulong>();
            private readonly ConcurrentHashSet<ulong> _usersWhoVoted = new ConcurrentHashSet<ulong>();

            private int _spamCount;

            //text, votes
            private readonly ConcurrentDictionary<string, int> _votes = new ConcurrentDictionary<string, int>();
            private readonly Logger _log;

            public AcrophobiaGame(ITextChannel channel, int time)
            {
                _log = LogManager.GetCurrentClassLogger();

                _channel = channel;
                _time = time;
                _source = new CancellationTokenSource();

                _rng = new NadekoRandom();
                var wordCount = _rng.Next(3, 6);

                var lettersArr = new char[wordCount];

                for (int i = 0; i < wordCount; i++)
                {
                    var randChar = (char)_rng.Next(65, 91);
                    lettersArr[i] = randChar == 'X' ? (char)_rng.Next(65, 88) : randChar;
                }
                _startingLetters = lettersArr.ToImmutableArray();
            }

            private EmbedBuilder GetEmbed()
            {
                var i = 0;
                return phase == AcroPhase.Submitting

                    ? new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("acrophobia"))
                        .WithDescription(GetText("acro_started", Format.Bold(string.Join(".", _startingLetters))))
                        .WithFooter(efb => efb.WithText(GetText("acro_started_footer", _time)))

                    : new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(GetText("acrophobia") + " - " + GetText("submissions_closed"))
                        .WithDescription(GetText("acro_nym_was", Format.Bold(string.Join(".", _startingLetters)) + "\n" +
$@"--
{_submissions.Aggregate("",(agg, cur) => agg + $"`{++i}.` **{cur.Key.ToLowerInvariant().ToTitleCase()}**\n")}
--"))
                        .WithFooter(efb => efb.WithText(GetText("acro_vote")));
            }

            public async Task Run()
            {
                NadekoBot.Client.MessageReceived += PotentialAcro;
                var embed = GetEmbed();

                //SUBMISSIONS PHASE
                await _channel.EmbedAsync(embed).ConfigureAwait(false);
                try
                {
                    await Task.Delay(_time * 1000, _source.Token).ConfigureAwait(false);
                    phase = AcroPhase.Idle;
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                //var i = 0;
                if (_submissions.Count == 0)
                {
                    await _channel.SendErrorAsync(GetText("acrophobia"), GetText("acro_ended_no_sub"));
                    return;
                }
                if (_submissions.Count == 1)
                {
                    await _channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithDescription(
                                GetText("acro_winner_only",
                                    Format.Bold(_submissions.First().Value.ToString())))
                            .WithFooter(efb => efb.WithText(_submissions.First().Key.ToLowerInvariant().ToTitleCase())))
                        .ConfigureAwait(false);
                    return;
                }
                var submissionClosedEmbed = GetEmbed();

                await _channel.EmbedAsync(submissionClosedEmbed).ConfigureAwait(false);

                //VOTING PHASE
                phase = AcroPhase.Voting;
                try
                {
                    //30 secondds for voting
                    await Task.Delay(30000, _source.Token).ConfigureAwait(false);
                    phase = AcroPhase.Idle;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                await End().ConfigureAwait(false);
            }

            private async Task PotentialAcro(SocketMessage arg)
            {
                try
                {
                    var msg = arg as SocketUserMessage;
                    if (msg == null || msg.Author.IsBot || msg.Channel.Id != _channel.Id)
                        return;

                    ++_spamCount;

                    var guildUser = (IGuildUser)msg.Author;

                    var input = msg.Content.ToUpperInvariant().Trim();

                    if (phase == AcroPhase.Submitting)
                    {
                        if (_spamCount > 10)
                        {
                            _spamCount = 0;
                            try { await _channel.EmbedAsync(GetEmbed()).ConfigureAwait(false); }
                            catch { }
                        }
                        var inputWords = input.Split(' '); //get all words

                        if (inputWords.Length != _startingLetters.Length) // number of words must be the same as the number of the starting letters
                            return;

                        for (int i = 0; i < _startingLetters.Length; i++)
                        {
                            var letter = _startingLetters[i];

                            if (!inputWords[i].StartsWith(letter.ToString())) // all first letters must match
                                return;
                        }


                        if (!_usersWhoSubmitted.Add(guildUser.Id))
                            return;
                        //try adding it to the list of answers
                        if (!_submissions.TryAdd(input, guildUser))
                        {
                            _usersWhoSubmitted.TryRemove(guildUser.Id);
                            return;
                        }

                        // all good. valid input. answer recorded
                        await _channel.SendConfirmAsync(GetText("acrophobia"),
                            GetText("acro_submit", guildUser.Mention,
                                _submissions.Count));
                        try
                        {
                            await msg.DeleteAsync();
                        }
                        catch
                        {
                            await msg.DeleteAsync(); //try twice
                        }
                    }
                    else if (phase == AcroPhase.Voting)
                    {
                        if (_spamCount > 10)
                        {
                            _spamCount = 0;
                            try { await _channel.EmbedAsync(GetEmbed()).ConfigureAwait(false); }
                            catch { }
                        }

                        //if (submissions.TryGetValue(input, out usr) && usr.Id != guildUser.Id)
                        //{
                        //    if (!usersWhoVoted.Add(guildUser.Id))
                        //        return;
                        //    votes.AddOrUpdate(input, 1, (key, old) => ++old);
                        //    await channel.SendConfirmAsync("Acrophobia", $"{guildUser.Mention} cast their vote!").ConfigureAwait(false);
                        //    await msg.DeleteAsync().ConfigureAwait(false);
                        //    return;
                        //}

                        int num;
                        if (int.TryParse(input, out num) && num > 0 && num <= _submissions.Count)
                        {
                            var kvp = _submissions.Skip(num - 1).First();
                            var usr = kvp.Value;
                            //can't vote for yourself, can't vote multiple times
                            if (usr.Id == guildUser.Id || !_usersWhoVoted.Add(guildUser.Id))
                                return;
                            _votes.AddOrUpdate(kvp.Key, 1, (key, old) => ++old);
                            await _channel.SendConfirmAsync(GetText("acrophobia"),
                                GetText("acro_vote_cast", Format.Bold(guildUser.ToString()))).ConfigureAwait(false);
                            await msg.DeleteAsync().ConfigureAwait(false);
                        }

                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }

            public async Task End()
            {
                if (!_votes.Any())
                {
                    await _channel.SendErrorAsync(GetText("acrophobia"), GetText("acro_no_votes_cast")).ConfigureAwait(false);
                    return;
                }
                var table = _votes.OrderByDescending(v => v.Value);
                var winner = table.First();
                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("acrophobia"))
                    .WithDescription(GetText("acro_winner", Format.Bold(_submissions[winner.Key].ToString()),
                        Format.Bold(winner.Value.ToString())))
                    .WithFooter(efb => efb.WithText(winner.Key.ToLowerInvariant().ToTitleCase()));

                await _channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            public void EnsureStopped()
            {
                NadekoBot.Client.MessageReceived -= PotentialAcro;
                if (!_source.IsCancellationRequested)
                    _source.Cancel();
            }

            private string GetText(string key, params object[] replacements)
                => GetTextStatic(key,
                    NadekoBot.Localization.GetCultureInfo(_channel.Guild),
                    typeof(Games).Name.ToLowerInvariant(),
                    replacements);
        }
    }
}