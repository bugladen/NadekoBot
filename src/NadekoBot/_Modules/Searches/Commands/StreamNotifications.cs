using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Classes.JSONModels;
using NadekoBot.Modules.Permissions.Classes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace NadekoBot.Modules.Searches.Commands
{
    internal class StreamNotifications : DiscordCommand
    {

        private readonly Timer checkTimer = new Timer
        {
            Interval = new TimeSpan(0, 0, 15).TotalMilliseconds,
        };

        private ConcurrentDictionary<string, Tuple<bool, string>> cachedStatuses = new ConcurrentDictionary<string, Tuple<bool, string>>();

        public StreamNotifications(DiscordModule module) : base(module)
        {

            checkTimer.Elapsed += async (s, e) =>
            {
                cachedStatuses.Clear();
                try
                {
                    var streams = SpecificConfigurations.Default.AllConfigs.SelectMany(c => c.ObservingStreams);
                    if (!streams.Any()) return;

                    foreach (var stream in streams)
                    {
                        Tuple<bool, string> data;
                        try
                        {
                            data = await GetStreamStatus(stream).ConfigureAwait(false);
                        }
                        catch
                        {
                            continue;
                        }

                        if (data.Item1 != stream.LastStatus)
                        {
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
                                else if (stream.Type == StreamNotificationConfig.StreamType.Beam)
                                    msg += $"\n`Here is the Link:`【 http://www.beam.pro/{stream.Username}/ 】";
                                else if (stream.Type == StreamNotificationConfig.StreamType.YoutubeGaming)
                                    msg += $"\n`Here is the Link:`【 not implemented yet - {stream.Username} 】";
                            await channel.SendMessage(msg).ConfigureAwait(false);
                        }
                    }
                }
                catch { }
                await ConfigHandler.SaveConfig().ConfigureAwait(false);
            };
            checkTimer.Start();
        }

        private async Task<Tuple<bool, string>> GetStreamStatus(StreamNotificationConfig stream, bool checkCache = true)
        {
            bool isLive;
            string response;
            JObject data;
            Tuple<bool, string> result;
            switch (stream.Type)
            {
                case StreamNotificationConfig.StreamType.Hitbox:
                    var hitboxUrl = $"https://api.hitbox.tv/media/status/{stream.Username}";
                    if (checkCache && cachedStatuses.TryGetValue(hitboxUrl, out result))
                        return result;
                    response = await SearchHelper.GetResponseStringAsync(hitboxUrl).ConfigureAwait(false);
                    data = JObject.Parse(response);
                    isLive = data["media_is_live"].ToString() == "1";
                    result = new Tuple<bool, string>(isLive, data["media_views"].ToString());
                    cachedStatuses.TryAdd(hitboxUrl, result);
                    return result;
                case StreamNotificationConfig.StreamType.Twitch:
                    var twitchUrl = $"https://api.twitch.tv/kraken/streams/{Uri.EscapeUriString(stream.Username)}";
                    if (checkCache && cachedStatuses.TryGetValue(twitchUrl, out result))
                        return result;
                    response = await SearchHelper.GetResponseStringAsync(twitchUrl).ConfigureAwait(false);
                    data = JObject.Parse(response);
                    isLive = !string.IsNullOrWhiteSpace(data["stream"].ToString());
                    result = new Tuple<bool, string>(isLive, isLive ? data["stream"]["viewers"].ToString() : "0");
                    cachedStatuses.TryAdd(twitchUrl, result);
                    return result;
                case StreamNotificationConfig.StreamType.Beam:
                    var beamUrl = $"https://beam.pro/api/v1/channels/{stream.Username}";
                    if (checkCache && cachedStatuses.TryGetValue(beamUrl, out result))
                        return result;
                    response = await SearchHelper.GetResponseStringAsync(beamUrl).ConfigureAwait(false);
                    data = JObject.Parse(response);
                    isLive = data["online"].ToObject<bool>() == true;
                    result = new Tuple<bool, string>(isLive, data["viewersCurrent"].ToString());
                    cachedStatuses.TryAdd(beamUrl, result);
                    return result;
                default:
                    break;
            }
            return new Tuple<bool, string>(false, "0");
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "hitbox")
                .Alias(Module.Prefix + "hb")
                .Description("Notifies this channel when a certain user starts streaming." +
                             $" | `{Prefix}hitbox SomeStreamer`")
                .Parameter("username", ParameterType.Unparsed)
                .AddCheck(SimpleCheckers.ManageServer())
                .Do(TrackStream(StreamNotificationConfig.StreamType.Hitbox));

            cgb.CreateCommand(Module.Prefix + "twitch")
                .Alias(Module.Prefix + "tw")
                .Description("Notifies this channel when a certain user starts streaming." +
                             $" | `{Prefix}twitch SomeStreamer`")
                .AddCheck(SimpleCheckers.ManageServer())
                .Parameter("username", ParameterType.Unparsed)
                .Do(TrackStream(StreamNotificationConfig.StreamType.Twitch));

            cgb.CreateCommand(Module.Prefix + "beam")
                .Alias(Module.Prefix + "bm")
                .Description("Notifies this channel when a certain user starts streaming." +
                             $" | `{Prefix}beam SomeStreamer`")
                .AddCheck(SimpleCheckers.ManageServer())
                .Parameter("username", ParameterType.Unparsed)
                .Do(TrackStream(StreamNotificationConfig.StreamType.Beam));

            cgb.CreateCommand(Module.Prefix + "checkhitbox")
                .Alias(Module.Prefix + "chhb")
                .Description("Checks if a certain user is streaming on the hitbox platform." +
                             $" | `{Prefix}chhb SomeStreamer`")
                .Parameter("username", ParameterType.Unparsed)
                .AddCheck(SimpleCheckers.ManageServer())
                .Do(async e =>
                {
                    var stream = e.GetArg("username")?.Trim();
                    if (string.IsNullOrWhiteSpace(stream))
                        return;
                    try
                    {
                        var streamStatus = (await GetStreamStatus(new StreamNotificationConfig
                        {
                            Username = stream,
                            Type = StreamNotificationConfig.StreamType.Hitbox
                        }));
                        if (streamStatus.Item1)
                        {
                            await e.Channel.SendMessage($"`Streamer {streamStatus.Item2} is online.`");
                        }
                    }
                    catch
                    {
                        await e.Channel.SendMessage("No channel found.");
                    }
                });

            cgb.CreateCommand(Module.Prefix + "checktwitch")
                .Alias(Module.Prefix + "chtw")
                .Description("Checks if a certain user is streaming on the twitch platform." +
                             $" | `{Prefix}chtw SomeStreamer`")
                .AddCheck(SimpleCheckers.ManageServer())
                .Parameter("username", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var stream = e.GetArg("username")?.Trim();
                    if (string.IsNullOrWhiteSpace(stream))
                        return;
                    try
                    {
                        var streamStatus = (await GetStreamStatus(new StreamNotificationConfig
                        {
                            Username = stream,
                            Type = StreamNotificationConfig.StreamType.Twitch
                        }));
                        if (streamStatus.Item1)
                        {
                            await e.Channel.SendMessage($"`Streamer {streamStatus.Item2} is online.`");
                        }
                    }
                    catch
                    {
                        await e.Channel.SendMessage("No channel found.");
                    }
                });

            cgb.CreateCommand(Module.Prefix + "checkbeam")
                .Alias(Module.Prefix + "chbm")
                .Description("Checks if a certain user is streaming on the beam platform." +
                             $" | `{Prefix}chbm SomeStreamer`")
                .AddCheck(SimpleCheckers.ManageServer())
                .Parameter("username", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var stream = e.GetArg("username")?.Trim();
                    if (string.IsNullOrWhiteSpace(stream))
                        return;
                    try
                    {
                        var streamStatus = (await GetStreamStatus(new StreamNotificationConfig
                        {
                            Username = stream,
                            Type = StreamNotificationConfig.StreamType.Beam
                        }));
                        if (streamStatus.Item1)
                        {
                            await e.Channel.SendMessage($"`Streamer {streamStatus.Item2} is online.`");
                        }
                    }
                    catch
                    {
                        await e.Channel.SendMessage("No channel found.");
                    }
                });

            cgb.CreateCommand(Module.Prefix + "removestream")
                .Alias(Module.Prefix + "rms")
                .Description("Removes notifications of a certain streamer on this channel." +
                             $" | `{Prefix}rms SomeGuy`")
                .AddCheck(SimpleCheckers.ManageServer())
                .Parameter("username", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var username = e.GetArg("username")?.ToLower().Trim();
                    if (string.IsNullOrWhiteSpace(username))
                        return;

                    var config = SpecificConfigurations.Default.Of(e.Server.Id);

                    var toRemove = config.ObservingStreams
                        .FirstOrDefault(snc => snc.ChannelId == e.Channel.Id &&
                                        snc.Username.ToLower().Trim() == username);
                    if (toRemove == null)
                    {
                        await e.Channel.SendMessage(":anger: No such stream.").ConfigureAwait(false);
                        return;
                    }

                    config.ObservingStreams.Remove(toRemove);
                    await ConfigHandler.SaveConfig().ConfigureAwait(false);
                    await e.Channel.SendMessage($":ok: Removed `{toRemove.Username}`'s stream from notifications.").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "liststreams")
                .Alias(Module.Prefix + "ls")
                .Description("Lists all streams you are following on this server." +
                             $" | `{Prefix}ls`")
                .Do(async e =>
                {

                    var config = SpecificConfigurations.Default.Of(e.Server.Id);

                    var streams = config.ObservingStreams.Where(snc =>
                        snc.ServerId == e.Server.Id);

                    var streamsArray = streams as StreamNotificationConfig[] ?? streams.ToArray();

                    if (streamsArray.Length == 0)
                    {
                        await e.Channel.SendMessage("You are not following any streams on this server.").ConfigureAwait(false);
                        return;
                    }

                    var text = string.Join("\n", streamsArray.Select(snc =>
                    {
                        try
                        {
                            return $"`{snc.Username}`'s stream on **{e.Server.GetChannel(e.Channel.Id).Name}** channel. 【`{snc.Type.ToString()}`】";
                        }
                        catch { }
                        return "";
                    }));

                    await e.Channel.SendMessage($"You are following **{streamsArray.Length}** streams on this server.\n\n" + text).ConfigureAwait(false);
                });
        }

        private Func<CommandEventArgs, Task> TrackStream(StreamNotificationConfig.StreamType type) =>
            async e =>
            {
                var username = e.GetArg("username")?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(username))
                    return;

                var config = SpecificConfigurations.Default.Of(e.Server.Id);

                var stream = new StreamNotificationConfig
                {
                    ServerId = e.Server.Id,
                    ChannelId = e.Channel.Id,
                    Username = username,
                    Type = type,
                };
                var exists = config.ObservingStreams.Contains(stream);
                if (exists)
                {
                    await e.Channel.SendMessage(":anger: I am already notifying that stream on this channel.").ConfigureAwait(false);
                    return;
                }
                Tuple<bool, string> data;
                try
                {
                    data = await GetStreamStatus(stream).ConfigureAwait(false);
                }
                catch
                {
                    await e.Channel.SendMessage(":anger: Stream probably doesn't exist.").ConfigureAwait(false);
                    return;
                }
                var msg = $"Stream is currently **{(data.Item1 ? "ONLINE" : "OFFLINE")}** with **{data.Item2}** viewers";
                if (data.Item1)
                    if (type == StreamNotificationConfig.StreamType.Hitbox)
                        msg += $"\n`Here is the Link:`【 http://www.hitbox.tv/{stream.Username}/ 】";
                    else if (type == StreamNotificationConfig.StreamType.Twitch)
                        msg += $"\n`Here is the Link:`【 http://www.twitch.tv/{stream.Username}/ 】";
                    else if (type == StreamNotificationConfig.StreamType.Beam)
                        msg += $"\n`Here is the Link:`【 https://beam.pro/{stream.Username}/ 】";
                    else if (type == StreamNotificationConfig.StreamType.YoutubeGaming)
                        msg += $"\n`Here is the Link:` not implemented yet - {stream.Username}";
                stream.LastStatus = data.Item1;
                if (!exists)
                    msg = $":ok: I will notify this channel when status changes.\n{msg}";
                await e.Channel.SendMessage(msg).ConfigureAwait(false);
                config.ObservingStreams.Add(stream);
            };
    }
}
