using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot;

namespace NadekoBot.Commands {
    class LoLCommands : DiscordCommand {
        public override Func<CommandEventArgs, Task> DoFunc() {
            throw new NotImplementedException();
        }

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("~lolchamp")
                  .Description("Checks champion statistic for a lol champion.")
                  .Parameter("region", ParameterType.Unparsed)
                  .Do(async e => {
                      if (string.IsNullOrWhiteSpace(e.GetArg("region")))
                          return;
                      try {
                          var arg = e.GetArg("region");
                          var data = Newtonsoft.Json.Linq.JArray.Parse(await Classes.SearchHelper.GetResponseAsync($"http://api.champion.gg/stats/champs/{e.GetArg("region")}?api_key={NadekoBot.creds.LOLAPIKey}"))[0];
                          var general = data["general"];
                          await e.Channel.SendMessage(
$@"**`Champion Name:` {data["title"]}
`Kills:` {general["kills"]} `Deaths:` {general["deaths"]}** `K/D:`**{float.Parse(general["kills"].ToString()) / float.Parse(general["deaths"].ToString()):f2}**
`Minions:` **{general["minionsKilled"]}**
`Win percentage:` **{general["winPercent"]}%**
");
                      }
                      catch (Exception) {
                          await e.Channel.SendMessage("💢 Failed retreiving data for that champion.");
                          return;
                      }
                  });
        }
    }
}
