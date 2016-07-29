using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Classes.JSONModels;
using NadekoBot.Modules.Music;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace NadekoBot.Modules.Administration.Commands
{
    internal class PlayingRotate : DiscordCommand
    {
        private static readonly Timer timer = new Timer(20000);

        public static Dictionary<string, Func<string>> PlayingPlaceholders { get; } =
            new Dictionary<string, Func<string>> {
                {"%servers%", () => NadekoBot.Client.Servers.Count().ToString()},
                {"%users%", () => NadekoBot.Client.Servers.SelectMany(s => s.Users).Count().ToString()},
                {"%playing%", () => {
                        var cnt = MusicModule.MusicPlayers.Count(kvp => kvp.Value.CurrentSong != null);
                        if (cnt != 1) return cnt.ToString();
                        try {
                            var mp = MusicModule.MusicPlayers.FirstOrDefault();
                            return mp.Value.CurrentSong.SongInfo.Title;
                        }
                        catch {
                            return "No songs";
                        }
                    }
                },
                {"%queued%", () => MusicModule.MusicPlayers.Sum(kvp => kvp.Value.Playlist.Count).ToString()},
                {"%trivia%", () => Games.Commands.TriviaCommands.RunningTrivias.Count.ToString()}
            };

        private readonly SemaphoreSlim playingPlaceholderLock = new SemaphoreSlim(1,1);

        public PlayingRotate(DiscordModule module) : base(module)
        {
            var i = -1;
            timer.Elapsed += async (s, e) =>
            {
                try
                {
                    i++;
                    var status = "";
                    //wtf am i doing, just use a queue ffs
                    await playingPlaceholderLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        if (PlayingPlaceholders.Count == 0
                            || NadekoBot.Config.RotatingStatuses.Count == 0
                            || i >= NadekoBot.Config.RotatingStatuses.Count)
                        {
                            i = 0;
                        }
                        status = NadekoBot.Config.RotatingStatuses[i];
                        status = PlayingPlaceholders.Aggregate(status,
                            (current, kvp) => current.Replace(kvp.Key, kvp.Value()));
                    }
                    finally { playingPlaceholderLock.Release(); }
                    if (string.IsNullOrWhiteSpace(status))
                        return;
                    await Task.Run(() => { NadekoBot.Client.SetGame(status); });
                }
                catch { }
            };

            timer.Enabled = NadekoBot.Config.IsRotatingStatus;
        }

        public Func<CommandEventArgs, Task> DoFunc() => async e =>
        {
            await playingPlaceholderLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (timer.Enabled)
                    timer.Stop();
                else
                    timer.Start();
                NadekoBot.Config.IsRotatingStatus = timer.Enabled;
                await ConfigHandler.SaveConfig().ConfigureAwait(false);
            }
            finally {
                playingPlaceholderLock.Release();
            }
            await e.Channel.SendMessage($"❗`Rotating playing status has been {(timer.Enabled ? "enabled" : "disabled")}.`").ConfigureAwait(false);
        };

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "rotateplaying")
                .Alias(Module.Prefix + "ropl")
                .Description($"Toggles rotation of playing status of the dynamic strings you specified earlier. | `{Prefix}ropl`")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(DoFunc());

            cgb.CreateCommand(Module.Prefix + "addplaying")
                .Alias(Module.Prefix + "adpl")
                .Description("Adds a specified string to the list of playing strings to rotate. " +
                             "Supported placeholders: " + string.Join(", ", PlayingPlaceholders.Keys)+ $" | `{Prefix}adpl`")
                .Parameter("text", ParameterType.Unparsed)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    var arg = e.GetArg("text");
                    if (string.IsNullOrWhiteSpace(arg))
                        return;
                    await playingPlaceholderLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        NadekoBot.Config.RotatingStatuses.Add(arg);
                        await ConfigHandler.SaveConfig();
                    }
                    finally
                    {
                        playingPlaceholderLock.Release();
                    }
                    await e.Channel.SendMessage("🆗 `Added a new playing string.`").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "listplaying")
                .Alias(Module.Prefix + "lipl")
                .Description($"Lists all playing statuses with their corresponding number. | `{Prefix}lipl`")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    if (NadekoBot.Config.RotatingStatuses.Count == 0)
                        await e.Channel.SendMessage("`There are no playing strings. " +
                                                    "Add some with .addplaying [text] command.`").ConfigureAwait(false);
                    var sb = new StringBuilder();
                    for (var i = 0; i < NadekoBot.Config.RotatingStatuses.Count; i++)
                    {
                        sb.AppendLine($"`{i + 1}.` {NadekoBot.Config.RotatingStatuses[i]}");
                    }
                    await e.Channel.SendMessage(sb.ToString()).ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "removeplaying")
                .Alias(Module.Prefix + "repl", Module.Prefix + "rmpl")
                .Description($"Removes a playing string on a given number. | `{Prefix}rmpl`")
                .Parameter("number", ParameterType.Required)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    var arg = e.GetArg("number");
                    int num;
                    string str;
                    await playingPlaceholderLock.WaitAsync().ConfigureAwait(false);
                    try {
                        if (!int.TryParse(arg.Trim(), out num) || num <= 0 || num > NadekoBot.Config.RotatingStatuses.Count)
                            return;
                        str = NadekoBot.Config.RotatingStatuses[num - 1];
                        NadekoBot.Config.RotatingStatuses.RemoveAt(num - 1);
                        await ConfigHandler.SaveConfig().ConfigureAwait(false);
                    }
                    finally { playingPlaceholderLock.Release(); }
                    await e.Channel.SendMessage($"🆗 `Removed playing string #{num}`({str})").ConfigureAwait(false);
                });
        }
    }
}
