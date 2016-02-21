using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot;
using System.Drawing;
using NadekoBot.Extensions;

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
                          //todo save this for at least 1 hour
                          Image img = Image.FromFile("data/lol/bg.png");
                          using (Graphics g = Graphics.FromImage(img)) {
                              int statsFontSize = 15;
                              int margin = 5;
                              int imageSize = 75;
                              var normalFont = new Font("Times New Roman", 9, FontStyle.Regular);
                              //draw champ image
                              g.DrawImage(Image.FromFile($"data/lol/champions/{data["title"].ToString()}.png"), new Rectangle(margin, margin, imageSize, imageSize));
                              //draw champ name
                              g.DrawString($"{data["title"]}", new Font("Times New Roman", 25, FontStyle.Regular), Brushes.WhiteSmoke, margin + imageSize + margin, margin);
                              //draw champ surname
                                //todo
                              //draw roles
                              g.DrawString(
@"    Average Stats

Kills: 5.12    CS: 250.5
Deaths: 0.01   Win: 99%
Assists: 6.55  Ban: 10%
", normalFont, Brushes.WhiteSmoke, img.Width - 150, margin);
                              //draw masteries
                              g.DrawString($"MASTERIES: 18 / 0 / 12", normalFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin + 20);
                              //draw runes
                              g.DrawString(@"RUNES
9 x Greater Mark of Attack Damage
9 x Greater Glyph of Scaling Magic Resist
9 x Greater Seal of Armor
3 x Greater Quintessence of Attack Speed", normalFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin + 40);
                              //draw counters
                              int smallImgSize = 50;
                              var counters = new string[] { "Yasuo", "Yasuo", "Yasuo" };
                              for (int i = 0; i < counters.Length; i++) {
                                  g.DrawImage(Image.FromFile("data/lol/champions/" + counters[i] + ".png"), new Rectangle(i * (smallImgSize + margin) + margin, img.Height - smallImgSize - margin, smallImgSize, smallImgSize));
                              }
                              //draw countered by
                              var countered = new string[] { "Yasuo", "Yasuo", "Yasuo" };
                              for (int i = 0; i < countered.Length; i++) {
                                  g.DrawImage(Image.FromFile("data/lol/champions/" + counters[i] + ".png"), new Rectangle(i * (smallImgSize + margin) + margin, img.Height - smallImgSize - margin, smallImgSize, smallImgSize));
                              }
                              //draw item build
                              var build = new string[] { "Bloodthirster", "Bloodthirster", "Bloodthirster", "Bloodthirster", "Bloodthirster", "Bloodthirster" };
                              /*
                              int kdkdPosTop = 75;
                              Font kdkdFont = new Font("Bodoni MT", statsFontSize, FontStyle.Regular);
                              string killsString = "Kills:";
                              SizeF killsSize = g.MeasureString(killsString, kdkdFont);
                              string deathsString = "Deaths:";
                              SizeF deathsSize = g.MeasureString(deathsString, kdkdFont);
                              string k_dString = "K/D:";
                              SizeF k_dSize = g.MeasureString(k_dString, kdkdFont);
                             // g.DrawString(k_dString, Brushes.WhiteSmoke,200, kdkdPosTop);
                              g.DrawString($"       {general["kills"]}             { general["deaths"]}        { float.Parse(general["kills"].ToString()) / float.Parse(general["deaths"].ToString()):f2}"
                                  ,new Font("Bodoni MT", statsFontSize, FontStyle.Regular), Brushes.OrangeRed, 200, kdkdPosTop);
                                  */
                          }
                          await e.Channel.SendFile(data["title"] + "_stats.png", img.ToStream());
                          await e.Channel.SendMessage(
$@"**`Champion Name:` {data["title"]}

`Minions:` **{general["minionsKilled"]}**
`Win percentage:` **{general["winPercent"]}%**
");
                      }
                      catch (Exception ex) {
                          await e.Channel.SendMessage("💢 Failed retreiving data for that champion.");
                          return;
                      }
                  });
        }
    }
}
