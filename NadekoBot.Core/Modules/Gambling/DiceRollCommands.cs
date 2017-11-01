using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using Image = ImageSharp.Image;
using ImageSharp;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class DriceRollCommands : NadekoSubmodule
        {
            private static readonly Regex dndRegex  = new Regex(@"^(?<n1>\d+)d(?<n2>\d+)(?:\+(?<add>\d+))?(?:\-(?<sub>\d+))?$", RegexOptions.Compiled);
            private static readonly Regex fudgeRegex  = new Regex(@"^(?<n1>\d+)d(?:F|f)$", RegexOptions.Compiled);

            private static readonly char[] _fateRolls = { '-', ' ', '+' };
            private readonly IImagesService _images;

            public DriceRollCommands(IImagesService images)
            {
                _images = images;
            }


            [NadekoCommand, Usage, Description, Aliases]
            public async Task Roll()
            {
                var rng = new NadekoRandom();
                var gen = rng.Next(1, 101);

                var num1 = gen / 10;
                var num2 = gen % 10;
                var imageStream = await Task.Run(() =>
                {
                    var ms = new MemoryStream();
                    new[] { GetDice(num1), GetDice(num2) }.Merge().SaveAsPng(ms);
                    ms.Position = 0;
                    return ms;
                }).ConfigureAwait(false);

                await Context.Channel.SendFileAsync(imageStream, 
                    "dice.png", 
                    Context.User.Mention + " " + GetText("dice_rolled", Format.Code(gen.ToString()))).ConfigureAwait(false);
            }

            public enum RollOrderType
            {
                Ordered,
                Unordered
            }

            [NadekoCommand, Usage, Description, Aliases]
            [Priority(1)]
            public async Task Roll(int num)
            {
                await InternalRoll(num, true).ConfigureAwait(false);
            }


            [NadekoCommand, Usage, Description, Aliases]
            [Priority(1)]
            public async Task Rolluo(int num = 1)
            {
                await InternalRoll(num, false).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [Priority(0)]
            public async Task Roll(string arg)
            {
                await InternallDndRoll(arg, true).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [Priority(0)]
            public async Task Rolluo(string arg)
            {
                await InternallDndRoll(arg, false).ConfigureAwait(false);
            }

            private async Task InternalRoll(int num, bool ordered)
            {
                if (num < 1 || num > 30)
                {
                    await ReplyErrorLocalized("dice_invalid_number", 1, 30).ConfigureAwait(false);
                    return;
                }

                var rng = new NadekoRandom();

                var dice = new List<Image<Rgba32>>(num);
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
                await Context.Channel.SendFileAsync(ms, "dice.png",
                    Context.User.Mention +  " " +
                    GetText("dice_rolled_num", Format.Bold(values.Count.ToString())) +
                    " " + GetText("total_average",
                        Format.Bold(values.Sum().ToString()),
                        Format.Bold((values.Sum() / (1.0f * values.Count)).ToString("N2")))).ConfigureAwait(false);
            }

            private async Task InternallDndRoll(string arg, bool ordered)
            {
                Match match;
                int n1;
                int n2;
                if ((match = fudgeRegex.Match(arg)).Length != 0 &&
                    int.TryParse(match.Groups["n1"].ToString(), out n1) &&
                    n1 > 0 && n1 < 500)
                {
                    var rng = new NadekoRandom();

                    var rolls = new List<char>();

                    for (int i = 0; i < n1; i++)
                    {
                        rolls.Add(_fateRolls[rng.Next(0, _fateRolls.Length)]);
                    }
                    var embed = new EmbedBuilder().WithOkColor().WithDescription(Context.User.Mention + " " + GetText("dice_rolled_num", Format.Bold(n1.ToString())))
                        .AddField(efb => efb.WithName(Format.Bold("Result"))
                            .WithValue(string.Join(" ", rolls.Select(c => Format.Code($"[{c}]")))));
                    await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
                else if ((match = dndRegex.Match(arg)).Length != 0)
                {
                    var rng = new NadekoRandom();
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
                            arr[i] = rng.Next(1, n2 + 1);
                        }

                        var sum = arr.Sum();
                        var embed = new EmbedBuilder().WithOkColor().WithDescription(Context.User.Mention + " " +GetText("dice_rolled_num", n1) + $"`1 - {n2}`")
                        .AddField(efb => efb.WithName(Format.Bold("Rolls"))
                            .WithValue(string.Join(" ", (ordered ? arr.OrderBy(x => x).AsEnumerable() : arr).Select(x => Format.Code(x.ToString())))))
                        .AddField(efb => efb.WithName(Format.Bold("Sum"))
                            .WithValue(sum + " + " + add + " - " + sub + " = " + (sum + add - sub)));
                        await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task NRoll([Remainder] string range)
            {
                int rolled;
                if (range.Contains("-"))
                {
                    var arr = range.Split('-')
                        .Take(2)
                        .Select(int.Parse)
                        .ToArray();
                    if (arr[0] > arr[1])
                    {
                        await ReplyErrorLocalized("second_larger_than_first").ConfigureAwait(false);
                        return;
                    }
                    rolled = new NadekoRandom().Next(arr[0], arr[1] + 1);
                }
                else
                {
                    rolled = new NadekoRandom().Next(0, int.Parse(range) + 1);
                }

                await ReplyConfirmLocalized("dice_rolled", Format.Bold(rolled.ToString())).ConfigureAwait(false);
            }

            private Image<Rgba32> GetDice(int num)
            {
                if (num < 0 || num > 10)
                    throw new ArgumentOutOfRangeException(nameof(num));

                if (num == 10)
                {
                    var images = _images.Dice;
                    using (var imgOneStream = images[1].ToStream())
                    using (var imgZeroStream = images[0].ToStream())
                    {
                        var imgOne = Image.Load(imgOneStream);
                        var imgZero = Image.Load(imgZeroStream);

                        return new[] { imgOne, imgZero }.Merge();
                    }
                }
                using (var die = _images.Dice[num].ToStream())
                {
                    return Image.Load(die);
                }
            }
        }
    }
}