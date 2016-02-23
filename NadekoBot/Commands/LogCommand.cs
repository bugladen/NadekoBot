using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;

namespace NadekoBot.Commands {
    class LogCommand : DiscordCommand {

        public LogCommand() : base() {
            NadekoBot.client.MessageReceived += MsgRecivd;
            NadekoBot.client.MessageDeleted += MsgDltd;
            NadekoBot.client.MessageUpdated += MsgUpdtd;
            NadekoBot.client.UserUpdated += UsrUpdtd;
        }

        ConcurrentDictionary<Server, Channel> logs = new ConcurrentDictionary<Server, Channel>();
        ConcurrentDictionary<Server, Channel> loggingPresences = new ConcurrentDictionary<Server, Channel>();
        
        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            if (e.User.Id != NadekoBot.OwnerID ||
                          !e.User.ServerPermissions.ManageServer)
                return;
            Channel ch;
            if (!logs.TryRemove(e.Server, out ch)) {
                logs.TryAdd(e.Server, e.Channel);
                await e.Channel.SendMessage($"**I WILL BEGIN LOGGING SERVER ACTIVITY IN THIS CHANNEL**");
                return;
            }

            await e.Channel.SendMessage($"**NO LONGER LOGGING IN {ch.Mention} CHANNEL**");
        };

        private async void MsgRecivd(object sender, MessageEventArgs e) {
            try {
                if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage($"`Type:` **Message received** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n`{e.User}:` {e.Message.Text}");
            }
            catch { }
        }
        private async void MsgDltd(object sender, MessageEventArgs e) {
            try {
                if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage($"`Type:` **Message deleted** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n`{e.User}:` {e.Message.Text}");
            }
            catch { }
        }
        private async void MsgUpdtd(object sender, MessageUpdatedEventArgs e) {
            try {
                if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage($"`Type:` **Message updated** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n**BEFORE**: `{e.User}:` {e.Before.Text}\n---------------\n**AFTER**: `{e.User}:` {e.Before.Text}");
            }
            catch { }
        }
        private async void UsrUpdtd(object sender, UserUpdatedEventArgs e) {
            try {
                Channel ch;
                if (loggingPresences.TryGetValue(e.Server, out ch))
                    if (e.Before.Status != e.After.Status) {
                        var msg = await ch.SendMessage($"**{e.Before.Name}** is now **{e.After.Status}**.");
                        await Task.Delay(4000);
                        await msg.Delete();
                    }
            }
            catch { }

            try {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                string str = $"`Type:` **User updated** `Time:` **{DateTime.Now}** `User:` **{e.Before.Name}**\n";
                if (e.Before.Name != e.After.Name)
                    str += $"`New name:` **{e.After.Name}**";
                else if (e.Before.AvatarUrl != e.After.AvatarUrl)
                    str += $"`New Avatar:` {e.After.AvatarUrl}";
                else if (e.Before.Status != e.After.Status)
                    str += $"Status `{e.Before.Status}` -> `{e.After.Status}`";
                else
                    return;
                await ch.SendMessage(str);
            }
            catch { }
        }
        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(".logserver")
                  .Description("Toggles logging in this channel. Logs every message sent/deleted/edited on the server. BOT OWNER ONLY. SERVER OWNER ONLY.")
                  .Do(DoFunc());

            cgb.CreateCommand(".userpresence")
                  .Description("Starts logging to this channel when someone from the server goes online/offline/idle. BOT OWNER ONLY. SERVER OWNER ONLY.")
                  .Do(async e => {
                      if (e.User.Id != NadekoBot.OwnerID ||
                          !e.User.ServerPermissions.ManageServer)
                          return;
                      Channel ch;
                      if (!loggingPresences.TryRemove(e.Server, out ch)) {
                          loggingPresences.TryAdd(e.Server, e.Channel);
                          await e.Channel.SendMessage($"**User presence notifications enabled.**");
                          return;
                      }

                      await e.Channel.SendMessage($"**User presence notifications disabled.**");
                  });
        }
    }
}
