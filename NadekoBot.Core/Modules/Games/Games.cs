using Discord.Commands;
using Discord;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using System;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games
{
    /* more games
    - Shiritori
    - Simple RPG adventure
    */
    public partial class Games : NadekoTopLevelModule<GamesService>
    {
        private readonly IImageCache _images;

        public Games(IDataCache data)
        {
            _images = data.LocalImages;
        }
//#if GLOBAL_NADEKO
//        [NadekoCommand, Usage, Description, Aliases]
//        [RequireContext(ContextType.Guild)]
//        public async Task TrickOrTreat()
//        {
//            if (DateTime.UtcNow.Day != 31 ||
//                DateTime.UtcNow.Month != 10
//                || !_service.HalloweenAwardedUsers.Add(Context.User.Id)
//        )
//            {
//                return;
//            }
//            if (await _service.GetTreat(Context.User.Id))
//            {
//                await Context.Channel
//                    .SendConfirmAsync($"You've got a treat of 10🍬! Happy Halloween!")
//                    .ConfigureAwait(false);
//            }
//            else
//            {
//                await Context.Channel
//                    .EmbedAsync(new EmbedBuilder()
//                    .WithDescription("No treat for you :c Happy Halloween!")
//                    .WithImageUrl("http://tinyurl.com/ybntddbb")
//                    .WithErrorColor())
//                    .ConfigureAwait(false);
//            }
//        }
//#endif
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
        public async Task EightBall([Remainder] string question = null)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(NadekoBot.OkColor)
                               .AddField(efb => efb.WithName("❓ " + GetText("question") ).WithValue(question).WithIsInline(false))
                               .AddField(efb => efb.WithName("🎱 " + GetText("8ball")).WithValue(_service.EightBallResponses[new NadekoRandom().Next(0, _service.EightBallResponses.Length)]).WithIsInline(false)));
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RateGirl(IGuildUser usr)
        {
            var gr = _service.GirlRatings.GetOrAdd(usr.Id, GetGirl);
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
                    "This is your NO-GO ZONE. We do not hang around, and date, and marry women who are at least, in our mind, a 5. " +
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

            return new GirlRating(_images, crazy, hot, roll, advice);
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
