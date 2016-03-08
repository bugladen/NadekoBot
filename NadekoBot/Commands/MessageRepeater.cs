using System;
using System.Timers;
using System.Collections.Concurrent;
using Discord;
using NadekoBot.Classes.Permissions;
using Discord.Commands;

namespace NadekoBot.Commands {
    class MessageRepeater : IDiscordCommand {
        private readonly ConcurrentDictionary<Server, Repeater> repeaters = new ConcurrentDictionary<Server, Repeater>();
        private class Repeater {
            [Newtonsoft.Json.JsonIgnore]
            public readonly Timer MessageTimer;
            [Newtonsoft.Json.JsonIgnore]
            public Channel RepeatingChannel { get; }

            public ulong RepeatingServerId { get; set; }
            public ulong RepeatingChannelId { get; set; }
            public string RepeatingMessage { get; set; }
            public int Interval { get; set; }

            private Repeater(int interval) {
                this.Interval = interval;
                MessageTimer = new Timer {Interval = Interval};
                MessageTimer.Elapsed += async (s, e) => {
                    var ch = RepeatingChannel;
                    var msg = RepeatingMessage;
                    if (ch != null && !string.IsNullOrWhiteSpace(msg)) {
                        try {
                            await ch.SendMessage(msg);
                        } catch { }
                    }
                };
            }

            private Repeater(int interval, ulong channelId, ulong serverId) : this(interval) {
                this.RepeatingChannelId = channelId;
                this.RepeatingServerId = serverId;
            }

            public Repeater(int interval, ulong channelId, ulong serverId, Channel channel) 
                : this(interval,channelId,serverId) {
                this.RepeatingChannel = channel;
            }
        }
        public void Init(CommandGroupBuilder cgb) {

            cgb.CreateCommand(".repeat")
                .Description("Repeat a message every X minutes. If no parameters are specified, " +
                             "repeat is disabled. Requires manage messages.")
                .Parameter("minutes", ParameterType.Optional)
                .Parameter("msg", ParameterType.Unparsed)
                .AddCheck(SimpleCheckers.ManageMessages())
                .Do(async e => {
                    var minutesStr = e.GetArg("minutes");
                    var msg = e.GetArg("msg");

                    // if both null, disable
                    if (string.IsNullOrWhiteSpace(msg) && string.IsNullOrWhiteSpace(minutesStr)) {
                        await e.Channel.SendMessage("Repeating disabled");
                        Repeater rep;
                        if (repeaters.TryGetValue(e.Server, out rep))
                            rep.MessageTimer.Stop();
                        return;
                    }
                    int minutes;
                    if (!int.TryParse(minutesStr, out minutes) || minutes < 1 || minutes > 720) {
                        await e.Channel.SendMessage("Invalid value");
                        return;
                    }

                    var repeater = repeaters.GetOrAdd(
                        e.Server,
                        s => new Repeater(minutes * 60 * 1000, e.Channel.Id, e.Server.Id, e.Channel)
                    );

                    if (!string.IsNullOrWhiteSpace(msg))
                        repeater.RepeatingMessage = msg;

                    repeater.MessageTimer.Stop();
                    repeater.MessageTimer.Start();

                    await e.Channel.SendMessage(String.Format("👌 Repeating `{0}` every " +
                                                              "**{1}** minutes on {2} channel.",
                                                              repeater.RepeatingMessage, minutes, repeater.RepeatingChannel));
                });
        }
    }
}
