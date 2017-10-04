using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Games.Common.Acrophobia;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class AcropobiaCommands : NadekoSubmodule<GamesService>
        {
            private readonly DiscordSocketClient _client;

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
                if (_service.AcrophobiaGames.TryAdd(channel.Id, game))
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
                        _service.AcrophobiaGames.TryRemove(channel.Id, out game);
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
    }
}