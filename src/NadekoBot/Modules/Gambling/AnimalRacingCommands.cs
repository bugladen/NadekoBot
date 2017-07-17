using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Services.Impl;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class AnimalRacingCommands : NadekoSubmodule
        {
            private readonly BotConfig _bc;
            private readonly CurrencyService _cs;
            private readonly DiscordSocketClient _client;
            

            public static ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new ConcurrentDictionary<ulong, AnimalRace>();

            public AnimalRacingCommands(BotConfig bc, CurrencyService cs, DiscordSocketClient client)
            {
                _bc = bc;
                _cs = cs;
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Race()
            {
                var ar = new AnimalRace(Context.Guild.Id, (ITextChannel)Context.Channel, Prefix, 
                    _bc, _cs, _client,_localization, _strings);

                if (ar.Fail)
                    await ReplyErrorLocalized("race_failed_starting").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task JoinRace(int amount = 0)
            {

                if (amount < 0)
                    amount = 0;


                AnimalRace ar;
                if (!AnimalRaces.TryGetValue(Context.Guild.Id, out ar))
                {
                    await ReplyErrorLocalized("race_not_exist").ConfigureAwait(false);
                    return;
                }
                await ar.JoinRace(Context.User as IGuildUser, amount);
            }

            //todo 85 needs to be completely isolated, shouldn't use any services in the constructor,
            //then move the rest either to the module itself, or the service
            public class AnimalRace
            {

                private ConcurrentQueue<string> animals { get; }

                public bool Fail { get; set; }

                private readonly List<Participant> _participants = new List<Participant>();
                private readonly ulong _serverId;
                private int _messagesSinceGameStarted;
                private readonly string _prefix;

                private readonly Logger _log;

                private readonly ITextChannel _raceChannel;
                private readonly BotConfig _bc;
                private readonly CurrencyService _cs;
                private readonly DiscordSocketClient _client;
                private readonly ILocalization _localization;
                private readonly NadekoStrings _strings;

                public bool Started { get; private set; }

                public AnimalRace(ulong serverId, ITextChannel channel, string prefix, BotConfig bc,
                    CurrencyService cs, DiscordSocketClient client, ILocalization localization,
                    NadekoStrings strings)
                {
                    _prefix = prefix;
                    _bc = bc;
                    _cs = cs;
                    _log = LogManager.GetCurrentClassLogger();
                    _serverId = serverId;
                    _raceChannel = channel;
                    _client = client;
                    _localization = localization;
                    _strings = strings;

                    if (!AnimalRaces.TryAdd(serverId, this))
                    {
                        Fail = true;
                        return;
                    }
                    
                    animals = new ConcurrentQueue<string>(_bc.RaceAnimals.Select(ra => ra.Icon).Shuffle());


                    var cancelSource = new CancellationTokenSource();
                    var token = cancelSource.Token;
                    var fullgame = CheckForFullGameAsync(token);
                    Task.Run(async () =>
                    {
                        try
                        {
                            try
                            {
                                await _raceChannel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_starting"),
                                    footer: GetText("animal_race_join_instr", _prefix));
                            }
                            catch (Exception ex)
                            {
                                _log.Warn(ex);
                            }
                            var t = await Task.WhenAny(Task.Delay(20000, token), fullgame);
                            Started = true;
                            cancelSource.Cancel();
                            if (t == fullgame)
                            {
                                try { await _raceChannel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_full") ); } catch (Exception ex) { _log.Warn(ex); }
                            }
                            else if (_participants.Count > 1)
                            {
                                try { await _raceChannel.SendConfirmAsync(GetText("animal_race"), GetText("animal_race_starting_with_x", _participants.Count)); } catch (Exception ex) { _log.Warn(ex); }
                            }
                            else
                            {
                                try { await _raceChannel.SendErrorAsync(GetText("animal_race"), GetText("animal_race_failed")); } catch (Exception ex) { _log.Warn(ex); }
                                var p = _participants.FirstOrDefault();

                                if (p != null && p.AmountBet > 0)
                                    await _cs.AddAsync(p.User, "BetRace", p.AmountBet, false).ConfigureAwait(false);
                                End();
                                return;
                            }
                            await Task.Run(StartRace);
                            End();
                        }
                        catch { try { End(); } catch { } }
                    });
                }

                private void End()
                {
                    AnimalRace throwaway;
                    AnimalRaces.TryRemove(_serverId, out throwaway);
                }

                private async Task StartRace()
                {
                    var rng = new NadekoRandom();
                    Participant winner = null;
                    IUserMessage msg = null;
                    var place = 1;
                    try
                    {
                        _client.MessageReceived += Client_MessageReceived;

                        while (!_participants.All(p => p.Total >= 60))
                        {
                            //update the state
                            _participants.ForEach(p =>
                            {
                                p.Total += 1 + rng.Next(0, 10);
                            });


                            _participants
                                .OrderByDescending(p => p.Total)
                                .ForEach(p =>
                                {
                                    if (p.Total > 60)
                                    {
                                        if (winner == null)
                                        {
                                            winner = p;
                                        }
                                        p.Total = 60;
                                        if (p.Place == 0)
                                            p.Place = place++;
                                    }
                                });


                            //draw the state

                            var text = $@"|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|
{String.Join("\n", _participants.Select(p => $"{(int)(p.Total / 60f * 100),-2}%|{p.ToString()}"))}
|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|";
                            if (msg == null || _messagesSinceGameStarted >= 10) // also resend the message if channel was spammed
                            {
                                if (msg != null)
                                    try { await msg.DeleteAsync(); } catch { }
                                _messagesSinceGameStarted = 0;
                                try { msg = await _raceChannel.SendMessageAsync(text).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                            }
                            else
                            {
                                try { await msg.ModifyAsync(m => m.Content = text).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                            }

                            await Task.Delay(2500);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    finally
                    {
                        _client.MessageReceived -= Client_MessageReceived;
                    }

                    if (winner != null)
                    {
                        if (winner.AmountBet > 0)
                        {
                            var wonAmount = winner.AmountBet * (_participants.Count - 1);

                            await _cs.AddAsync(winner.User, "Won a Race", wonAmount, true)
                                .ConfigureAwait(false);
                            await _raceChannel.SendConfirmAsync(GetText("animal_race"),
                                    Format.Bold(GetText("animal_race_won_money", winner.User.Mention,
                                        winner.Animal, wonAmount + _bc.CurrencySign)))
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await _raceChannel.SendConfirmAsync(GetText("animal_race"),
                                Format.Bold(GetText("animal_race_won", winner.User.Mention, winner.Animal))).ConfigureAwait(false);
                        }
                    }

                }

                private Task Client_MessageReceived(SocketMessage imsg)
                {
                    var _ = Task.Run(() =>
                    {
                        var msg = imsg as SocketUserMessage;
                        if (msg == null)
                            return Task.CompletedTask;
                        if ((msg.Author.Id == _client.CurrentUser.Id) || !(imsg.Channel is ITextChannel) || imsg.Channel != _raceChannel)
                            return Task.CompletedTask;
                        Interlocked.Increment(ref _messagesSinceGameStarted);
                        return Task.CompletedTask;
                    });
                    return Task.CompletedTask;
                }

                private async Task CheckForFullGameAsync(CancellationToken cancelToken)
                {
                    while (animals.Count > 0)
                    {
                        await Task.Delay(100, cancelToken);
                    }
                }

                public async Task JoinRace(IGuildUser u, int amount = 0)
                {
                    string animal;
                    if (!animals.TryDequeue(out animal))
                    {
                        await _raceChannel.SendErrorAsync(GetText("animal_race_no_race")).ConfigureAwait(false);
                        return;
                    }
                    var p = new Participant(u, animal, amount);
                    if (_participants.Contains(p))
                    {
                        await _raceChannel.SendErrorAsync(GetText("animal_race_already_in")).ConfigureAwait(false);
                        return;
                    }
                    if (Started)
                    {
                        await _raceChannel.SendErrorAsync(GetText("animal_race_already_started")).ConfigureAwait(false);
                        return;
                    }
                    if (amount > 0)
                        if (!await _cs.RemoveAsync(u, "BetRace", amount, false).ConfigureAwait(false))
                        {
                            await _raceChannel.SendErrorAsync(GetText("not_enough", _bc.CurrencySign)).ConfigureAwait(false);
                            return;
                        }
                    _participants.Add(p);
                    string confStr;
                    if (amount > 0)
                        confStr = GetText("animal_race_join_bet", u.Mention, p.Animal, amount + _bc.CurrencySign);
                    else
                        confStr = GetText("animal_race_join", u.Mention, p.Animal);
                    await _raceChannel.SendConfirmAsync(GetText("animal_race"), Format.Bold(confStr)).ConfigureAwait(false);
                }

                private string GetText(string text)
                    => _strings.GetText(text,
                        _localization.GetCultureInfo(_raceChannel.Guild),
                        typeof(Gambling).Name.ToLowerInvariant());

                private string GetText(string text, params object[] replacements)
                    => _strings.GetText(text,
                        _localization.GetCultureInfo(_raceChannel.Guild),
                        typeof(Gambling).Name.ToLowerInvariant(),
                        replacements);
            }

            public class Participant
            {
                public IGuildUser User { get; }
                public string Animal { get; }
                public int AmountBet { get; }

                public float Coeff { get; set; }
                public int Total { get; set; }

                public int Place { get; set; }

                public Participant(IGuildUser u, string a, int amount)
                {
                    User = u;
                    Animal = a;
                    AmountBet = amount;
                }

                public override int GetHashCode() => User.GetHashCode();

                public override bool Equals(object obj)
                {
                    var p = obj as Participant;
                    return p != null && p.User == User;
                }

                public override string ToString()
                {
                    var str = new string('‣', Total) + Animal;
                    if (Place == 0)
                        return str;

                    str += $"`#{Place}`";

                    if (Place == 1)
                        str += "🏆";

                    return str;
                }
            }
        }
    }
}