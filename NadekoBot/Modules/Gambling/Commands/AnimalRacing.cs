using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling.Commands
{
    class AnimalRacing : DiscordCommand
    {
        public static ConcurrentDictionary<ulong, AnimalRace> AnimalRaces = new ConcurrentDictionary<ulong, AnimalRace>();

        public AnimalRacing(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Prefix + "race")
                .Description($"Starts a new animal race. | `{Prefix}race`")
                .Do(e =>
                {
                    var ar = new AnimalRace(e.Server.Id, e.Channel);
                    if (ar.Fail)
                    {
                        return;
                    }
                });


            cgb.CreateCommand(Prefix + "joinrace")
                .Alias(Prefix + "jr")
                .Description($"Joins a new race. You can specify an amount of flowers for betting (optional). You will get YourBet*(participants-1) back if you win. | `{Prefix}jr` or `{Prefix}jr 5`")
                .Parameter("amount", ParameterType.Optional)
                .Do(async e =>
                {

                    int amount;
                    if (!int.TryParse(e.GetArg("amount"), out amount) || amount < 0)
                        amount = 0;

                    var userFlowers = GamblingModule.GetUserFlowers(e.User.Id);

                    if (userFlowers < amount)
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
                        return;
                    }

                    if (amount > 0)
                        await FlowersHandler.RemoveFlowers(e.User, "BetRace", (int)amount, true).ConfigureAwait(false);

                    AnimalRace ar;
                    if (!AnimalRaces.TryGetValue(e.Server.Id, out ar))
                    {
                        await e.Channel.SendMessage("No race exists on this server");
                        return;
                    }
                    await ar.JoinRace(e.User, amount);

                });
        }

        public class AnimalRace
        {

            private ConcurrentQueue<string> animals = new ConcurrentQueue<string>(NadekoBot.Config.RaceAnimals.Shuffle());

            public bool Fail { get; internal set; }

            public List<Participant> participants = new List<Participant>();
            private ulong serverId;
            private int messagesSinceGameStarted = 0;

            public Channel raceChannel { get; set; }
            public bool Started { get; private set; } = false;

            public AnimalRace(ulong serverId, Channel ch)
            {
                this.serverId = serverId;
                this.raceChannel = ch;
                if (!AnimalRaces.TryAdd(serverId, this))
                {
                    Fail = true;
                    return;
                }
                var cancelSource = new CancellationTokenSource();
                var token = cancelSource.Token;
                var fullgame = CheckForFullGameAsync(token);
                Task.Run(async () =>
                {
                    try
                    {
                        await raceChannel.SendMessage($"🏁`Race is starting in 20 seconds or when the room is full. Type {NadekoBot.Config.CommandPrefixes.Gambling}jr to join the race.`");
                        var t = await Task.WhenAny(Task.Delay(20000, token), fullgame);
                        Started = true;
                        cancelSource.Cancel();
                        if (t == fullgame)
                        {
                            await raceChannel.SendMessage("🏁`Race full, starting right now!`");
                        }
                        else if (participants.Count > 1)
                        {
                            await raceChannel.SendMessage("🏁`Game starting with " + participants.Count + " participants.`");
                        }
                        else
                        {
                            await raceChannel.SendMessage("🏁`Race failed to start since there was not enough participants.`");
                            var p = participants.FirstOrDefault();
                            if (p != null)
                                await FlowersHandler.AddFlowersAsync(p.User, "BetRace", p.AmountBet, true).ConfigureAwait(false);
                            End();
                            return;
                        }
                        await Task.Run(StartRace);
                        End();
                    }
                    catch { }
                });
            }

            private void End()
            {
                AnimalRace throwaway;
                AnimalRaces.TryRemove(serverId, out throwaway);
            }

            private async Task StartRace()
            {
                var rng = new Random();
                Participant winner = null;
                Message msg = null;
                int place = 1;
                try
                {
                    NadekoBot.Client.MessageReceived += Client_MessageReceived;

                    while (!participants.All(p => p.Total >= 60))
                    {
                        //update the state
                        participants.ForEach(p =>
                        {

                            p.Total += 1 + rng.Next(0, 10);
                            if (p.Total > 60)
                            {
                                p.Total = 60;
                                if (winner == null)
                                {
                                    winner = p;
                                }
                                if (p.Place == 0)
                                    p.Place = place++;
                            }
                        });


                        //draw the state

                        var text = $@"|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|
{String.Join("\n", participants.Select(p => $"{(int)(p.Total / 60f * 100),-2}%|{p.ToString()}"))}
|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|";
                        if (msg == null || messagesSinceGameStarted >= 10) // also resend the message if channel was spammed
                        {
                            if (msg != null)
                                try { await msg.Delete(); } catch { }
                            msg = await raceChannel.SendMessage(text);
                            messagesSinceGameStarted = 0;
                        }
                        else
                            await msg.Edit(text);

                        await Task.Delay(2500);
                    }
                }
                finally
                {
                    NadekoBot.Client.MessageReceived -= Client_MessageReceived;
                }

                if (winner.AmountBet > 0)
                {
                    var wonAmount = winner.AmountBet * (participants.Count - 1);
                    await FlowersHandler.AddFlowersAsync(winner.User, "Won a Race", wonAmount).ConfigureAwait(false);
                    await raceChannel.SendMessage($"🏁 {winner.User.Mention} as {winner.Animal} **Won the race and {wonAmount}{NadekoBot.Config.CurrencySign}!**");
                }
                else
                {
                    await raceChannel.SendMessage($"🏁 {winner.User.Mention} as {winner.Animal} **Won the race!**");
                }

            }

            private void Client_MessageReceived(object sender, MessageEventArgs e)
            {
                if (e.Message.IsAuthor || e.Channel.IsPrivate || e.Channel != raceChannel)
                    return;
                messagesSinceGameStarted++;
            }

            private async Task CheckForFullGameAsync(CancellationToken cancelToken)
            {
                while (animals.Count > 0)
                {
                    await Task.Delay(100, cancelToken);
                }
            }

            public async Task<bool> JoinRace(User u, int amount = 0)
            {
                var animal = "";
                if (!animals.TryDequeue(out animal))
                {
                    await raceChannel.SendMessage($"{u.Mention} `There is no running race on this server.`");
                    return false;
                }
                var p = new Participant(u, animal, amount);
                if (participants.Contains(p))
                {
                    await raceChannel.SendMessage($"{u.Mention} `You already joined this race.`");
                    return false;
                }
                if (Started)
                {
                    await raceChannel.SendMessage($"{u.Mention} `Race is already started`");
                    return false;
                }
                participants.Add(p);
                await raceChannel.SendMessage($"{u.Mention} **joined the race as a {p.Animal}" + (amount > 0 ? $" and bet {amount} {NadekoBot.Config.CurrencyName.SnPl(amount)}!**" : "**"));
                return true;
            }
        }

        public class Participant
        {
            public User User { get; set; }
            public string Animal { get; set; }
            public int AmountBet { get; set; }

            public float Coeff { get; set; }
            public int Total { get; set; }

            public int Place { get; set; } = 0;

            public Participant(User u, string a, int amount)
            {
                this.User = u;
                this.Animal = a;
                this.AmountBet = amount;
            }

            public override int GetHashCode()
            {
                return User.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var p = obj as Participant;
                return p == null ?
                    false :
                    p.User == User;
            }

            public override string ToString()
            {
                var str = new string('‣', Total) + Animal;
                if (Place == 0)
                    return str;
                if (Place == 1)
                {
                    return str + "🏆";
                }
                else if (Place == 2)
                {
                    return str + "`2nd`";
                }
                else if (Place == 3)
                {
                    return str + "`3rd`";
                }
                else
                {
                    return str + $"`{Place}th`";
                }

            }
        }
    }
}
