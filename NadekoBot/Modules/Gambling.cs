using Discord.Commands;
using Discord.Modules;
using NadekoBot.Extensions;
using System.Linq;
using Discord;
using NadekoBot.Commands;

namespace NadekoBot.Modules
{
    internal class Gambling : DiscordModule
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
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

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
                      var membersArray = members as User[] ?? members.ToArray();
                      var usr = membersArray[new System.Random().Next(0, membersArray.Length)];
                      await e.Channel.SendMessage($"**Raffled user:** {usr.Name} (id: {usr.Id})");
                  });
                cgb.CreateCommand("$$$")
                  .Description("Check how many NadekoFlowers you have.")
                  .Do(async e => {
                      var pts = Classes.DbHandler.Instance.GetStateByUserId((long)e.User.Id)?.Value ?? 0;
                      var str = $"`You have {pts} NadekoFlowers".SnPl((int)pts)+"`\n";
                      for (var i = 0; i < pts; i++) {
                          str += "🌸";
                      }
                      await e.Channel.SendMessage(str);
                  });
            });
        }
    }
}
