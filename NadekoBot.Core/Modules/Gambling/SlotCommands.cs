using Discord;
using Discord.Commands;
using ImageSharp;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using SixLabors.Primitives;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Common;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class SlotCommands : GamblingSubmodule<GamblingService>
        {
            private static long _totalBet;
            private static long _totalPaidOut;

            private static readonly HashSet<ulong> _runningUsers = new HashSet<ulong>();

            private const int _alphaCutOut = byte.MaxValue / 3;

            //here is a payout chart
            //https://lh6.googleusercontent.com/-i1hjAJy_kN4/UswKxmhrbPI/AAAAAAAAB1U/82wq_4ZZc-Y/DE6B0895-6FC1-48BE-AC4F-14D1B91AB75B.jpg
            //thanks to judge for helping me with this

            private readonly IImageCache _images;
            private readonly ICurrencyService _cs;

            public SlotCommands(IDataCache data, ICurrencyService cs)
            {
                _images = data.LocalImages;
                _cs = cs;
            }

            public class SlotMachine
            {
                public const int MaxValue = 5;

                static readonly List<Func<int[], int>> _winningCombos = new List<Func<int[], int>>()
                {
                    //three flowers
                    (arr) => arr.All(a=>a==MaxValue) ? 30 : 0,
                    //three of the same
                    (arr) => !arr.Any(a => a != arr[0]) ? 10 : 0,
                    //two flowers
                    (arr) => arr.Count(a => a == MaxValue) == 2 ? 4 : 0,
                    //one flower
                    (arr) => arr.Any(a => a == MaxValue) ? 1 : 0,
                };

                public static SlotResult Pull()
                {
                    var numbers = new int[3];
                    for (var i = 0; i < numbers.Length; i++)
                    {
                        numbers[i] = new NadekoRandom().Next(0, MaxValue + 1);
                    }
                    var multi = 0;
                    foreach (var t in _winningCombos)
                    {
                        multi = t(numbers);
                        if (multi != 0)
                            break;
                    }

                    return new SlotResult(numbers, multi);
                }

                public struct SlotResult
                {
                    public int[] Numbers { get; }
                    public int Multiplier { get; }
                    public SlotResult(int[] nums, int multi)
                    {
                        Numbers = nums;
                        Multiplier = multi;
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SlotStats()
            {
                //i remembered to not be a moron
                var paid = _totalPaidOut;
                var bet = _totalBet;

                if (bet <= 0)
                    bet = 1;

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("Slot Stats")
                    .AddField(efb => efb.WithName("Total Bet").WithValue(bet.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("Paid Out").WithValue(paid.ToString()).WithIsInline(true))
                    .WithFooter(efb => efb.WithText($"Payout Rate: {paid * 1.0 / bet * 100:f4}%"));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SlotTest(int tests = 1000)
            {
                if (tests <= 0)
                    return;
                //multi vs how many times it occured
                var dict = new Dictionary<int, int>();
                for (int i = 0; i < tests; i++)
                {
                    var res = SlotMachine.Pull();
                    if (dict.ContainsKey(res.Multiplier))
                        dict[res.Multiplier] += 1;
                    else
                        dict.Add(res.Multiplier, 1);
                }

                var sb = new StringBuilder();
                const int bet = 1;
                int payout = 0;
                foreach (var key in dict.Keys.OrderByDescending(x => x))
                {
                    sb.AppendLine($"x{key} occured {dict[key]} times. {dict[key] * 1.0f / tests * 100}%");
                    payout += key * dict[key];
                }
                await Context.Channel.SendConfirmAsync("Slot Test Results", sb.ToString(),
                    footer: $"Total Bet: {tests * bet} | Payout: {payout * bet} | {payout * 1.0f / tests * 100}%");
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Slot(ShmartNumber amount)
            {
                if (!_runningUsers.Add(Context.User.Id))
                    return;
                try
                {
                    if (!await CheckBetMandatory(amount))
                        return;
                    const int maxAmount = 9999;
                    if (amount > maxAmount)
                    {
                        await ReplyErrorLocalized("max_bet_limit", maxAmount + _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }

                    if (!await _cs.RemoveAsync(Context.User, "Slot Machine", amount, false, gamble: true))
                    {
                        await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                    Interlocked.Add(ref _totalBet, amount.Value);
                    using (var bgFileStream = _images.SlotBackground.ToStream())
                    {
                        var bgImage = ImageSharp.Image.Load(bgFileStream);

                        var result = SlotMachine.Pull();
                        int[] numbers = result.Numbers;

                        for (int i = 0; i < 3; i++)
                        {
                            using (var file = _images.SlotEmojis[numbers[i]].ToStream())
                            using (var randomImage = ImageSharp.Image.Load(file))
                            {
                                bgImage.DrawImage(randomImage, 100, default, new Point(95 + 142 * i, 330));
                            }
                        }

                        var won = amount * result.Multiplier;
                        var printWon = won;
                        var n = 0;
                        do
                        {
                            var digit = printWon % 10;
                            using (var fs = _images.SlotNumbers[digit].ToStream())
                            using (var img = ImageSharp.Image.Load(fs))
                            {
                                bgImage.DrawImage(img, 100, default, new Point(230 - n * 16, 462));
                            }
                            n++;
                        } while ((printWon /= 10) != 0);

                        var printAmount = amount;
                        n = 0;
                        do
                        {
                            var digit = printAmount % 10;
                            using (var fs = _images.SlotNumbers[digit].ToStream())
                            using (var img = ImageSharp.Image.Load(fs))
                            {
                                bgImage.DrawImage(img, 100, default, new Point(395 - n * 16, 462));
                            }
                            n++;
                        } while ((printAmount /= 10) != 0);

                        var msg = GetText("better_luck");
                        if (result.Multiplier != 0)
                        {
                            await _cs.AddAsync(Context.User, $"Slot Machine x{result.Multiplier}", amount * result.Multiplier, false, gamble: true);
                            Interlocked.Add(ref _totalPaidOut, amount * result.Multiplier);
                            if (result.Multiplier == 1)
                                msg = GetText("slot_single", _bc.BotConfig.CurrencySign, 1);
                            else if (result.Multiplier == 4)
                                msg = GetText("slot_two", _bc.BotConfig.CurrencySign, 4);
                            else if (result.Multiplier == 10)
                                msg = GetText("slot_three", 10);
                            else if (result.Multiplier == 30)
                                msg = GetText("slot_jackpot", 30);
                        }

                        await Context.Channel.SendFileAsync(bgImage.ToStream(), "result.png", Context.User.Mention + " " + msg + $"\n`{GetText("slot_bet")}:`{amount} `{GetText("won")}:` {amount * result.Multiplier}{_bc.BotConfig.CurrencySign}").ConfigureAwait(false);
                    }
                }
                finally
                {
                    var _ = Task.Run(async () =>
                    {
                        await Task.Delay(1500);
                        _runningUsers.Remove(Context.User.Id);
                    });
                }
            }
        }
    }
}