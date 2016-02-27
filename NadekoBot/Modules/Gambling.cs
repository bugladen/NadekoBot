using Discord.Commands;
using Discord.Modules;
using NadekoBot.Extensions;
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
                      try {
                          var usr = members.ToArray()[new System.Random().Next(0, members.Count())];
                          await e.Channel.SendMessage($"**Raffled user:** {usr.Name} (id: {usr.Id})");
                      }
                      catch { }
                  });
                /*
                cgb.CreateCommand("$$")
                  .Description("Add moneyz")
                  .Parameter("val", ParameterType.Required)
                  .Do(e => {
                      var arg = e.GetArg("val");
                      var num = int.Parse(arg);
                      Classes.DBHandler.Instance.InsertData(
                          new Classes._DataModels.CurrencyTransaction {
                              Value = num,
                              Reason = "Money plz",
                              UserId = (long)e.User.Id,
                          });
                  });
                  */
                cgb.CreateCommand("$$$")
                  .Description("Check how many NadekoFlowers you have.")
                  .Do(async e => {
                      var pts = Classes.DBHandler.Instance.GetStateByUserId((long)e.User.Id)?.Value ?? 0;
                      string str = $"`You have {pts} NadekoFlowers".SnPl((int)pts)+"`\n";
                      for (int i = 0; i < pts; i++) {
                          str += "🌸";
                      }
                      await e.Channel.SendMessage(str);
                  });
            });
        }
    }
}
