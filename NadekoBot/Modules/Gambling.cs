using Discord.Commands;
using Discord.Modules;
using System.Linq;

namespace NadekoBot.Modules
{
    class Gambling : DiscordModule
    {

        public Gambling() {
            commands.Add(new DrawCommand());
            commands.Add(new FlipCoinCommand());
            commands.Add(new DiceRollCommand());
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                commands.ForEach(com => com.Init(cgb));

                cgb.CreateCommand("$raffle")
                  .Description("Prints a name and ID of a random user from the online list from the (optional) role.")
                  .Parameter("role", ParameterType.Optional)
                  .Do(async e => {
                      var arg = string.IsNullOrWhiteSpace(e.GetArg("role")) ? "@everyone" : e.GetArg("role");
                      var role = e.Server.FindRoles(arg).FirstOrDefault();
                      if (role == null) {
                          await e.Channel.SendMessage("💢 Role not found.");
                          return;
                      }
                      var members = role.Members.Where(u => u.Status == Discord.UserStatus.Online); // only online
                      await e.Channel.SendMessage($"**Raffled user:** {members.ToArray()[new System.Random().Next(0, members.Count())].Name}");
                  });
            });
        }
    }
}
