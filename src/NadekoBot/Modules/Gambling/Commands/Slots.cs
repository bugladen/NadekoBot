using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class Slots : ModuleBase
        {
            private static int totalBet = 0;
            private static int totalPaidOut = 0;

            private const string backgroundPath = "data/slots/background.png";

            private static readonly byte[] backgroundBuffer;
            private static readonly byte[][] numbersBuffer = new byte[10][];
            private static readonly byte[][] emojiBuffer;

            const int alphaCutOut = byte.MaxValue / 3;

            static Slots()
            {
                backgroundBuffer = File.ReadAllBytes(backgroundPath);

                for (int i = 0; i < 10; i++)
                {
                    numbersBuffer[i] = File.ReadAllBytes("data/slots/" + i + ".png");
                }
                int throwaway;
                var emojiFiles = Directory.GetFiles("data/slots/emojis/", "*.png")
                    .Where(f => int.TryParse(Path.GetFileNameWithoutExtension(f), out throwaway))
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .ToArray();

                emojiBuffer = new byte[emojiFiles.Length][];
                for (int i = 0; i < emojiFiles.Length; i++)
                {
                    emojiBuffer[i] = File.ReadAllBytes(emojiFiles[i]);
                }
            }


            private static MemoryStream InternalGetStream(string path)
            {
                var ms = new MemoryStream();
                using (var fs = File.Open(path, FileMode.Open))
                {
                    fs.CopyTo(ms);
                    fs.Flush();
                }
                ms.Position = 0;
                return ms;
            }

            //here is a payout chart 
            //https://lh6.googleusercontent.com/-i1hjAJy_kN4/UswKxmhrbPI/AAAAAAAAB1U/82wq_4ZZc-Y/DE6B0895-6FC1-48BE-AC4F-14D1B91AB75B.jpg
            //thanks to judge for helping me with this

            public class SlotMachine
            {
                public const int MaxValue = 5;

                static readonly List<Func<int[], int>> winningCombos = new List<Func<int[], int>>()
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
                    for (int i = 0; i < numbers.Length; i++)
                    {
                        numbers[i] = new NadekoRandom().Next(0, MaxValue + 1);
                    }
                    int multi = 0;
                    for (int i = 0; i < winningCombos.Count; i++)
                    {
                        multi = winningCombos[i](numbers);
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
                        this.Numbers = nums;
                        this.Multiplier = multi;
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SlotStats()
            {
                //i remembered to not be a moron
                var paid = totalPaidOut;
                var bet = totalBet;

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
                foreach (var key in dict.Keys.OrderByDescending(x=>x))
                {
                    sb.AppendLine($"x{key} occured {dict[key]} times. {dict[key] * 1.0f / tests * 100}%");
                    payout += key * dict[key];
                }
                await Context.Channel.SendConfirmAsync("Slot Test Results", sb.ToString(),
                    footer: $"Total Bet: {tests * bet} | Payout: {payout * bet} | {payout * 1.0f / tests * 100}%");
            }

            static HashSet<ulong> runningUsers = new HashSet<ulong>();
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Slot(int amount = 0)
            {
                if (!runningUsers.Add(Context.User.Id))
                    return;
                try
                {
                    if (amount < 1)
                    {
                        await Context.Channel.SendErrorAsync($"You can't bet less than 1{NadekoBot.BotConfig.CurrencySign}").ConfigureAwait(false);
                        return;
                    }

                    if (amount > 999)
                    {
                        await Context.Channel.SendErrorAsync($"You can't bet more than 999{NadekoBot.BotConfig.CurrencySign}").ConfigureAwait(false);
                        return;
                    }

                    if (!await CurrencyHandler.RemoveCurrencyAsync(Context.User, "Slot Machine", amount, false))
                        return;
                    Interlocked.Add(ref totalBet, amount);
                    using (var bgFileStream = new MemoryStream(backgroundBuffer))
                    {
                        var bgImage = new ImageSharp.Image(bgFileStream);

                        var result = SlotMachine.Pull();
                        int[] numbers = result.Numbers;
                        using (var bgPixels = bgImage.Lock())
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                using (var file = new MemoryStream(emojiBuffer[numbers[i]]))
                                {
                                    var randomImage = new ImageSharp.Image(file);
                                    using (var toAdd = randomImage.Lock())
                                    {
                                        for (int j = 0; j < toAdd.Width; j++)
                                        {
                                            for (int k = 0; k < toAdd.Height; k++)
                                            {
                                                var x = 95 + 142 * i + j;
                                                int y = 330 + k;
                                                var toSet = toAdd[j, k];
                                                if (toSet.A < alphaCutOut)
                                                    continue;
                                                bgPixels[x, y] = toAdd[j, k];
                                            }
                                        }
                                    }
                                }
                            }

                            var won = amount * result.Multiplier;
                            var printWon = won;
                            var n = 0;
                            do
                            {
                                var digit = printWon % 10;
                                using (var fs = new MemoryStream(numbersBuffer[digit]))
                                {
                                    var img = new ImageSharp.Image(fs);
                                    using (var pixels = img.Lock())
                                    {
                                        for (int i = 0; i < pixels.Width; i++)
                                        {
                                            for (int j = 0; j < pixels.Height; j++)
                                            {
                                                if (pixels[i, j].A < alphaCutOut)
                                                    continue;
                                                var x = 230 - n * 16 + i;
                                                bgPixels[x, 462 + j] = pixels[i, j];
                                            }
                                        }
                                    }
                                }
                                n++;
                            } while ((printWon /= 10) != 0);

                            var printAmount = amount;
                            n = 0;
                            do
                            {
                                var digit = printAmount % 10;
                                using (var fs = new MemoryStream(numbersBuffer[digit]))
                                {
                                    var img = new ImageSharp.Image(fs);
                                    using (var pixels = img.Lock())
                                    {
                                        for (int i = 0; i < pixels.Width; i++)
                                        {
                                            for (int j = 0; j < pixels.Height; j++)
                                            {
                                                if (pixels[i, j].A < alphaCutOut)
                                                    continue;
                                                var x = 395 - n * 16 + i;
                                                bgPixels[x, 462 + j] = pixels[i, j];
                                            }
                                        }
                                    }
                                }
                                n++;
                            } while ((printAmount /= 10) != 0);
                        }

                        var msg = "Better luck next time ^_^";
                        if (result.Multiplier != 0)
                        {
                            await CurrencyHandler.AddCurrencyAsync(Context.User, $"Slot Machine x{result.Multiplier}", amount * result.Multiplier, false);
                            Interlocked.Add(ref totalPaidOut, amount * result.Multiplier);
                            if (result.Multiplier == 1)
                                msg = $"A single {NadekoBot.BotConfig.CurrencySign}, x1 - Try again!";
                            else if (result.Multiplier == 4)
                                msg = $"Good job! Two {NadekoBot.BotConfig.CurrencySign} - bet x4";
                            else if (result.Multiplier == 10)
                                msg = "Wow! Lucky! Three of a kind! x10";
                            else if (result.Multiplier == 30)
                                msg = "WOAAHHHHHH!!! Congratulations!!! x30";
                        }

                        await Context.Channel.SendFileAsync(bgImage.ToStream(), "result.png", Context.User.Mention + " " + msg + $"\n`Bet:`{amount} `Won:` {amount * result.Multiplier}{NadekoBot.BotConfig.CurrencySign}").ConfigureAwait(false);
                    }
                }
                finally
                {
                    var t = Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        runningUsers.Remove(Context.User.Id);
                    });                    
                }
            }
        }
    }
}
