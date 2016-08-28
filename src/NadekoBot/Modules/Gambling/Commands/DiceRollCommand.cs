using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        private Regex dndRegex { get; } = new Regex(@"(?<n1>\d+)d(?<n2>\d+)", RegexOptions.Compiled);
        ////todo drawing
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public Task Roll(IUserMessage umsg, [Remainder] string arg = null) =>
        //    InternalRoll(umsg, arg, true);

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public Task Rolluo(IUserMessage umsg, [Remainder] string arg = null) =>
        //    InternalRoll(umsg, arg, false);

        //private async Task InternalRoll(IUserMessage umsg, string arg, bool ordered) {
        //    var channel = (ITextChannel)umsg.Channel;
        //    var r = new Random();
        //    if (string.IsNullOrWhiteSpace(arg))
        //    {
        //        var gen = r.Next(0, 101);

        //        var num1 = gen / 10;
        //        var num2 = gen % 10;

        //        var imageStream = await new Image[2] { GetDice(num1), GetDice(num2) }.Merge().ToStream(ImageFormat.Png);

        //        await channel.SendFileAsync(imageStream, "dice.png").ConfigureAwait(false);
        //        return;
        //    }
        //    Match m;
        //    if ((m = dndRegex.Match(arg)).Length != 0)
        //    {
        //        int n1;
        //        int n2;
        //        if (int.TryParse(m.Groups["n1"].ToString(), out n1) &&
        //            int.TryParse(m.Groups["n2"].ToString(), out n2) &&
        //            n1 <= 50 && n2 <= 100000 && n1 > 0 && n2 > 0)
        //        {
        //            var arr = new int[n1];
        //            for (int i = 0; i < n1; i++)
        //            {
        //                arr[i] = r.Next(1, n2 + 1);
        //            }
        //            var elemCnt = 0;
        //            await channel.SendMessageAsync($"`Rolled {n1} {(n1 == 1 ? "die" : "dice")} 1-{n2}.`\n`Result:` " + string.Join(", ", (ordered ? arr.OrderBy(x => x).AsEnumerable() : arr).Select(x => elemCnt++ % 2 == 0 ? $"**{x}**" : x.ToString()))).ConfigureAwait(false);
        //        }
        //        return;
        //    }
        //    try
        //    {
        //        var num = int.Parse(e.Args[0]);
        //        if (num < 1) num = 1;
        //        if (num > 30)
        //        {
        //            await channel.SendMessageAsync("You can roll up to 30 dice at a time.").ConfigureAwait(false);
        //            num = 30;
        //        }
        //        var dices = new List<Image>(num);
        //        var values = new List<int>(num);
        //        for (var i = 0; i < num; i++)
        //        {
        //            var randomNumber = r.Next(1, 7);
        //            var toInsert = dices.Count;
        //            if (ordered)
        //            {
        //                if (randomNumber == 6 || dices.Count == 0)
        //                    toInsert = 0;
        //                else if (randomNumber != 1)
        //                    for (var j = 0; j < dices.Count; j++)
        //                    {
        //                        if (values[j] < randomNumber)
        //                        {
        //                            toInsert = j;
        //                            break;
        //                        }
        //                    }
        //            }
        //            else
        //            {
        //                toInsert = dices.Count;
        //            }
        //            dices.Insert(toInsert, GetDice(randomNumber));
        //            values.Insert(toInsert, randomNumber);
        //        }

        //        var bitmap = dices.Merge();
        //        await channel.SendMessageAsync(values.Count + " Dice rolled. Total: **" + values.Sum() + "** Average: **" + (values.Sum() / (1.0f * values.Count)).ToString("N2") + "**").ConfigureAwait(false);
        //        await channel.SendFileAsync("dice.png", bitmap.ToStream(ImageFormat.Png)).ConfigureAwait(false);
        //    }
        //    catch
        //    {
        //        await channel.SendMessageAsync("Please enter a number of dice to roll.").ConfigureAwait(false);
        //    }
        //}

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
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
                    rolled = new Random().Next(arr[0], arr[1] + 1);
                }
                else
                {
                    rolled = new Random().Next(0, int.Parse(range) + 1);
                }

                await channel.SendMessageAsync($"{umsg.Author.Mention} rolled **{rolled}**.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendMessageAsync($":anger: {ex.Message}").ConfigureAwait(false);
            }
        }


        ////todo drawing
        //private Image GetDice(int num) => num != 10
        //                                  ? Properties.Resources.ResourceManager.GetObject("_" + num) as Image
        //                                  : new[]
        //                                    {
        //                                      (Properties.Resources.ResourceManager.GetObject("_" + 1) as Image),
        //                                      (Properties.Resources.ResourceManager.GetObject("_" + 0) as Image),
        //                                    }.Merge();
    }
}