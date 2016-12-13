using Discord;
using Discord.Commands;
using ImageSharp;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class DriceRollCommands
        {
            private Regex dndRegex { get; } = new Regex(@"^(?<n1>\d+)d(?<n2>\d+)(?:\+(?<add>\d+))?(?:\-(?<sub>\d+))?$", RegexOptions.Compiled);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Roll(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (channel == null)
                    return;
                var rng = new NadekoRandom();
                var gen = rng.Next(1, 101);

                var num1 = gen / 10;
                var num2 = gen % 10;
                var imageStream = await Task.Run(() =>
                {
                    try
                    {
                        var ms = new MemoryStream();
                        new[] { GetDice(num1), GetDice(num2) }.Merge().SaveAsPng(ms);
                        ms.Position = 0;
                        return ms;
                    }
                    catch { return new MemoryStream(); }
                });

                await channel.SendFileAsync(imageStream, "dice.png", $"{umsg.Author.Mention} rolled " + Format.Code(gen.ToString())).ConfigureAwait(false);
            }
            //todo merge into internallDndRoll and internalRoll
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task Roll(IUserMessage umsg, string arg)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (channel == null)
                    return;

                var ordered = true;
                var rng = new NadekoRandom();
                Match match;
                if ((match = dndRegex.Match(arg)).Length != 0)
                {
                    int n1;
                    int n2;
                    if (int.TryParse(match.Groups["n1"].ToString(), out n1) &&
                        int.TryParse(match.Groups["n2"].ToString(), out n2) &&
                        n1 <= 50 && n2 <= 100000 && n1 > 0 && n2 > 0)
                    {
                        var add = 0;
                        var sub = 0;
                        int.TryParse(match.Groups["add"].Value, out add);
                        int.TryParse(match.Groups["sub"].Value, out sub);

                        var arr = new int[n1];
                        for (int i = 0; i < n1; i++)
                        {
                            arr[i] = rng.Next(1, n2 + 1) + add - sub;
                        }
                        var elemCnt = 0;
                        await channel.SendConfirmAsync($"{umsg.Author.Mention} rolled {n1} {(n1 == 1 ? "die" : "dice")} `1 to {n2}` +`{add}` -`{sub}`.\n`Result:` " + string.Join(", ", (ordered ? arr.OrderBy(x => x).AsEnumerable() : arr).Select(x => elemCnt++ % 2 == 0 ? $"**{x}**" : x.ToString()))).ConfigureAwait(false);
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task Roll(IUserMessage umsg, int num)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (channel == null)
                    return;

                var ordered = true;

                if (num < 1 || num > 30)
                {
                    await channel.SendErrorAsync("Invalid number specified. You can roll up to 1-30 dice at a time.").ConfigureAwait(false);
                    return;
                }

                var rng = new NadekoRandom();

                var dice = new List<Image>(num);
                var values = new List<int>(num);
                for (var i = 0; i < num; i++)
                {
                    var randomNumber = rng.Next(1, 7);
                    var toInsert = dice.Count;
                    if (ordered)
                    {
                        if (randomNumber == 6 || dice.Count == 0)
                            toInsert = 0;
                        else if (randomNumber != 1)
                            for (var j = 0; j < dice.Count; j++)
                            {
                                if (values[j] < randomNumber)
                                {
                                    toInsert = j;
                                    break;
                                }
                            }
                    }
                    else
                    {
                        toInsert = dice.Count;
                    }
                    dice.Insert(toInsert, GetDice(randomNumber));
                    values.Insert(toInsert, randomNumber);
                }

                var bitmap = dice.Merge();
                var ms = new MemoryStream();
                bitmap.SaveAsPng(ms);
                ms.Position = 0;
                await channel.SendFileAsync(ms, "dice.png", $"{umsg.Author.Mention} rolled {values.Count} {(values.Count == 1 ? "die" : "dice")}. Total: **{values.Sum()}** Average: **{(values.Sum() / (1.0f * values.Count)).ToString("N2")}**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Rolluo(IUserMessage umsg, string arg)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (channel == null)
                    return;

                var ordered = false;
                var rng = new NadekoRandom();
                Match match;
                if ((match = dndRegex.Match(arg)).Length != 0)
                {
                    int n1;
                    int n2;
                    if (int.TryParse(match.Groups["n1"].ToString(), out n1) &&
                        int.TryParse(match.Groups["n2"].ToString(), out n2) &&
                        n1 <= 50 && n2 <= 100000 && n1 > 0 && n2 > 0)
                    {
                        var add = 0;
                        var sub = 0;
                        int.TryParse(match.Groups["add"].Value, out add);
                        int.TryParse(match.Groups["sub"].Value, out sub);

                        var arr = new int[n1];
                        for (int i = 0; i < n1; i++)
                        {
                            arr[i] = rng.Next(1, n2 + 1) + add - sub;
                        }
                        var elemCnt = 0;
                        await channel.SendConfirmAsync($"{umsg.Author.Mention} rolled {n1} {(n1 == 1 ? "die" : "dice")} `1 to {n2}` +`{add}` -`{sub}`.\n`Result:` " + string.Join(", ", (ordered ? arr.OrderBy(x => x).AsEnumerable() : arr).Select(x => elemCnt++ % 2 == 0 ? $"**{x}**" : x.ToString()))).ConfigureAwait(false);
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Rolluo(IUserMessage umsg, int num)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (channel == null)
                    return;

                var ordered = false;

                if (num < 1 || num > 30)
                {
                    await channel.SendErrorAsync("Invalid number specified. You can roll up to 1-30 dice at a time.").ConfigureAwait(false);
                    return;
                }

                var rng = new NadekoRandom();

                var dice = new List<Image>(num);
                var values = new List<int>(num);
                for (var i = 0; i < num; i++)
                {
                    var randomNumber = rng.Next(1, 7);
                    var toInsert = dice.Count;
                    if (ordered)
                    {
                        if (randomNumber == 6 || dice.Count == 0)
                            toInsert = 0;
                        else if (randomNumber != 1)
                            for (var j = 0; j < dice.Count; j++)
                            {
                                if (values[j] < randomNumber)
                                {
                                    toInsert = j;
                                    break;
                                }
                            }
                    }
                    else
                    {
                        toInsert = dice.Count;
                    }
                    dice.Insert(toInsert, GetDice(randomNumber));
                    values.Insert(toInsert, randomNumber);
                }

                var bitmap = dice.Merge();
                var ms = new MemoryStream();
                bitmap.SaveAsPng(ms);
                ms.Position = 0;
                await channel.SendFileAsync(ms, "dice.png", $"{umsg.Author.Mention} rolled {values.Count} {(values.Count == 1 ? "die" : "dice")}. Total: **{values.Sum()}** Average: **{(values.Sum() / (1.0f * values.Count)).ToString("N2")}**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task NRoll(IUserMessage umsg, [Remainder] string range)
            {
                var channel = (ITextChannel)umsg.Channel;

                try
                {
                    int rolled;
                    if (range.Contains("-"))
                    {
                        var arr = range.Split('-')
                                        .Take(2)
                                        .Select(int.Parse)
                                        .ToArray();
                        if (arr[0] > arr[1])
                            throw new ArgumentException("First argument should be bigger than the second one.");
                        rolled = new NadekoRandom().Next(arr[0], arr[1] + 1);
                    }
                    else
                    {
                        rolled = new NadekoRandom().Next(0, int.Parse(range) + 1);
                    }

                    await channel.SendConfirmAsync($"{umsg.Author.Mention} rolled **{rolled}**.").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await channel.SendErrorAsync($":anger: {ex.Message}").ConfigureAwait(false);
                }
            }

            private Image GetDice(int num)
            {
                const string pathToImage = "data/images/dice";
                if (num != 10)
                {
                    using (var stream = File.OpenRead(Path.Combine(pathToImage, $"{num}.png")))
                        return new Image(stream);
                }

                using (var one = File.OpenRead(Path.Combine(pathToImage, "1.png")))
                using (var zero = File.OpenRead(Path.Combine(pathToImage, "0.png")))
                {
                    Image imgOne = new Image(one);
                    Image imgZero = new Image(zero);

                    return new[] { imgOne, imgZero }.Merge();
                }
            }
        }
    }
}