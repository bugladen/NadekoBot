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

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            if (e.User.Id != NadekoBot.OwnerID ||
               e.User.Server.Owner.Id != e.User.Id)
                return;
            Channel ch;
            if (!logs.TryRemove(e.Server, out ch)) {
                logs.TryAdd(e.Server, e.Channel);
                await e.Channel.SendMessage($"**I WILL BEGIN LOGGING SERVER ACTIVITY IN THIS CHANNEL**");
                return;
            }

            await e.Channel.SendMessage($"**NO LONGER LOGGIN IN {ch.Mention} CHANNEL**");
        };

        private async void MsgRecivd(object sender, MessageEventArgs e) {
            if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                return;
            Channel ch;
            if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                return;
            await ch.SendMessage($"`Type:` **Message received** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n`{e.User}:` {e.Message.Text}");
        }
        private async void MsgDltd(object sender, MessageEventArgs e) {
            if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                return;
            Channel ch;
            if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                return;
            await ch.SendMessage($"`Type:` **Message deleted** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n`{e.User}:` {e.Message.Text}");
        }
        private async void MsgUpdtd(object sender, MessageUpdatedEventArgs e) {
            if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.client.CurrentUser.Id)
                return;
            Channel ch;
            if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                return;
            await ch.SendMessage($"`Type:` **Message updated** `Time:` **{DateTime.Now}** `Channel:` **{e.Channel.Name}**\n**BEFORE**: `{e.User}:` {e.Before.Text}\n---------------\n**AFTER**: `{e.User}:` {e.Before.Text}");
        }
        private async void UsrUpdtd(object sender, UserUpdatedEventArgs e) {
            string str = $"`Type:` **User updated** `Time:` **{DateTime.Now}**\n";
            if (e.Before.Name != e.After.Name)
                str += $"Name changed from `{e.Before.Name}` to `{e.After.Name}`";
            else if (e.Before.AvatarUrl != e.After.AvatarUrl)
                str += $"Avatar url changed from `{e.Before.AvatarUrl}` to `{e.After.AvatarUrl}`";
            else if (e.Before.Status != e.After.Status)
                str += $"Status changed from `{e.Before.AvatarUrl}` to `{e.After.AvatarUrl}`";
        }
        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(".logserver")
                  .Description("Toggles logging in this channel. Logs every message sent/deleted/edited on the server. BOT OWNER ONLY. SERVER OWNER ONLY.")
                  .Do(DoFunc());
        }
    }
}
