using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration : NadekoTopLevelModule<AdministrationService>
    {
        public enum List { List = 0, Ls = 0 }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageMessages)]
        [Priority(2)]
        public async Task Delmsgoncmd(List _)
        {
            var guild = (SocketGuild)ctx.Guild;
            var (enabled, channels) = _service.GetDelMsgOnCmdData(ctx.Guild.Id);

            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithTitle(GetText("server_delmsgoncmd"))
                .WithDescription(enabled ? "✅" : "❌");

            var str = string.Join("\n", channels
                .Select(x =>
                {
                    var ch = guild.GetChannel(x.ChannelId)?.ToString()
                        ?? x.ChannelId.ToString();
                    var prefix = x.State ? "✅ " : "❌ ";
                    return prefix + ch;
                }));

            if (string.IsNullOrWhiteSpace(str))
                str = "-";

            embed.AddField(GetText("channel_delmsgoncmd"), str);

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        public enum Server { Server }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageMessages)]
        [Priority(1)]
        public async Task Delmsgoncmd(Server _ = Server.Server)
        {
            if (_service.ToggleDeleteMessageOnCommand(ctx.Guild.Id))
            {
                _service.DeleteMessagesOnCommand.Add(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync("delmsg_on").ConfigureAwait(false);
            }
            else
            {
                _service.DeleteMessagesOnCommand.TryRemove(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync("delmsg_off").ConfigureAwait(false);
            }
        }

        public enum Channel { Channel }
        public enum State { Enable, Disable, Inherit }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageMessages)]
        [Priority(0)]
        public Task Delmsgoncmd(Channel _, State s, ITextChannel ch)
            => Delmsgoncmd(_, s, ch.Id);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageMessages)]
        [Priority(1)]
        public async Task Delmsgoncmd(Channel _, State s, ulong? chId = null)
        {
            var actualChId = chId ?? ctx.Channel.Id;
            await _service.SetDelMsgOnCmdState(ctx.Guild.Id, actualChId, s).ConfigureAwait(false);

            if (s == State.Disable)
            {
                await ReplyConfirmLocalizedAsync("delmsg_channel_off").ConfigureAwait(false);
            }
            else if (s == State.Enable)
            {
                await ReplyConfirmLocalizedAsync("delmsg_channel_on").ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalizedAsync("delmsg_channel_inherit").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.DeafenMembers)]
        [BotPerm(GuildPerm.DeafenMembers)]
        public async Task Deafen(params IGuildUser[] users)
        {
            await _service.DeafenUsers(true, users).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("deafen").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.DeafenMembers)]
        [BotPerm(GuildPerm.DeafenMembers)]
        public async Task UnDeafen(params IGuildUser[] users)
        {
            await _service.DeafenUsers(false, users).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("undeafen").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task DelVoiChanl([Leftover] IVoiceChannel voiceChannel)
        {
            await voiceChannel.DeleteAsync().ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("delvoich", Format.Bold(voiceChannel.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task CreatVoiChanl([Leftover] string channelName)
        {
            var ch = await ctx.Guild.CreateVoiceChannelAsync(channelName).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("createvoich", Format.Bold(ch.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task DelTxtChanl([Leftover] ITextChannel toDelete)
        {
            await toDelete.DeleteAsync().ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("deltextchan", Format.Bold(toDelete.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task CreaTxtChanl([Leftover] string channelName)
        {
            var txtCh = await ctx.Guild.CreateTextChannelAsync(channelName).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("createtextchan", Format.Bold(txtCh.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task SetTopic([Leftover] string topic = null)
        {
            var channel = (ITextChannel)ctx.Channel;
            topic = topic ?? "";
            await channel.ModifyAsync(c => c.Topic = topic).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("set_topic").ConfigureAwait(false);

        }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task SetChanlName([Leftover] string name)
        {
            var channel = (ITextChannel)ctx.Channel;
            await channel.ModifyAsync(c => c.Name = name).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("set_channel_name").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task Edit(ulong messageId, [Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            await _service.EditMessage(Context, messageId, text).ConfigureAwait(false);
        }
    }
}