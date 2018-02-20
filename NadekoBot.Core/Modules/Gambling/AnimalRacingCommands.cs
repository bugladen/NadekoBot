using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Common;
using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Modules.Gambling.Common.AnimalRacing;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling.Common.AnimalRacing;
using NadekoBot.Modules.Gambling.Common.AnimalRacing.Exceptions;
using NadekoBot.Modules.Gambling.Services;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class AnimalRacingCommands : GamblingSubmodule<AnimalRaceService>
        {
            private readonly ICurrencyService _cs;
            private readonly DiscordSocketClient _client;

            public AnimalRacingCommands(ICurrencyService cs, DiscordSocketClient client)
            {
                _cs = cs;
                _client = client;
            }

            private IUserMessage raceMessage = null;

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NadekoOptions(typeof(RaceOptions))]
            public Task Race(params string[] args)
            {
                var (options, success) = OptionsParser.Default.ParseFrom(new RaceOptions(), args);

                var ar = new AnimalRace(options, _cs, _bc.BotConfig.RaceAnimals.Shuffle().ToArray());
                if (!_service.AnimalRaces.TryAdd(Context.Guild.Id, ar))
                    return Context.Channel.SendErrorAsync(GetText("animal_race"), GetText("animal_race_already_started"));

                ar.Initialize();

                var count = 0;
                Task _client_MessageReceived(SocketMessage arg)
                {
                    var _ = Task.Run(() =>
                    {
                        try
                        {
                            if (arg.Channel.Id == Context.Channel.Id)
                            {
                                if (ar.CurrentPhase == AnimalRace.Phase.Running && ++count % 9 == 0)
                                {
                                    raceMessage = null;
                                }
                            }
                        }
                        catch { }
                    });
                    return Task.CompletedTask;
                }

                Task Ar_OnEnded(AnimalRace race)
                {
                    _client.MessageReceived -= _client_MessageReceived;
                    _service.AnimalRaces.TryRemove(Context.Guild.Id, out _);
                    var winner = race.FinishedUsers[0];
                    if (race.FinishedUsers[0].Bet > 0)
                    {
                        return Context.Channel.SendConfirmAsync(GetText("animal_race"),
                                            GetText("animal_race_won_money", Format.Bold(winner.Username),
                                                winner.Animal.Icon, (race.FinishedUsers[0].Bet * (race.Users.Length - 1)) + _bc.BotConfig.CurrencySign));
                    }
                    else
                    {
                        return Context.Channel.SendConfirmAsync(GetText("animal_race"),
                            GetText("animal_race_won", Format.Bold(winner.Username), winner.Animal.Icon));
                    }
                }

                ar.OnStartingFailed += Ar_OnStartingFailed;
                ar.OnStateUpdate += Ar_OnStateUpdate;
                ar.OnEnded += Ar_OnEnded;
                ar.OnStarted += Ar_OnStarted;
                _client.MessageReceived += _client_MessageReceived;

                return Context.Channel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_starting", options.StartTime),
                                    footer: GetText("animal_race_join_instr", Prefix));
            }

            private Task Ar_OnStarted(AnimalRace race)
            {
                if (race.Users.Length == race.MaxUsers)
                    return Context.Channel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_full"));
                else
                    return Context.Channel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_starting_with_x", race.Users.Length));
            }

            private async Task Ar_OnStateUpdate(AnimalRace race)
            {
                var text = $@"|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|
{String.Join("\n", race.Users.Select(p =>
                {
                    var index = race.FinishedUsers.IndexOf(p);
                    var extra = (index == -1 ? "" : $"#{index + 1} {(index == 0 ? "🏆" : "")}");
                    return $"{(int)(p.Progress / 60f * 100),-2}%|{new string('‣', p.Progress) + p.Animal.Icon + extra}";
                }))}
|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|";

                var msg = raceMessage;

                if (msg == null)
                    raceMessage = await Context.Channel.SendConfirmAsync(text)
                        .ConfigureAwait(false);
                else
                    await msg.ModifyAsync(x => x.Embed = new EmbedBuilder()
                        .WithTitle(GetText("animal_race"))
                        .WithDescription(text)
                        .WithOkColor()
                        .Build())
                            .ConfigureAwait(false);
            }

            private Task Ar_OnStartingFailed(AnimalRace race)
            {
                _service.AnimalRaces.TryRemove(Context.Guild.Id, out _);
                return ReplyErrorLocalized("animal_race_failed");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task JoinRace(ShmartNumber amount = default)
            {
                if (!await CheckBetOptional(amount))
                    return;

                if (!_service.AnimalRaces.TryGetValue(Context.Guild.Id, out var ar))
                {
                    await ReplyErrorLocalized("race_not_exist").ConfigureAwait(false);
                    return;
                }
                try
                {
                    var user = await ar.JoinRace(Context.User.Id, Context.User.ToString(), amount)
                        .ConfigureAwait(false);
                    if (amount > 0)
                        await Context.Channel.SendConfirmAsync(GetText("animal_race_join_bet", Context.User.Mention, user.Animal.Icon, amount + _bc.BotConfig.CurrencySign)).ConfigureAwait(false);
                    else
                        await Context.Channel.SendConfirmAsync(GetText("animal_race_join", Context.User.Mention, user.Animal.Icon)).ConfigureAwait(false);
                }
                catch (ArgumentOutOfRangeException)
                {
                    //ignore if user inputed an invalid amount
                }
                catch (AlreadyJoinedException)
                {
                    // just ignore this
                }
                catch (AlreadyStartedException)
                {
                    //ignore
                }
                catch (AnimalRaceFullException)
                {
                    await Context.Channel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_full"))
                        .ConfigureAwait(false);
                }
                catch (NotEnoughFundsException)
                {
                    await Context.Channel.SendErrorAsync(GetText("not_enough", _bc.BotConfig.CurrencySign)).ConfigureAwait(false);
                }
            }
        }
    }
}