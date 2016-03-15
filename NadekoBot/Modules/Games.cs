using System;
using System.Linq;
using Discord.Modules;
using NadekoBot.Commands;
using Newtonsoft.Json.Linq;
using System.IO;
using Discord.Commands;
using NadekoBot.Extensions;

namespace NadekoBot.Modules {
    internal class Games : DiscordModule {
        private readonly string[] _8BallAnswers;
        private readonly Random rng = new Random();

        public Games() {
            commands.Add(new Trivia(this));
            commands.Add(new SpeedTyping(this));
            commands.Add(new PollCommand(this));
            //commands.Add(new BetrayGame(this));
            _8BallAnswers = JArray.Parse(File.ReadAllText("data/8ball.json")).Select(t => t.ToString()).ToArray();
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Games;

        public override void Install(ModuleManager manager) {
            manager.CreateCommands("", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "choose")
                  .Description("Chooses a thing from a list of things\n**Usage**: >choose Get up;Sleep;Sleep more")
                  .Parameter("list", Discord.Commands.ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("list");
                      if (string.IsNullOrWhiteSpace(arg))
                          return;
                      var list = arg.Split(';');
                      if (list.Count() < 2)
                          return;
                      await e.Channel.SendMessage(list[rng.Next(0, list.Length)]);
                  });

                cgb.CreateCommand(Prefix + "8ball")
                    .Description("Ask the 8ball a yes/no question.")
                    .Parameter("question", Discord.Commands.ParameterType.Unparsed)
                    .Do(async e => {
                        var question = e.GetArg("question");
                        if (string.IsNullOrWhiteSpace(question))
                            return;
                        try {
                            await e.Channel.SendMessage(
                                $":question: **Question**: `{question}` \n🎱 **8Ball Answers**: `{_8BallAnswers[rng.Next(0, _8BallAnswers.Length)]}`");
                        } catch { }
                    });

                cgb.CreateCommand(Prefix + "attack")
                    .Description("Attack a person. Supported attacks: 'splash', 'strike', 'burn', 'surge'.\n**Usage**: > strike @User")
                    .Parameter("attack_type", Discord.Commands.ParameterType.Required)
                    .Parameter("target", Discord.Commands.ParameterType.Required)
                    .Do(async e => {
                        var usr = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                        if (usr == null) {
                            await e.Channel.SendMessage("No such person.");
                            return;
                        }
                        var usrType = GetType(usr.Id);
                        var response = "";
                        var dmg = GetDamage(usrType, e.GetArg("attack_type").ToLowerInvariant());
                        response = e.GetArg("attack_type") + (e.GetArg("attack_type") == "splash" ? "es " : "s ") + $"{usr.Mention}{GetImage(usrType)} for {dmg}\n";
                        if (dmg >= 65) {
                            response += "It's super effective!";
                        } else if (dmg <= 35) {
                            response += "Ineffective!";
                        }
                        await e.Channel.SendMessage($"{ e.User.Mention }{GetImage(GetType(e.User.Id))} {response}");
                    });

                cgb.CreateCommand(Prefix + "poketype")
                    .Parameter("target", Discord.Commands.ParameterType.Required)
                    .Description("Gets the users element type. Use this to do more damage with strike!")
                    .Do(async e => {
                        var usr = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                        if (usr == null) {
                            await e.Channel.SendMessage("No such person.");
                            return;
                        }
                        var t = GetType(usr.Id);
                        await e.Channel.SendMessage($"{usr.Name}'s type is {GetImage(t)} {t}");
                    });
                cgb.CreateCommand(Prefix + "rps")
                    .Description("Play a game of rocket paperclip scissors with nadkeo.\n**Usage**: >rps scissors")
                    .Parameter("input", ParameterType.Required)
                    .Do(async e => {
                        var input = e.GetArg("input").Trim();
                        int pick;
                        switch (input) {
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

                        await e.Channel.SendMessage(msg);
                    });

                cgb.CreateCommand(Prefix + "linux")
                    .Description("Prints a customizable Linux interjection")
                    .Parameter("gnu", ParameterType.Required)
                    .Parameter("linux", ParameterType.Required)
                    .Do(async e => {
                        var guhnoo = e.Args[0];
                        var loonix = e.Args[1];

                        await e.Channel.SendMessage(
$@"
I'd just like to interject for moment. What you're refering to as {loonix}, is in fact, {guhnoo}/{loonix}, or as I've recently taken to calling it, {guhnoo} plus {loonix}. {loonix} is not an operating system unto itself, but rather another free component of a fully functioning {guhnoo} system made useful by the {guhnoo} corelibs, shell utilities and vital system components comprising a full OS as defined by POSIX.

Many computer users run a modified version of the {guhnoo} system every day, without realizing it. Through a peculiar turn of events, the version of {guhnoo} which is widely used today is often called {loonix}, and many of its users are not aware that it is basically the {guhnoo} system, developed by the {guhnoo} Project.

There really is a {loonix}, and these people are using it, but it is just a part of the system they use. {loonix} is the kernel: the program in the system that allocates the machine's resources to the other programs that you run. The kernel is an essential part of an operating system, but useless by itself; it can only function in the context of a complete operating system. {loonix} is normally used in combination with the {guhnoo} operating system: the whole system is basically {guhnoo} with {loonix} added, or {guhnoo}/{loonix}. All the so-called {loonix} distributions are really distributions of {guhnoo}/{loonix}.
");
                    });
            });
        }
        /*

            🌿 or 🍃 or 🌱 Grass
⚡ Electric
❄ Ice
☁ Fly
🔥 Fire
💧 or 💦 Water
⭕ Normal
🐛 Insect
🌟 or 💫 or ✨ Fairy
    */

        private string GetImage(PokeType t) {
            switch (t) {
                case PokeType.WATER:
                    return "💦";
                case PokeType.GRASS:
                    return "🌿";
                case PokeType.FIRE:
                    return "🔥";
                case PokeType.ELECTRICAL:
                    return "⚡️";
                default:
                    return "⭕️";
            }
        }

        private int GetDamage(PokeType targetType, string v) {
            var rng = new Random();
            switch (v) {
                case "splash": //water
                    if (targetType == PokeType.FIRE)
                        return rng.Next(65, 100);
                    else if (targetType == PokeType.ELECTRICAL)
                        return rng.Next(0, 35);
                    else
                        return rng.Next(40, 60);
                case "strike": //grass
                    if (targetType == PokeType.ELECTRICAL)
                        return rng.Next(65, 100);
                    else if (targetType == PokeType.FIRE)
                        return rng.Next(0, 35);
                    else
                        return rng.Next(40, 60);
                case "burn": //fire
                case "flame":
                    if (targetType == PokeType.GRASS)
                        return rng.Next(65, 100);
                    else if (targetType == PokeType.WATER)
                        return rng.Next(0, 35);
                    else
                        return rng.Next(40, 60);
                case "surge": //electrical
                case "electrocute":
                    if (targetType == PokeType.WATER)
                        return rng.Next(65, 100);
                    else if (targetType == PokeType.GRASS)
                        return rng.Next(0, 35);
                    else
                        return rng.Next(40, 60);
                default:
                    return 0;
            }
        }

        private PokeType GetType(ulong id) {
            if (id == 113760353979990024)
                return PokeType.FIRE;

            var remainder = id % 10;
            if (remainder < 3)
                return PokeType.WATER;
            else if (remainder >= 3 && remainder < 5) {
                return PokeType.GRASS;
            } else if (remainder >= 5 && remainder < 8) {
                return PokeType.FIRE;
            } else {
                return PokeType.ELECTRICAL;
            }
        }

        private enum PokeType {
            WATER, GRASS, FIRE, ELECTRICAL
        }

        private string GetRPSPick(int i) {
            if (i == 0)
                return "rocket";
            else if (i == 1)
                return "paperclip";
            else
                return "scissors";
        }
    }
}
