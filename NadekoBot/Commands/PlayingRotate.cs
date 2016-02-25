using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using System.Timers;

namespace NadekoBot.Commands {
    class PlayingRotate : DiscordCommand {

        private static List<string> rotatingStatuses = new List<string>();
        private static Timer timer = new Timer(12000);

        private Dictionary<string, Func<string>> playingPlaceholders => new Dictionary<string, Func<string>> {
            {"%servers%", ()=> NadekoBot.client.Servers.Count().ToString() },
            {"%users%", () => NadekoBot.client.Servers.SelectMany(s=>s.Users).Count().ToString() },
            {"%playing%", () => {
                    var cnt = Modules.Music.musicPlayers.Count;
                    if(cnt == 1) {
                        try {
                            var mp = Modules.Music.musicPlayers.FirstOrDefault();
                            return mp.Value.CurrentSong.Title;
                        } catch { }
                    }
                    return cnt.ToString();
                }
            },
            {"%queued%", () => Modules.Music.musicPlayers.Sum(kvp=>kvp.Value.SongQueue.Count).ToString() },
            {"%trivia%", () => Commands.Trivia.runningTrivias.Count.ToString() }
        };
        private object playingPlaceholderLock => new object();

        public PlayingRotate() {
            int i = -1;
            timer.Elapsed += (s, e) => {
                i++;
                Console.WriteLine("elapsed");
                string status = "";
                lock (playingPlaceholderLock) {
                    if (playingPlaceholders.Count == 0)
                        return;
                    if (i >= playingPlaceholders.Count) {
                        i = -1;
                        return;
                    }
                    status = rotatingStatuses[i];
                    foreach (var kvp in playingPlaceholders) {
                        status = status.Replace(kvp.Key, kvp.Value());
                    }
                }
                if (string.IsNullOrWhiteSpace(status))
                    return;
                NadekoBot.client.SetGame(status);
            };
        }

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            if (timer.Enabled)
                timer.Stop();
            else
                timer.Start();
            await e.Channel.SendMessage($"❗`Rotating playing status has been {(timer.Enabled ? "enabled" : "disabled")}.`");
        };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(".rotateplaying")
                .Alias(".ropl")
                .Description("Toggles rotation of playing status of the dynamic strings you specified earlier.")
                .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                .Do(DoFunc());

            cgb.CreateCommand(".addplaying")
                .Alias(".adpl")
                .Description("Adds a specified string to the list of playing strings to rotate. Supported placeholders: " + string.Join(", ", playingPlaceholders.Keys))
                .Parameter("text", ParameterType.Unparsed)
                .Do(async e => {
                    var arg = e.GetArg("text");
                    if (string.IsNullOrWhiteSpace(arg))
                        return;
                    lock (playingPlaceholderLock) {
                        rotatingStatuses.Add(arg);
                    }
                    await e.Channel.SendMessage("🆗 `Added a new paying string.`");
                });

            cgb.CreateCommand(".listplaying")
                .Alias(".lipl")
                .Description("Lists all playing statuses with their corresponding number.")
                .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                .Do(async e => {
                    if (rotatingStatuses.Count == 0)
                        await e.Channel.SendMessage("`There are no playing strings. Add some with .addplaying [text] command.`");
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < rotatingStatuses.Count; i++) {
                        sb.AppendLine($"`{i + 1}.` {rotatingStatuses[i]}");
                    }
                    await e.Channel.SendMessage(sb.ToString());
                });

            cgb.CreateCommand(".removeplaying")
                .Alias(".repl")
                  .Description("Removes a playing string on a given number.")
                  .Parameter("number", ParameterType.Required)
                  .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                  .Do(async e => {
                      var arg = e.GetArg("number");
                      int num;
                      string str;
                      lock (playingPlaceholderLock) {
                          if (!int.TryParse(arg.Trim(), out num) || num <= 0 || num > rotatingStatuses.Count)
                              return;
                          str = rotatingStatuses[num];
                          rotatingStatuses.RemoveAt(num - 1);
                      }
                      await e.Channel.SendMessage($"🆗 `Removed playing string #{num}`({str})");
                  });
        }
    }
}
