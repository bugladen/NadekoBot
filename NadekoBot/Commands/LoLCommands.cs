using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using System.Drawing;
using NadekoBot.Extensions;
using Newtonsoft.Json.Linq;

namespace NadekoBot.Commands {
    internal class LoLCommands : DiscordCommand {

        private class CachedChampion {
            public System.IO.Stream ImageStream { get; set; }
            public DateTime AddedAt { get; set; }
            public string Name { get; set; }
        }

        private static Dictionary<string, CachedChampion> CachedChampionImages = new Dictionary<string, CachedChampion>();
        private readonly object cacheLock = new object();


        private System.Timers.Timer clearTimer { get; } = new System.Timers.Timer();
        public LoLCommands()  {
            clearTimer.Interval = new TimeSpan(0, 10, 0).TotalMilliseconds;
            clearTimer.Start();
            clearTimer.Elapsed += (s, e) => {
                try {
                    lock (cacheLock)
                        CachedChampionImages = CachedChampionImages
                            .Where(kvp => DateTime.Now - kvp.Value.AddedAt > new TimeSpan(1, 0, 0))
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
                catch { }
            };
        }

        private string[] trashTalk = new[] { "Better ban your counters. You are going to carry the game anyway.",
                                        "Go with the flow. Don't think. Just ban one of these.",
                                        "DONT READ BELOW! Ban Urgot mid OP 100%. Im smurf Diamond 1.",
                                        "Ask your teammates what would they like to play, and ban that.",
                                        "If you consider playing teemo, do it. If you consider teemo, you deserve him.",
                                        "Doesn't matter what you ban really. Enemy will ban your main and you will lose." };

        public override Func<CommandEventArgs, Task> DoFunc() {
            throw new NotImplementedException();
        }

        private class MatchupModel {
            public int Games { get; set; }
            public float WinRate { get; set; }
            [Newtonsoft.Json.JsonProperty("key")]
            public string Name { get; set; }
            public float StatScore { get; set; }
        }

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("~lolchamp")
                  .Description("Shows League Of Legends champion statistics. If there are spaces/apostrophes or in the name - omit them. Optional second parameter is a role.\n**Usage**:~lolchamp Riven or ~lolchamp Annie sup")
                  .Parameter("champ", ParameterType.Required)
                  .Parameter("position", ParameterType.Unparsed)
                  .Do(async e => {
                      try {
                          //get role
                          string role = ResolvePos(e.GetArg("position"));
                          string resolvedRole = role;
                          var name = e.GetArg("champ").Replace(" ", "");
                          CachedChampion champ = null;
                          lock (cacheLock) {
                              CachedChampionImages.TryGetValue(name + "_" + resolvedRole, out champ);
                          }
                          if (champ != null) {
                              Console.WriteLine("Sending lol image from cache.");
                              champ.ImageStream.Position = 0;
                              await e.Channel.SendFile("champ.png", champ.ImageStream);
                              return;
                          }
                          var allData = JArray.Parse(await Classes.SearchHelper.GetResponseStringAsync($"http://api.champion.gg/champion/{name}?api_key={NadekoBot.Creds.LOLAPIKey}"));
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
                              resolvedRole = ResolvePos(role);
                          }
                          lock (cacheLock) {
                              CachedChampionImages.TryGetValue(name + "_" + resolvedRole, out champ);
                          }
                          if (champ != null) {
                              Console.WriteLine("Sending lol image from cache.");
                              champ.ImageStream.Position = 0;
                              await e.Channel.SendFile("champ.png", champ.ImageStream);
                              return;
                          }
                          //name = data["title"].ToString();
                          // get all possible roles, and "select" the shown one
                          var roles = new string[allData.Count];
                          for (int i = 0; i < allData.Count; i++) {
                              roles[i] = allData[i]["role"].ToString();
                              if (roles[i] == role)
                                  roles[i] = ">" + roles[i] + "<";
                          }
                          var general = JArray.Parse(await Classes.SearchHelper.GetResponseStringAsync($"http://api.champion.gg/stats/champs/{name}?api_key={NadekoBot.Creds.LOLAPIKey}"))
                                              .Where(jt => jt["role"].ToString() == role)
                                              .FirstOrDefault()?["general"];
                          if (general == null) {
                              Console.WriteLine("General is null.");
                              return;
                          }
                          //get build data for this role
                          var buildData = data["items"]["mostGames"]["items"];
                          var items = new string[6];
                          for (int i = 0; i < 6; i++) {
                              items[i] = buildData[i]["id"].ToString();
                          }

                          //get matchup data to show counters and countered champions
                          var matchupDataIE = data["matchups"].ToObject<List<MatchupModel>>();

                          var matchupData = matchupDataIE.OrderBy(m => m.StatScore).ToArray();

                          var countered = new[] { matchupData[0].Name, matchupData[1].Name, matchupData[2].Name };
                          var counters = new[] { matchupData[matchupData.Length - 1].Name, matchupData[matchupData.Length - 2].Name, matchupData[matchupData.Length - 3].Name };

                          //get runes data
                          var runesJArray = data["runes"]["mostGames"]["runes"] as JArray;
                          var runes = string.Join("\n", runesJArray.OrderBy(jt => int.Parse(jt["number"].ToString())).Select(jt => jt["number"].ToString() + "x" + jt["name"]));

                          // get masteries data

                          var masteries = (data["masteries"]["mostGames"]["masteries"] as JArray);

                          //get skill order data<API_KEY>

                          var orderArr = (data["skills"]["mostGames"]["order"] as JArray);

                          //todo save this for at least 1 hour
                          Image img = Image.FromFile("data/lol/bg.png");
                          using (Graphics g = Graphics.FromImage(img)) {
                              g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                              //g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                              int statsFontSize = 15;
                              int margin = 5;
                              int imageSize = 75;
                              var normalFont = new Font("Monaco", 8, FontStyle.Regular);
                              var smallFont = new Font("Monaco", 7, FontStyle.Regular);
                              //draw champ image
                              var champName = data["key"].ToString().Replace(" ", "");

                              g.DrawImage(GetImage(champName), new Rectangle(margin, margin, imageSize, imageSize));
                              //draw champ name
                              if (champName == "MonkeyKing")
                                  champName = "Wukong";
                              g.DrawString($"{champName}", new Font("Times New Roman", 24, FontStyle.Regular), Brushes.WhiteSmoke, margin + imageSize + margin, margin);
                              //draw champ surname
                              //todo
                              //draw skill order
                              float orderFormula = 120 / orderArr.Count;
                              float orderVerticalSpacing = 10;
                              for (int i = 0; i < orderArr.Count; i++) {
                                  float orderX = margin + margin + imageSize + orderFormula * i + i;
                                  float orderY = margin + 35;
                                  string spellName = orderArr[i].ToString().ToLowerInvariant();

                                  if (spellName == "w")
                                      orderY += orderVerticalSpacing;
                                  else if (spellName == "e")
                                      orderY += orderVerticalSpacing * 2;
                                  else if (spellName == "r")
                                      orderY += orderVerticalSpacing * 3;

                                  g.DrawString(spellName.ToUpperInvariant(), new Font("Monaco", 7), Brushes.LimeGreen, orderX, orderY);
                              }
                              //draw roles
                              g.DrawString("Roles: " + string.Join(", ", roles), normalFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin);

                              //draw average stats
                              g.DrawString(
$@"    Average Stats

Kills: {general["kills"]}       CS: {general["minionsKilled"]}
Deaths: {general["deaths"]}   Win: {general["winPercent"]}%
Assists: {general["assists"]}  Ban: {general["banRate"]}%
", normalFont, Brushes.WhiteSmoke, img.Width - 150, margin);
                              //draw masteries
                              g.DrawString($"Masteries: {string.Join(" / ", masteries?.Select(jt => jt["total"]))}", normalFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin + 20);
                              //draw runes
                              g.DrawString($"{runes}", smallFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin + 40);
                              //draw counters
                              g.DrawString($"Best against", smallFont, Brushes.WhiteSmoke, margin, img.Height - imageSize + margin);
                              int smallImgSize = 50;

                              for (int i = 0; i < counters.Length; i++) {
                                  g.DrawImage(GetImage(counters[i]),
                                              new Rectangle(i * (smallImgSize + margin) + margin, img.Height - smallImgSize - margin,
                                              smallImgSize,
                                              smallImgSize));
                              }
                              //draw countered by
                              g.DrawString($"Worst against", smallFont, Brushes.WhiteSmoke, img.Width - 3 * (smallImgSize + margin), img.Height - imageSize + margin);

                              for (int i = 0; i < countered.Length; i++) {
                                  int j = countered.Length - i;
                                  g.DrawImage(GetImage(countered[i]),
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
                                  g.DrawImage(GetImage(items[i], GetImageType.Item),
                                              new Rectangle(img.Width - (j * (smallImgSize + margin) + margin), 92 + k * (smallImgSize + margin),
                                              smallImgSize,
                                              smallImgSize));
                              }
                          }
                          var cachedChamp = new CachedChampion { AddedAt = DateTime.Now, ImageStream = img.ToStream(System.Drawing.Imaging.ImageFormat.Png), Name = name.ToLower() + "_" + resolvedRole };
                          CachedChampionImages.Add(cachedChamp.Name, cachedChamp);
                          await e.Channel.SendFile(data["title"] + "_stats.png", cachedChamp.ImageStream);
                      }
                      catch (Exception ex) {
                          await e.Channel.SendMessage("💢 Failed retreiving data for that champion.");
                          return;
                      }
                  });

            cgb.CreateCommand("~lolban")
                  .Description("Shows top 6 banned champions ordered by ban rate. Ban these champions and you will be Plat 5 in no time.")
                  .Do(async e => {

                      int showCount = 6;
                      //http://api.champion.gg/stats/champs/mostBanned?api_key=YOUR_API_TOKEN&page=1&limit=2
                      try {
                          var data = JObject.Parse(
                              await Classes
                                  .SearchHelper
                                  .GetResponseStringAsync($"http://api.champion.gg/stats/champs/mostBanned?api_key={NadekoBot.Creds.LOLAPIKey}&page=1&limit={showCount}"))["data"] as JArray;

                          StringBuilder sb = new StringBuilder();
                          sb.AppendLine($"**Showing {showCount} top banned champions.**");
                          sb.AppendLine($"`{trashTalk[new Random().Next(0, trashTalk.Length)]}`");
                          for (int i = 0; i < data.Count; i++) {
                              if (i % 2 == 0 && i != 0)
                                  sb.AppendLine();
                              sb.Append($"`{i + 1}.` **{data[i]["name"]}**  ");
                              //sb.AppendLine($" ({data[i]["general"]["banRate"]}%)");
                          }

                          await e.Channel.SendMessage(sb.ToString());
                      }
                      catch (Exception ex) {
                          await e.Channel.SendMessage($"Fail:\n{ex}");
                      }
                  });
        }

        private enum GetImageType {
            Champion,
            Item
        }
        private Image GetImage(string id, GetImageType imageType = GetImageType.Champion) {
            try {
                switch (imageType) {
                    case GetImageType.Champion:
                        return Image.FromFile($"data/lol/champions/{id}.png");
                    case GetImageType.Item:
                    default:
                        return Image.FromFile($"data/lol/items/{id}.png");
                }
            }
            catch (Exception) {
                return Image.FromFile("data/lol/_ERROR.png");
            }
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
