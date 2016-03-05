using System;
using System.Collections.Concurrent;
using Discord.Commands;

namespace NadekoBot.Commands {
    internal class RatelimitCommand : IDiscordCommand {

        public static ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, DateTime>> RatelimitingChannels = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, DateTime>>();

        private static readonly TimeSpan ratelimitTime = new TimeSpan(0, 0, 0, 5);

        public RatelimitCommand() {
            NadekoBot.Client.MessageReceived += async (s, e) => {
                if (e.Channel.IsPrivate)
                    return;
                ConcurrentDictionary<ulong, DateTime> userTimePair;
                if (!RatelimitingChannels.TryGetValue(e.Channel.Id, out userTimePair)) return;
                DateTime lastMessageTime;
                if (userTimePair.TryGetValue(e.User.Id, out lastMessageTime)) {
                    if (DateTime.Now - lastMessageTime < ratelimitTime) {
                        try {
                            await e.Message.Delete();
                        } catch { }
                        return;
                    }
                }
                userTimePair.AddOrUpdate(e.User.Id, id => DateTime.Now, (id, dt) => DateTime.Now);
            };
        }

        public void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(".slowmode")
                .Description("Toggles slow mode. When ON, users will be able to send only 1 message every 5 seconds.")
                .Parameter("minutes", ParameterType.Optional)
                .Do(async e => {
                    //var minutesStr = e.GetArg("minutes");
                    //if (string.IsNullOrWhiteSpace(minutesStr)) {
                    //    RatelimitingChannels.Remove(e.Channel.Id);
                    //    return;
                    //}
                    ConcurrentDictionary<ulong, DateTime> throwaway;
                    if (RatelimitingChannels.TryRemove(e.Channel.Id, out throwaway)) {
                        await e.Channel.SendMessage("Slow mode disabled.");
                        return;
                    }
                    if (RatelimitingChannels.TryAdd(e.Channel.Id, new ConcurrentDictionary<ulong, DateTime>())) {
                        await e.Channel.SendMessage("Slow mode initiated. " +
                                                    "Users can't send more than 1 message every 5 seconds.");
                    }
                });
        }
    }
}