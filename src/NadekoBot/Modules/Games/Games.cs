using Discord.Commands;
using Discord;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using NadekoBot.Extensions;
using System.Net.Http;
using ImageSharp;
using NadekoBot.DataStructures;
using NLog;

namespace NadekoBot.Modules.Games
{
    [NadekoModule("Games", ">")]
    public partial class Games : NadekoTopLevelModule
    {
        private static readonly ImmutableArray<string> _8BallResponses = NadekoBot.BotConfig.EightBallResponses.Select(ebr => ebr.Text).ToImmutableArray();

        private static readonly Timer _t = new Timer((_) =>
        {
            _girlRatings.Clear();

        }, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Choose([Remainder] string list = null)
        {
            if (string.IsNullOrWhiteSpace(list))
                return;
            var listArr = list.Split(';');
            if (listArr.Length < 2)
                return;
            var rng = new NadekoRandom();
            await Context.Channel.SendConfirmAsync("🤔", listArr[rng.Next(0, listArr.Length)]).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task _8Ball([Remainder] string question = null)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(NadekoBot.OkColor)
                               .AddField(efb => efb.WithName("❓ " + GetText("question") ).WithValue(question).WithIsInline(false))
                               .AddField(efb => efb.WithName("🎱 " + GetText("8ball")).WithValue(_8BallResponses[new NadekoRandom().Next(0, _8BallResponses.Length)]).WithIsInline(false)));
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Rps(string input)
        {
            Func<int,string> getRpsPick = (p) =>
            {
                switch (p)
                {
                    case 0:
                        return "🚀";
                    case 1:
                        return "📎";
                    default:
                        return "✂️";
                }
            };

            int pick;
            switch (input)
            {
                case "r":
                case "rock":
                case "rocket":
                    pick = 0;
                    break;
                case "p":
                case "paper":
                case "paperclip":
                    pick = 1;
                    break;
                case "scissors":
                case "s":
                    pick = 2;
                    break;
                default:
                    return;
            }
            var nadekoPick = new NadekoRandom().Next(0, 3);
            string msg;
            if (pick == nadekoPick)
                msg = GetText("rps_draw", getRpsPick(pick));
            else if ((pick == 0 && nadekoPick == 1) ||
                     (pick == 1 && nadekoPick == 2) ||
                     (pick == 2 && nadekoPick == 0))
                msg = GetText("rps_win", NadekoBot.Client.CurrentUser.Mention,
                    getRpsPick(nadekoPick), getRpsPick(pick));
            else
                msg = GetText("rps_win", Context.User.Mention, getRpsPick(pick),
                    getRpsPick(nadekoPick));

            await Context.Channel.SendConfirmAsync(msg).ConfigureAwait(false);
        }

        private static readonly ConcurrentDictionary<ulong, GirlRating> _girlRatings = new ConcurrentDictionary<ulong, GirlRating>();

        public class GirlRating
        {
            private static readonly Logger _log = LogManager.GetCurrentClassLogger();

            public double Crazy { get; }
            public double Hot { get; }
            public int Roll { get; }
            public string Advice { get; }
            public AsyncLazy<string> Url { get; }

            public GirlRating(double crazy, double hot, int roll, string advice)
            {
                Crazy = crazy;
                Hot = hot;
                Roll = roll;
                Advice = advice; // convenient to have it here, even though atm there are only few different ones.

                Url = new AsyncLazy<string>(async () =>
                {
                    try
                    {
                        using (var ms = new MemoryStream(NadekoBot.Images.WifeMatrix.ToArray(), false))
                        using (var img = new ImageSharp.Image(ms))
                        {
                            const int minx = 35;
                            const int miny = 385;
                            const int length = 345;

                            var pointx = (int)(minx + length * (Hot / 10));
                            var pointy = (int)(miny - length * ((Crazy - 4) / 6));
                            
                            using (var pointMs = new MemoryStream(NadekoBot.Images.RategirlDot.ToArray(), false))
                            using (var pointImg = new ImageSharp.Image(pointMs))
                            {
                                img.DrawImage(pointImg, 100, default(Size), new Point(pointx - 10, pointy - 10));
                            }

                            string url;
                            using (var http = new HttpClient())
                            using (var imgStream = new MemoryStream())
                            {
                                img.Save(imgStream);
                                var byteContent = new ByteArrayContent(imgStream.ToArray());
                                http.AddFakeHeaders();

                                var reponse = await http.PutAsync("https://transfer.sh/img.png", byteContent);
                                url = await reponse.Content.ReadAsStringAsync();
                            }
                            return url;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                        return null;
                    }
                });
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RateGirl(IGuildUser usr)
        {
            var gr = _girlRatings.GetOrAdd(usr.Id, GetGirl);
            var img = await gr.Url;
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle("Girl Rating For " + usr)
                .AddField(efb => efb.WithName("Hot").WithValue(gr.Hot.ToString("F2")).WithIsInline(true))
                .AddField(efb => efb.WithName("Crazy").WithValue(gr.Crazy.ToString("F2")).WithIsInline(true))
                .AddField(efb => efb.WithName("Advice").WithValue(gr.Advice).WithIsInline(false))
                .WithImageUrl(img)).ConfigureAwait(false);
        }

        private double NextDouble(double x, double y)
        {
            var rng = new Random();
            return rng.NextDouble() * (y - x) + x;
        }

        private GirlRating GetGirl(ulong uid)
        {
            var rng = new NadekoRandom();

            var roll = rng.Next(1, 1001);

            if ((uid == 185968432783687681 ||
                 uid == 265642040950390784) && roll >= 900)
                roll = 1000;


            double hot;
            double crazy;
            string advice;
            if (roll < 500)
            {
                hot = NextDouble(0, 5);
                crazy = NextDouble(4, 10);
                advice = 
                    "This is your NO-GO ZONE. We do not hang around, and date, and marry women who are atleast, in our mind, a 5. " +
                    "So, this is your no-go zone. You don't go here. You just rule this out. Life is better this way, that's the way it is.";
            }
            else if (roll < 750)
            {
                hot = NextDouble(5, 8);
                crazy = NextDouble(4, .6 * hot + 4);
                advice = "Above a 5, and to about an 8, and below the crazy line - this is your FUN ZONE. You can " +
                       "hang around here, and meet these girls and spend time with them. Keep in mind, while you're " +
                       "in the fun zone, you want to move OUT of the fun zone to a more permanent location. " +
                       "These girls are most of the time not crazy.";
            }
            else if (roll < 900)
            {
                hot = NextDouble(5, 10);
                crazy = NextDouble(.61 * hot + 4, 10);
                advice = "Above the crazy line - it's the DANGER ZONE. This is redheads, strippers, anyone named Tiffany, " +
                       "hairdressers... This is where your car gets keyed, you get bunny in the pot, your tires get slashed, " +
                       "and you wind up in jail.";
            }
            else if (roll < 951)
            {
                hot = NextDouble(8, 10);
                crazy = NextDouble(7, .6 * hot + 4);
                advice = "Below the crazy line, above an 8 hot, but still about 7 crazy. This is your DATE ZONE. " +
                       "You can stay in the date zone indefinitely. These are the girls you introduce to your friends and your family. " +
                       "They're good looking, and they're reasonably not crazy most of the time. You can stay here indefinitely.";
            }
            else if (roll < 990)
            {
                hot = NextDouble(8, 10);
                crazy = NextDouble(5, 7);
                advice = "Above an 8 hot, and between about 7 and a 5 crazy - this is WIFE ZONE. If you meet this girl, you should consider long-term " +
                       "relationship. Rare.";
            }
            else if (roll < 999)
            {
                hot = NextDouble(8, 10);
                crazy = NextDouble(2, 3.99d);
                advice = "You've met a girl she's above 8 hot, and not crazy at all (below 4)... totally cool?" +
                         " You should be careful. That's a dude. You're talking to a tranny!";
            }
            else
            {
                hot = NextDouble(8, 10);
                crazy = NextDouble(4, 5);
                advice = "Below 5 crazy, and above 8 hot, this is the UNICORN ZONE, these things don't exist." +
                         "If you find a unicorn, please capture it safely, keep it alive, we'd like to study it, " +
                         "and maybe look at how to replicate that.";
            }

            return new GirlRating(crazy, hot, roll, advice);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Linux(string guhnoo, string loonix)
        {
            await Context.Channel.SendConfirmAsync(
$@"I'd just like to interject for moment. What you're refering to as {loonix}, is in fact, {guhnoo}/{loonix}, or as I've recently taken to calling it, {guhnoo} plus {loonix}. {loonix} is not an operating system unto itself, but rather another free component of a fully functioning {guhnoo} system made useful by the {guhnoo} corelibs, shell utilities and vital system components comprising a full OS as defined by POSIX.

Many computer users run a modified version of the {guhnoo} system every day, without realizing it. Through a peculiar turn of events, the version of {guhnoo} which is widely used today is often called {loonix}, and many of its users are not aware that it is basically the {guhnoo} system, developed by the {guhnoo} Project.

There really is a {loonix}, and these people are using it, but it is just a part of the system they use. {loonix} is the kernel: the program in the system that allocates the machine's resources to the other programs that you run. The kernel is an essential part of an operating system, but useless by itself; it can only function in the context of a complete operating system. {loonix} is normally used in combination with the {guhnoo} operating system: the whole system is basically {guhnoo} with {loonix} added, or {guhnoo}/{loonix}. All the so-called {loonix} distributions are really distributions of {guhnoo}/{loonix}."
            ).ConfigureAwait(false);
        }
    }
}
