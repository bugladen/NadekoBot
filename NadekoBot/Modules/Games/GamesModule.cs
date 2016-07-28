using Discord.Commands;
using Discord.Modules;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Commands;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Linq;

namespace NadekoBot.Modules.Games
{
    internal class GamesModule : DiscordModule
    {
        private readonly Random rng = new Random();

        public GamesModule()
        {
            commands.Add(new TriviaCommands(this));
            commands.Add(new SpeedTyping(this));
            commands.Add(new PollCommand(this));
            commands.Add(new PlantPick(this));
            commands.Add(new Bomberman(this));
            commands.Add(new Leet(this));
            //commands.Add(new BetrayGame(this));

        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Games;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "choose")
                  .Description($"Chooses a thing from a list of things | `{Prefix}choose Get up;Sleep;Sleep more`")
                  .Parameter("list", ParameterType.Unparsed)
                  .Do(async e =>
                  {
                      var arg = e.GetArg("list");
                      if (string.IsNullOrWhiteSpace(arg))
                          return;
                      var list = arg.Split(';');
                      if (list.Count() < 2)
                          return;
                      await e.Channel.SendMessage(list[rng.Next(0, list.Length)]).ConfigureAwait(false);
                  });

                cgb.CreateCommand(Prefix + "8ball")
                    .Description($"Ask the 8ball a yes/no question. | `{Prefix}8ball should i do something`")
                    .Parameter("question", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var question = e.GetArg("question");
                        if (string.IsNullOrWhiteSpace(question))
                            return;
                        try
                        {
                            await e.Channel.SendMessage(
                                $":question: `Question` __**{question}**__ \n🎱 `8Ball Answers` __**{NadekoBot.Config._8BallResponses[rng.Next(0, NadekoBot.Config._8BallResponses.Length)]}**__")
                                    .ConfigureAwait(false);
                        }
                        catch { }
                    });

                cgb.CreateCommand(Prefix + "rps")
                    .Description($"Play a game of rocket paperclip scissors with Nadeko. | `{Prefix}rps scissors`")
                    .Parameter("input", ParameterType.Required)
                    .Do(async e =>
                    {
                        var input = e.GetArg("input").Trim();
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
                            msg = $"{NadekoBot.BotMention} won! :{GetRPSPick(nadekoPick)}: beats :{GetRPSPick(pick)}:";
                        else
                            msg = $"{e.User.Mention} won! :{GetRPSPick(pick)}: beats :{GetRPSPick(nadekoPick)}:";

                        await e.Channel.SendMessage(msg).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "linux")
                    .Description($"Prints a customizable Linux interjection | `{Prefix}linux Spyware Windows`")
                    .Parameter("gnu", ParameterType.Required)
                    .Parameter("linux", ParameterType.Required)
                    .Do(async e =>
                    {
                        var guhnoo = e.Args[0];
                        var loonix = e.Args[1];

                        await e.Channel.SendMessage(
$@"
I'd just like to interject for moment. What you're refering to as {loonix}, is in fact, {guhnoo}/{loonix}, or as I've recently taken to calling it, {guhnoo} plus {loonix}. {loonix} is not an operating system unto itself, but rather another free component of a fully functioning {guhnoo} system made useful by the {guhnoo} corelibs, shell utilities and vital system components comprising a full OS as defined by POSIX.

Many computer users run a modified version of the {guhnoo} system every day, without realizing it. Through a peculiar turn of events, the version of {guhnoo} which is widely used today is often called {loonix}, and many of its users are not aware that it is basically the {guhnoo} system, developed by the {guhnoo} Project.

There really is a {loonix}, and these people are using it, but it is just a part of the system they use. {loonix} is the kernel: the program in the system that allocates the machine's resources to the other programs that you run. The kernel is an essential part of an operating system, but useless by itself; it can only function in the context of a complete operating system. {loonix} is normally used in combination with the {guhnoo} operating system: the whole system is basically {guhnoo} with {loonix} added, or {guhnoo}/{loonix}. All the so-called {loonix} distributions are really distributions of {guhnoo}/{loonix}.
").ConfigureAwait(false);
                    });
            });
        }

        private string GetRPSPick(int i)
        {
            if (i == 0)
                return "rocket";
            else if (i == 1)
                return "paperclip";
            else
                return "scissors";
        }
    }
}
