using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot;
using System.Drawing;
using NadekoBot.Extensions;
using Newtonsoft.Json.Linq;
namespace NadekoBot.Commands {
    class LoLCommands : DiscordCommand {
        public override Func<CommandEventArgs, Task> DoFunc() {
            throw new NotImplementedException();
        }

        class MatchupModel {
            public int Games { get; set; }
            public float WinRate { get; set; }
            [Newtonsoft.Json.JsonProperty("key")]
            public string Name { get; set; }
            public float StatScore { get; set; }
        }

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("~lolchamp")
                  .Description("Checks champion statistic for a lol champion.")
                  .Parameter("champ", ParameterType.Required)
                  .Parameter("position", ParameterType.Unparsed)
                  .Do(async e => {
                      try {
                          //get role
                          string role = ResolvePos(e.GetArg("position"));
                          var name = e.GetArg("champ").Replace(" ","");

                          //get basic champion stats
                          var allData = JArray.Parse(await Classes.SearchHelper.GetResponseAsync($"http://api.champion.gg/stats/champs/{name}?api_key={NadekoBot.creds.LOLAPIKey}"));
                          JToken data = null;
                          if (role != null) {
                              for (int i = 0; i < allData.Count; i++) {
                                  if (allData[i]["role"].ToString().Equals(role)) {
                                      data = allData[i];
                                      break;
                                  }
                              }
                              if (data == null) {
                                  await e.Channel.SendMessage("💢 Data for that role does not exist.");
                                  return;
                              }
                          }
                          else {
                              data = allData[0];
                              role = allData[0]["role"].ToString();
                          }
                          //name = data["title"].ToString();
                          // get all possible roles, and "select" the shown one
                          var roles = new string[allData.Count];
                          for (int i = 0; i < allData.Count; i++) {
                              roles[i] = allData[i]["role"].ToString();
                              if (roles[i] == role)
                                  roles[i] = ">" + roles[i] + "<";
                          }
                          var general = data["general"];

                          //get build data for this role
                          var buildDatas = JArray.Parse(await Classes.SearchHelper.GetResponseAsync($"http://api.champion.gg/champion/{name}/items/finished/mostPopular?api_key={NadekoBot.creds.LOLAPIKey}"));
                          JToken buildData = null;
                          for (int i = 0; i < buildDatas.Count; i++) {
                              if (buildDatas[i]["role"].ToString() == role) {
                                  buildData = buildDatas[i];
                              }
                          }
                          if (buildData == null)
                              return; // wtf would cause this tho?
                          var items = new string[6];
                          for (int i = 0; i < 6; i++) {
                              items[i] = buildData["items"][i].ToString();
                          }

                          //get matchup data to show counters and countered champions
                          var matchupDatas = JArray.Parse(await Classes.SearchHelper.GetResponseAsync($"http://api.champion.gg/champion/{name}/matchup?api_key={NadekoBot.creds.LOLAPIKey}"));
                          List<MatchupModel> matchupDataIE = null;
                          for (int i = 0; i < matchupDatas.Count; i++) {
                              if (matchupDatas[i]["role"].ToString() == role) {
                                  matchupDataIE = matchupDatas[i]["matchups"].ToObject<List<MatchupModel>>();
                              }
                          }
                          if (matchupDataIE == null)
                              return;
                          var matchupData = matchupDataIE.OrderBy(m => m.StatScore).ToArray();
                          //matchupData.OrderBy(mm => mm.WinRate);

                          var countered = new[] { matchupData[0].Name, matchupData[1].Name, matchupData[2].Name };
                          var counters = new[] { matchupData[matchupData.Length - 1].Name, matchupData[matchupData.Length - 2].Name, matchupData[matchupData.Length - 3].Name };

                          var runeDatas = JArray.Parse(await Classes.SearchHelper.GetResponseAsync($"http://api.champion.gg/champion/{name}/runes/mostPopular?api_key={NadekoBot.creds.LOLAPIKey}"));
                          JToken runeData = null;
                          for (int i = 0; i < runeDatas.Count; i++) {
                              if (runeDatas[i]["role"].ToString() == role) {
                                  runeData = runeDatas[i];
                              }
                          }
                          if (runeData == null)
                              return;

                          var runesJArray = runeData["runes"] as JArray;
                          var runes = string.Join("\n", runesJArray.OrderBy(jt => int.Parse(jt["number"].ToString())).Select(jt => jt["number"].ToString() + "x" + jt["name"]));

                          //todo save this for at least 1 hour
                          Image img = Image.FromFile("data/lol/bg.png");
                          using (Graphics g = Graphics.FromImage(img)) {
                              int statsFontSize = 15;
                              int margin = 5;
                              int imageSize = 75;
                              var normalFont = new Font("Times New Roman", 9, FontStyle.Regular);
                              var smallFont = new Font("Times New Roman", 8, FontStyle.Regular);
                              //draw champ image
                              g.DrawImage(Image.FromFile($"data/lol/champions/{name}.png"), new Rectangle(margin, margin, imageSize, imageSize));
                              //draw champ name
                              g.DrawString($"{data["title"]}", new Font("Times New Roman", 25, FontStyle.Regular), Brushes.WhiteSmoke, margin + imageSize + margin, margin);
                              //draw champ surname
                              //todo
                              //draw roles

                              g.DrawString("Roles: " + string.Join(", ", roles), normalFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin);
                              //draw average stats
                              g.DrawString(
$@"    Average Stats

Kills: {general["kills"]}     CS: {general["minionsKilled"]}
Deaths: {general["deaths"]}  Win: {general["winPercent"]}%
Assists: {general["assists"]}  Ban: {general["banRate"]}%
", normalFont, Brushes.WhiteSmoke, img.Width - 150, margin);
                              //draw masteries
                              g.DrawString($"MASTERIES: 18 / 0 / 12", normalFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin + 20);
                              //draw runes
                              g.DrawString($"{runes}", smallFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin + 40);
                              //draw counters
                              g.DrawString($"Best against", smallFont, Brushes.WhiteSmoke, margin, img.Height - imageSize + margin);
                              int smallImgSize = 50;

                              for (int i = 0; i < counters.Length; i++) {
                                  g.DrawImage(Image.FromFile("data/lol/champions/" + counters[i] + ".png"),
                                              new Rectangle(i * (smallImgSize + margin) + margin, img.Height - smallImgSize - margin,
                                              smallImgSize,
                                              smallImgSize));
                              }
                              //draw countered by
                              g.DrawString($"Worst against", smallFont, Brushes.WhiteSmoke, img.Width - 3 * (smallImgSize + margin), img.Height - imageSize + margin);

                              for (int i = 0; i < countered.Length; i++) {
                                  int j = countered.Length - i;
                                  g.DrawImage(Image.FromFile("data/lol/champions/" + countered[i] + ".png"),
                                              new Rectangle(img.Width - (j * (smallImgSize + margin) + margin), img.Height - smallImgSize - margin,
                                              smallImgSize,
                                              smallImgSize));
                              }
                              //draw item build
                              g.DrawString("Popular build", normalFont, Brushes.WhiteSmoke, img.Width - (3 * (smallImgSize + margin) + margin), 77);

                              for (int i = 0; i < 6; i++) {
                                  var inverse_i = 5 - i;
                                  var j = inverse_i % 3 + 1;
                                  var k = inverse_i / 3;
                                  Console.WriteLine(k);
                                  g.DrawImage(Image.FromFile("data/lol/items/" + items[i] + ".png"),
                                              new Rectangle(img.Width - (j * (smallImgSize + margin) + margin), 92 + k * (smallImgSize + margin),
                                              smallImgSize,
                                              smallImgSize));
                              }

                              /*
                              
                                  */
                          }
                          await e.Channel.SendFile(data["title"] + "_stats.png", img.ToStream());
                      }
                      catch (Exception ex) {
                          await e.Channel.SendMessage("💢 Failed retreiving data for that champion.");
                          return;
                      }
                  });
        }

        private string ResolvePos(string pos) {
            if (string.IsNullOrWhiteSpace(pos))
                return null;
            switch (pos.ToLowerInvariant()) {
                case "m":
                case "mid":
                case "midorfeed":
                case "midd":
                case "middle":
                    return "Middle";
                case "top":
                case "topp":
                case "t":
                case "toporfeed":
                    return "Top";
                case "j":
                case "jun":
                case "jungl":
                case "jungle":
                    return "Jungle";
                case "a":
                case "ad":
                case "adc":
                case "carry":
                case "ad carry":
                case "adcarry":
                case "c":
                    return "ADC";
                case "s":
                case "sup":
                case "supp":
                case "support":
                    return "Support";
                default:
                    return pos;
            }
        }
    }
}
