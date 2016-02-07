using System;
using System.Linq;
using Discord.Modules;
using NadekoBot.Extensions;
using NadekoBot.Commands;
//🃏
//🏁
namespace NadekoBot.Modules
{
    class Games : DiscordModule
    {
        public Games() : base() {
            commands.Add(new Trivia());
            commands.Add(new SpeedTyping());
            commands.Add(new PollCommand());
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(">choose")
                  .Description("Chooses a thing from a list of things\n**Usage**: >choose Get up;Sleep more;Sleep even more")
                  .Parameter("list", Discord.Commands.ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("list");
                      if (string.IsNullOrWhiteSpace(arg))
                          return;
                      var list = arg.Split(';');
                      if (list.Count() < 2)
                          return;
                      await e.Send(list[new Random().Next(0, list.Length)]);
                  });

                cgb.CreateCommand(">")
                    .Description("Attack a person. Supported attacks: 'splash', 'strike', 'burn', 'surge'.\n**Usage**: > strike @User")
                    .Parameter("attack_type",Discord.Commands.ParameterType.Required)
                    .Parameter("target",Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        var usr = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                        var usrType = GetType(usr.Id);
                        string response = "";
                        int dmg = GetDamage(usrType, e.GetArg("attack_type").ToLowerInvariant());
                        response = e.GetArg("attack_type") + (e.GetArg("attack_type") == "splash" ? "es " : "s ") + $"{usr.Mention}{GetImage(usrType)} for {dmg}\n";
                        if (dmg >= 65)
                        {
                            response += "It's super effective!";
                        }
                        else if (dmg <= 35) {
                            response += "Ineffective!";
                        }
                        await e.Send($"{ e.User.Mention }{GetImage(GetType(e.User.Id))} {response}");
                    });

                cgb.CreateCommand("poketype")
                    .Parameter("target", Discord.Commands.ParameterType.Required)
                    .Description("Gets the users element type. Use this to do more damage with strike")
                    .Do(async e =>
                    {
                        var usr = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                        if (usr == null) {
                            await e.Send("No such person.");
                        }
                        var t = GetType(usr.Id);
                        await e.Send($"{usr.Name}'s type is {GetImage(t)} {t}");
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
        string GetImage(PokeType t) {
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

        private int GetDamage(PokeType targetType, string v)
        {
            var rng = new Random();
            switch (v)
            {
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
            else if (remainder >= 3 && remainder < 5)
            {
                return PokeType.GRASS;
            }
            else if (remainder >= 5 && remainder < 8)
            {
                return PokeType.FIRE;
            }
            else {
                return PokeType.ELECTRICAL;
            }
        }

        private enum PokeType
        {
            WATER, GRASS, FIRE, ELECTRICAL
        }
    }
}
