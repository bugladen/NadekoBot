using System;
using System.Timers;
using System.Collections.Concurrent;
using Discord;
using NadekoBot.Classes.Permissions;
using Discord.Commands;
using NadekoBot.Modules;

namespace NadekoBot.Commands {
    class MessageRepeater : DiscordCommand {
        private readonly ConcurrentDictionary<Server, Repeater> repeaters = new ConcurrentDictionary<Server, Repeater>();
        private class Repeater {
            [Newtonsoft.Json.JsonIgnore]
            public Timer MessageTimer { get; set; }
            [Newtonsoft.Json.JsonIgnore]
            public Channel RepeatingChannel { get; set; }

            public ulong RepeatingServerId { get; set; }
            public ulong RepeatingChannelId { get; set; }
            public string RepeatingMessage { get; set; }
            public int Interval { get; set; }

            public Repeater Start() {
                MessageTimer = new Timer { Interval = Interval };
                MessageTimer.Elapsed += async (s, e) => {
                    var ch = RepeatingChannel;
                    var msg = RepeatingMessage;
                    if (ch != null && !string.IsNullOrWhiteSpace(msg)) {
                        try {
                            await ch.SendMessage(msg);
                        } catch { }
                    }
                };
                return this;
            }
        }
        internal override void Init(CommandGroupBuilder cgb) {

            cgb.CreateCommand(Module.Prefix + "repeat")
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
                        s => new Repeater {
                            Interval = minutes * 60 * 1000,
                            RepeatingChannel = e.Channel,
                            RepeatingChannelId = e.Channel.Id,
                            RepeatingServerId = e.Server.Id,
                        }.Start()
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

        public MessageRepeater(DiscordModule module) : base(module) {}
    }
}
