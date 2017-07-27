using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Collections;
using NadekoBot.Services.Impl;
using NadekoBot.Modules.Games.Common.Acrophobia;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class AcropobiaCommands : NadekoSubmodule
        {
            private readonly DiscordSocketClient _client;

            //channelId, game
            public static ConcurrentDictionary<ulong, Acrophobia> AcrophobiaGames { get; } = new ConcurrentDictionary<ulong, Acrophobia>();

            public AcropobiaCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Acro(int submissionTime = 30)
            {
                if (submissionTime < 10 || submissionTime > 120)
                    return;
                var channel = (ITextChannel)Context.Channel;

                var game = new Acrophobia(submissionTime);
                if (AcrophobiaGames.TryAdd(channel.Id, game))
                {
                    try
                    {
                        game.OnStarted += Game_OnStarted;
                        game.OnEnded += Game_OnEnded;
                        game.OnVotingStarted += Game_OnVotingStarted;
                        game.OnUserVoted += Game_OnUserVoted;
                        _client.MessageReceived += _client_MessageReceived;
                        await game.Run().ConfigureAwait(false);
                    }
                    finally
                    {
                        _client.MessageReceived -= _client_MessageReceived;
                        AcrophobiaGames.TryRemove(channel.Id, out game);
                        game.Dispose();
                    }
                }
                else
                {
                    await ReplyErrorLocalized("acro_running").ConfigureAwait(false);
                }

                Task _client_MessageReceived(SocketMessage msg)
                {
                    if (msg.Channel.Id != Context.Channel.Id)
                        return Task.CompletedTask;

                    var _ = Task.Run(async () =>
                    {
                        try
                        {
                            var success = await game.UserInput(msg.Author.Id, msg.Author.ToString(), msg.Content)
                                .ConfigureAwait(false);
                            if (success)
                                await msg.DeleteAsync().ConfigureAwait(false);
                        }
                        catch { }
                    });

                    return Task.CompletedTask;
                }
            }

            private Task Game_OnStarted(Acrophobia game)
            {
                var embed = new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("acrophobia"))
                        .WithDescription(GetText("acro_started", Format.Bold(string.Join(".", game.StartingLetters))))
                        .WithFooter(efb => efb.WithText(GetText("acro_started_footer", game.SubmissionPhaseLength)));

                return Context.Channel.EmbedAsync(embed);
            }

            private Task Game_OnUserVoted(string user)
            {
                return Context.Channel.SendConfirmAsync(
                    GetText("acrophobia"),
                    GetText("acro_vote_cast", Format.Bold(user)));
            }

            private async Task Game_OnVotingStarted(Acrophobia game, ImmutableArray<KeyValuePair<AcrophobiaUser, int>> submissions)
            {
                if (submissions.Length == 0)
                {
                    await Context.Channel.SendErrorAsync(GetText("acrophobia"), GetText("acro_ended_no_sub"));
                    return;
                }
                if (submissions.Length == 1)
                {
                    await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithDescription(
                                GetText("acro_winner_only",
                                    Format.Bold(submissions.First().Key.UserName)))
                            .WithFooter(efb => efb.WithText(submissions.First().Key.Input)))
                        .ConfigureAwait(false);
                    return;
                }


                var i = 0;
                var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(GetText("acrophobia") + " - " + GetText("submissions_closed"))
                        .WithDescription(GetText("acro_nym_was", Format.Bold(string.Join(".", game.StartingLetters)) + "\n" +
$@"--
{submissions.Aggregate("", (agg, cur) => agg + $"`{++i}.` **{cur.Key.Input}**\n")}
--"))
                        .WithFooter(efb => efb.WithText(GetText("acro_vote")));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            private async Task Game_OnEnded(Acrophobia game, ImmutableArray<KeyValuePair<AcrophobiaUser, int>> votes)
            {
                if (!votes.Any() || votes.All(x => x.Value == 0))
                {
                    await Context.Channel.SendErrorAsync(GetText("acrophobia"), GetText("acro_no_votes_cast")).ConfigureAwait(false);
                    return;
                }
                var table = votes.OrderByDescending(v => v.Value);
                var winner = table.First();
                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("acrophobia"))
                    .WithDescription(GetText("acro_winner", Format.Bold(winner.Key.UserName),
                        Format.Bold(winner.Value.ToString())))
                    .WithFooter(efb => efb.WithText(winner.Key.Input));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }

        //public enum AcroPhase
        //{
        //    Submitting,
        //    Idle, // used to wait for some other actions while transitioning through phases
        //    Voting
        //}

        ////todo 85 Isolate, this shouldn't print or anything like that.
        //public class OldAcrophobiaGame
        //{
        //    private readonly ITextChannel _channel;
        //    private readonly int _time;
        //    private readonly NadekoRandom _rng;
        //    private readonly ImmutableArray<char> _startingLetters;
        //    private readonly CancellationTokenSource _source;
        //    private AcroPhase phase { get; set; } = AcroPhase.Submitting;

        //    private readonly ConcurrentDictionary<string, IGuildUser> _submissions = new ConcurrentDictionary<string, IGuildUser>();
        //    public IReadOnlyDictionary<string, IGuildUser> Submissions => _submissions;

        //    private readonly ConcurrentHashSet<ulong> _usersWhoSubmitted = new ConcurrentHashSet<ulong>();
        //    private readonly ConcurrentHashSet<ulong> _usersWhoVoted = new ConcurrentHashSet<ulong>();

        //    private int _spamCount;

        //    //text, votes
        //    private readonly ConcurrentDictionary<string, int> _votes = new ConcurrentDictionary<string, int>();
        //    private readonly Logger _log;
        //    private readonly DiscordSocketClient _client;
        //    private readonly NadekoStrings _strings;

        //    public OldAcrophobiaGame(DiscordSocketClient client, NadekoStrings strings, ITextChannel channel, int time)
        //    {
        //        _log = LogManager.GetCurrentClassLogger();
        //        _client = client;
        //        _strings = strings;

        //        _channel = channel;
        //        _time = time;
        //        _source = new CancellationTokenSource();

        //        _rng = new NadekoRandom();
        //        var wordCount = _rng.Next(3, 6);

        //        var lettersArr = new char[wordCount];

        //        for (int i = 0; i < wordCount; i++)
        //        {
        //            var randChar = (char)_rng.Next(65, 91);
        //            lettersArr[i] = randChar == 'X' ? (char)_rng.Next(65, 88) : randChar;
        //        }
        //        _startingLetters = lettersArr.ToImmutableArray();
        //    }

        //    private Task PotentialAcro(SocketMessage arg)
        //    {
        //        var _ = Task.Run(async () =>
        //        {
        //            try
        //            {
        //                var msg = arg as SocketUserMessage;
        //                if (msg == null || msg.Author.IsBot || msg.Channel.Id != _channel.Id)
        //                    return;

        //                ++_spamCount;

        //                var guildUser = (IGuildUser)msg.Author;

        //                var input = msg.Content.ToUpperInvariant().Trim();

        //                if (phase == AcroPhase.Submitting)
        //                {
        //                    // all good. valid input. answer recorded
        //                    await _channel.SendConfirmAsync(GetText("acrophobia"),
        //                        GetText("acro_submit", guildUser.Mention,
        //                            _submissions.Count));
        //                    try
        //                    {
        //                        await msg.DeleteAsync();
        //                    }
        //                    catch
        //                    {
        //                        await msg.DeleteAsync(); //try twice
        //                    }
        //                }
        //                else if (phase == AcroPhase.Voting)
        //                {
        //                    if (_spamCount > 10)
        //                    {
        //                        _spamCount = 0;
        //                        try { await _channel.EmbedAsync(GetEmbed()).ConfigureAwait(false); }
        //                        catch { }
        //                    }

        //                    //if (submissions.TryGetValue(input, out usr) && usr.Id != guildUser.Id)
        //                    //{
        //                    //    if (!usersWhoVoted.Add(guildUser.Id))
        //                    //        return;
        //                    //    votes.AddOrUpdate(input, 1, (key, old) => ++old);
        //                    //    await channel.SendConfirmAsync("Acrophobia", $"{guildUser.Mention} cast their vote!").ConfigureAwait(false);
        //                    //    await msg.DeleteAsync().ConfigureAwait(false);
        //                    //    return;
        //                    //}

        //                    int num;
        //                    if (int.TryParse(input, out num) && num > 0 && num <= _submissions.Count)
        //                    {
        //                        var kvp = _submissions.Skip(num - 1).First();
        //                        var usr = kvp.Value;
        //                        //can't vote for yourself, can't vote multiple times
        //                        if (usr.Id == guildUser.Id || !_usersWhoVoted.Add(guildUser.Id))
        //                            return;
        //                        _votes.AddOrUpdate(kvp.Key, 1, (key, old) => ++old);
                                
        //                    }

        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                _log.Warn(ex);
        //            }
        //        });
        //        return Task.CompletedTask;
        //    }
            
        //}
    }
}