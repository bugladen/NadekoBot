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
        public class Acropobia : ModuleBase
        {
            //channelId, game
            public static ConcurrentDictionary<ulong, AcrophobiaGame> AcrophobiaGames { get; } = new ConcurrentDictionary<ulong, AcrophobiaGame>();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Acro(int time = 60)
            {
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
                    await channel.SendErrorAsync("Acrophobia game is already running in this channel.").ConfigureAwait(false);
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
            private readonly ITextChannel channel;
            private readonly int time;
            private readonly NadekoRandom rng;
            private readonly ImmutableArray<char> startingLetters;
            private readonly CancellationTokenSource source;
            private AcroPhase phase { get; set; } = AcroPhase.Submitting;

            private readonly ConcurrentDictionary<string, IGuildUser> submissions = new ConcurrentDictionary<string, IGuildUser>();
            public IReadOnlyDictionary<string, IGuildUser> Submissions => submissions;

            private readonly ConcurrentHashSet<ulong> usersWhoVoted = new ConcurrentHashSet<ulong>();

            private int spamCount = 0;

            //text, votes
            private readonly ConcurrentDictionary<string, int> votes = new ConcurrentDictionary<string, int>();
            private readonly Logger _log;

            public AcrophobiaGame(ITextChannel channel, int time)
            {
                this._log = LogManager.GetCurrentClassLogger();

                this.channel = channel;
                this.time = time;
                this.source = new CancellationTokenSource();

                this.rng = new NadekoRandom();
                var wordCount = rng.Next(3, 6);

                var lettersArr = new char[wordCount];

                for (int i = 0; i < wordCount; i++)
                {
                    var randChar = (char)rng.Next(65, 91);
                    lettersArr[i] = randChar == 'X' ? (char)rng.Next(65, 88) : randChar;
                }
                startingLetters = lettersArr.ToImmutableArray();
            }

            private EmbedBuilder GetEmbed()
            {
                var i = 0;
                return phase == AcroPhase.Submitting

                ? new EmbedBuilder().WithOkColor()
                    .WithTitle("Acrophobia")
                    .WithDescription($"Game started. Create a sentence with the following acronym: **{string.Join(".", startingLetters)}.**\n")
                    .WithFooter(efb => efb.WithText("You have " + this.time + " seconds to make a submission."))

                : new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("Acrophobia - Submissions Closed")
                    .WithDescription($@"Acronym was **{string.Join(".", startingLetters)}.**
--
{this.submissions.Aggregate("", (agg, cur) => agg + $"`{++i}.` **{cur.Key.ToLowerInvariant().ToTitleCase()}**\n")}
--")
                    .WithFooter(efb => efb.WithText("Vote by typing a number of the submission"));
            }

            public async Task Run()
            {
                NadekoBot.Client.MessageReceived += PotentialAcro;
                var embed = GetEmbed();

                //SUBMISSIONS PHASE
                await channel.EmbedAsync(embed).ConfigureAwait(false);
                try
                {
                    await Task.Delay(time * 1000, source.Token).ConfigureAwait(false);
                    phase = AcroPhase.Idle;
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                //var i = 0;
                if (submissions.Count == 0)
                {
                    await channel.SendErrorAsync("Acrophobia", "Game ended with no submissions.");
                    return;
                }
                else if (submissions.Count == 1)
                {
                    await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithDescription($"{submissions.First().Value.Mention} is the winner for being the only user who made a submission!")
                        .WithFooter(efb => efb.WithText(submissions.First().Key.ToLowerInvariant().ToTitleCase())))
                            .ConfigureAwait(false);
                    return;
                }
                var submissionClosedEmbed = GetEmbed();

                await channel.EmbedAsync(submissionClosedEmbed).ConfigureAwait(false);

                //VOTING PHASE
                this.phase = AcroPhase.Voting;
                try
                {
                    //30 secondds for voting
                    await Task.Delay(30000, source.Token).ConfigureAwait(false);
                    this.phase = AcroPhase.Idle;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                await End().ConfigureAwait(false);
            }

            private async void PotentialAcro(SocketMessage arg)
            {
                try
                {
                    var msg = arg as SocketUserMessage;
                    if (msg == null || msg.Author.IsBot || msg.Channel.Id != channel.Id)
                        return;

                    ++spamCount;

                    var guildUser = (IGuildUser)msg.Author;

                    var input = msg.Content.ToUpperInvariant().Trim();

                    if (phase == AcroPhase.Submitting)
                    {
                        if (spamCount > 10)
                        {
                            spamCount = 0;
                            try { await channel.EmbedAsync(GetEmbed()).ConfigureAwait(false); }
                            catch { }
                        }
                        //user didn't input something already
                        IGuildUser throwaway;
                        if (submissions.TryGetValue(input, out throwaway))
                            return;
                        var inputWords = input.Split(' '); //get all words

                        if (inputWords.Length != startingLetters.Length) // number of words must be the same as the number of the starting letters
                            return;

                        for (int i = 0; i < startingLetters.Length; i++)
                        {
                            var letter = startingLetters[i];

                            if (!inputWords[i].StartsWith(letter.ToString())) // all first letters must match
                                return;
                        }

                        //try adding it to the list of answers
                        if (!submissions.TryAdd(input, guildUser))
                            return;

                        // all good. valid input. answer recorded
                        await channel.SendConfirmAsync("Acrophobia", $"{guildUser.Mention} submitted their sentence. ({submissions.Count} total)");
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
                        if (spamCount > 10)
                        {
                            spamCount = 0;
                            try { await channel.EmbedAsync(GetEmbed()).ConfigureAwait(false); }
                            catch { }
                        }

                        IGuildUser usr;
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
                        if (int.TryParse(input, out num) && num > 0 && num <= submissions.Count)
                        {
                            var kvp = submissions.Skip(num - 1).First();
                            usr = kvp.Value;
                            //can't vote for yourself, can't vote multiple times
                            if (usr.Id == guildUser.Id || !usersWhoVoted.Add(guildUser.Id))
                                return;
                            votes.AddOrUpdate(kvp.Key, 1, (key, old) => ++old);
                            await channel.SendConfirmAsync("Acrophobia", $"{guildUser.Mention} cast their vote!").ConfigureAwait(false);
                            await msg.DeleteAsync().ConfigureAwait(false);
                            return;
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
                if (!votes.Any())
                {
                    await channel.SendErrorAsync("Acrophobia", "No votes cast. Game ended with no winner.").ConfigureAwait(false);
                    return;
                }
                var table = votes.OrderByDescending(v => v.Value);
                var winner = table.First();
                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle("Acrophobia")
                    .WithDescription($"Winner is {submissions[winner.Key].Mention} with {winner.Value} points.\n")
                    .WithFooter(efb => efb.WithText(winner.Key.ToLowerInvariant().ToTitleCase()));

                await channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            public void EnsureStopped()
            {
                NadekoBot.Client.MessageReceived -= PotentialAcro;
                if (!source.IsCancellationRequested)
                    source.Cancel();
            }
        }
    }
}