using Discord.Commands;
using Discord;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System;
using System.Linq;
using System.Collections.Generic;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Games
{
    [Module(">", AppendSpace = false)]
    public partial class GamesModule : DiscordModule
    {
        //todo DB
        private IEnumerable<string> _8BallResponses;
        public GamesModule(ILocalization loc, CommandService cmds, IBotConfiguration config, IDiscordClient client) : base(loc, cmds, config, client)
        {
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Choose(IMessage imsg, [Remainder] string list = null)
        {
            var channel = imsg.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(list))
                return;
            var listArr = list.Split(';');
            if (listArr.Count() < 2)
                return;
            var rng = new Random();
            await imsg.Channel.SendMessageAsync(listArr[rng.Next(0, listArr.Length)]).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task _8Ball(IMessage imsg, [Remainder] string question = null)
        {
            var channel = imsg.Channel as ITextChannel;

            if (string.IsNullOrWhiteSpace(question))
                return;
                var rng = new Random();
            await imsg.Channel.SendMessageAsync($@":question: `Question` __**{question}**__ 
🎱 `8Ball Answers` __**{_8BallResponses.Shuffle().FirstOrDefault()}**__").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Rps(IMessage imsg, string input)
        {
            var channel = imsg.Channel as ITextChannel;

            Func<int,string> GetRPSPick = (p) =>
            {
                if (p == 0)
                    return "rocket";
                else if (p == 1)
                    return "paperclip";
                else
                    return "scissors";
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
            var nadekoPick = new Random().Next(0, 3);
            var msg = "";
            if (pick == nadekoPick)
                msg = $"It's a draw! Both picked :{GetRPSPick(pick)}:";
            else if ((pick == 0 && nadekoPick == 1) ||
                     (pick == 1 && nadekoPick == 2) ||
                     (pick == 2 && nadekoPick == 0))
                msg = $"{(await NadekoBot.Client.GetCurrentUserAsync()).Mention} won! :{GetRPSPick(nadekoPick)}: beats :{GetRPSPick(pick)}:";
            else
                msg = $"{imsg.Author.Mention} won! :{GetRPSPick(pick)}: beats :{GetRPSPick(nadekoPick)}:";

            await imsg.Channel.SendMessageAsync(msg).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Linux(IMessage imsg, string guhnoo, string loonix)
        {
            var channel = imsg.Channel as ITextChannel;

            await imsg.Channel.SendMessageAsync(
$@"I'd just like to interject for moment. What you're refering to as {loonix}, is in fact, {guhnoo}/{loonix}, or as I've recently taken to calling it, {guhnoo} plus {loonix}. {loonix} is not an operating system unto itself, but rather another free component of a fully functioning {guhnoo} system made useful by the {guhnoo} corelibs, shell utilities and vital system components comprising a full OS as defined by POSIX.

Many computer users run a modified version of the {guhnoo} system every day, without realizing it. Through a peculiar turn of events, the version of {guhnoo} which is widely used today is often called {loonix}, and many of its users are not aware that it is basically the {guhnoo} system, developed by the {guhnoo} Project.

There really is a {loonix}, and these people are using it, but it is just a part of the system they use. {loonix} is the kernel: the program in the system that allocates the machine's resources to the other programs that you run. The kernel is an essential part of an operating system, but useless by itself; it can only function in the context of a complete operating system. {loonix} is normally used in combination with the {guhnoo} operating system: the whole system is basically {guhnoo} with {loonix} added, or {guhnoo}/{loonix}. All the so-called {loonix} distributions are really distributions of {guhnoo}/{loonix}."
            ).ConfigureAwait(false);
        }
    }
}
