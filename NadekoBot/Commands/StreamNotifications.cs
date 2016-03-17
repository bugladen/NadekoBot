using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Classes.JSONModels;
using NadekoBot.Classes.Permissions;
using NadekoBot.Modules;
using Newtonsoft.Json.Linq;

namespace NadekoBot.Commands {
    internal class StreamNotifications : DiscordCommand {

        private readonly Timer checkTimer = new Timer {
            Interval = new TimeSpan(0, 0, 15).TotalMilliseconds,
        };
        public StreamNotifications(DiscordModule module) : base(module) {

            checkTimer.Elapsed += async (s, e) => {
                try {
                    var streams = NadekoBot.Config.ObservingStreams;
                    if (streams == null || !streams.Any()) return;

                    foreach (var stream in streams) {
                        Tuple<bool, string> data;
                        try {
                            data = await GetStreamStatus(stream);
                        } catch {
                            continue;
                        }

                        if (data.Item1 != stream.LastStatus) {
                            stream.LastStatus = data.Item1;
                            var server = NadekoBot.Client.GetServer(stream.ServerId);
                            var channel = server?.GetChannel(stream.ChannelId);
                            if (channel == null)
                                continue;
                            var msg = $"`{stream.Username}`'s stream is now " +
                                      $"**{(data.Item1 ? "ONLINE" : "OFFLINE")}** with " +
                                      $"**{data.Item2}** viewers.";
                            if (stream.LastStatus)
                                if (stream.Type == StreamNotificationConfig.StreamType.Hitbox)
                                    msg += $"\n`Here is the Link:`【 http://www.hitbox.tv/{stream.Username}/ 】";
                                else if (stream.Type == StreamNotificationConfig.StreamType.Twitch)
                                    msg += $"\n`Here is the Link:`【 http://www.twitch.tv/{stream.Username}/ 】";
                                else if (stream.Type == StreamNotificationConfig.StreamType.YoutubeGaming)
                                    msg += $"\n`Here is the Link:`【 not implemented yet - {stream.Username} 】";
                            await channel.SendMessage(msg);
                        }
                    }
                } catch { }
                ConfigHandler.SaveConfig();
            };

            checkTimer.Start();
        }

        private async Task<Tuple<bool, string>> GetStreamStatus(StreamNotificationConfig stream) {
            bool isLive;
            string response;
            JObject data;
            switch (stream.Type) {
                case StreamNotificationConfig.StreamType.Hitbox:
                    response = await SearchHelper.GetResponseStringAsync($"https://api.hitbox.tv/media/status/{stream.Username}");
                    data = JObject.Parse(response);
                    isLive = data["media_is_live"].ToString() == "1";
                    return new Tuple<bool, string>(isLive, data["media_views"].ToString());
                case StreamNotificationConfig.StreamType.Twitch:
                    response = await SearchHelper.GetResponseStringAsync($"https://api.twitch.tv/kraken/streams/{Uri.EscapeUriString(stream.Username)}");
                    data = JObject.Parse(response);
                    isLive = !string.IsNullOrWhiteSpace(data["stream"].ToString());
                    return new Tuple<bool, string>(isLive, isLive ? data["stream"]["viewers"].ToString() : "0");
                default:
                    break;
            }
            return new Tuple<bool, string>(false, "0");
        }

        internal override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(Module.Prefix + "hitbox")
                .Alias(Module.Prefix + "hb")
                .Description("Notifies this channel when a certain user starts streaming." +
                             "\n**Usage**: ~hitbox SomeStreamer")
                .Parameter("username", ParameterType.Unparsed)
                .AddCheck(SimpleCheckers.ManageServer())
                .Do(TrackStream(StreamNotificationConfig.StreamType.Hitbox));

            cgb.CreateCommand(Module.Prefix + "twitch")
                .Alias(Module.Prefix + "tw")
                .Description("Notifies this channel when a certain user starts streaming." +
                             "\n**Usage**: ~twitch SomeStreamer")
                .AddCheck(SimpleCheckers.ManageServer())
                .Parameter("username", ParameterType.Unparsed)
                .Do(TrackStream(StreamNotificationConfig.StreamType.Twitch));

            cgb.CreateCommand(Module.Prefix + "removestream")
                .Alias(Module.Prefix + "rms")
                .Description("Removes notifications of a certain streamer on this channel." +
                             "\n**Usage**: ~rms SomeGuy")
                .AddCheck(SimpleCheckers.ManageServer())
                .Parameter("username", ParameterType.Unparsed)
                .Do(async e => {
                    var username = e.GetArg("username")?.ToLower().Trim();
                    if (string.IsNullOrWhiteSpace(username))
                        return;

                    var toRemove = NadekoBot.Config.ObservingStreams
                        .FirstOrDefault(snc => snc.ChannelId == e.Channel.Id &&
                                        snc.Username.ToLower().Trim() == username);
                    if (toRemove == null) {
                        await e.Channel.SendMessage(":anger: No such stream.");
                        return;
                    }

                    NadekoBot.Config.ObservingStreams.Remove(toRemove);
                    ConfigHandler.SaveConfig();
                    await e.Channel.SendMessage($":ok: Removed `{toRemove.Username}`'s stream from notifications.");
                });

            cgb.CreateCommand(Module.Prefix + "liststreams")
                .Alias(Module.Prefix + "ls")
                .Description("Lists all streams you are following on this server." +
                             "\n**Usage**: ~ls")
                .Do(async e => {
                    var streams = NadekoBot.Config.ObservingStreams.Where(snc =>
                        snc.ServerId == e.Server.Id);

                    var streamsArray = streams as StreamNotificationConfig[] ?? streams.ToArray();

                    if (streamsArray.Length == 0) {
                        await e.Channel.SendMessage("You are not following any streams on this server.");
                        return;
                    }

                    var text = string.Join("\n", streamsArray.Select(snc => {
                        try {
                            return $"`{snc.Username}`'s stream on **{e.Server.GetChannel(e.Channel.Id).Name}** channel. 【`{snc.Type.ToString()}`】";
                        } catch { }
                        return "";
                    }));

                    await e.Channel.SendMessage($"You are following **{streamsArray.Length}** streams on this server.\n\n" + text);
                });
        }

        private Func<CommandEventArgs, Task> TrackStream(StreamNotificationConfig.StreamType type) =>
            async e => {
                var username = e.GetArg("username");
                if (string.IsNullOrWhiteSpace(username))
                    return;

                var stream = new StreamNotificationConfig {
                    ServerId = e.Server.Id,
                    ChannelId = e.Channel.Id,
                    Username = username,
                    Type = type,
                };
                var exists = NadekoBot.Config.ObservingStreams.Contains(stream);
                if (exists) {
                    await e.Channel.SendMessage(":anger: I am already notifying that stream on this channel.");
                }
                Tuple<bool, string> data;
                try {
                    data = await GetStreamStatus(stream);
                } catch {
                    await e.Channel.SendMessage(":anger: Stream probably doesn't exist.");
                    return;
                }
                var msg = $"Stream is currently **{(data.Item1 ? "ONLINE" : "OFFLINE")}** with **{data.Item2}** viewers";
                if (data.Item1)
                    if (type == StreamNotificationConfig.StreamType.Hitbox)
                        msg += $"\n`Here is the Link:`【 http://www.hitbox.tv/{stream.Username}/ 】";
                    else if (type == StreamNotificationConfig.StreamType.Twitch)
                        msg += $"\n`Here is the Link:`【 http://www.twitch.tv/{stream.Username}/ 】";
                    else if (type == StreamNotificationConfig.StreamType.YoutubeGaming)
                        msg += $"\n`Here is the Link:` not implemented yet - {stream.Username}";
                stream.LastStatus = data.Item1;
                if (!exists)
                    msg = $":ok: I will notify this channel when status changes.\n{msg}";
                await e.Channel.SendMessage(msg);
                NadekoBot.Config.ObservingStreams.Add(stream);
                ConfigHandler.SaveConfig();
            };
    }
}
