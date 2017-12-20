using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using Discord.WebSocket;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration : NadekoTopLevelModule<AdministrationService>
    {
        private IGuild _nadekoSupportServer;
        private readonly DbService _db;

        public Administration(DbService db)
        {
            _db = db;
        }

        public enum List { List = 0, Ls = 0 }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Priority(2)]
        public async Task Delmsgoncmd(List _)
        {
            var guild = (SocketGuild)Context.Guild;
            GuildConfig conf;
            using (var uow = _db.UnitOfWork)
            {
                conf = uow.GuildConfigs.For(Context.Guild.Id,
                    set => set.Include(x => x.DelMsgOnCmdChannels));
            }

            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithTitle(GetText("server_delmsgoncmd"))
                .WithDescription(conf.DeleteMessageOnCommand
                    ? "✅"
                    : "❌");

            var str = string.Join("\n", conf.DelMsgOnCmdChannels
                .Select(x =>
                {
                    var ch = guild.GetChannel(x.ChannelId)?.ToString()
                        ?? x.ChannelId.ToString();
                    var prefix = x.State
                        ? "✅ "
                        : "❌ ";
                    return prefix + ch;
                }));

            if (string.IsNullOrWhiteSpace(str))
                str = "-";

            embed.AddField(GetText("channel_delmsgoncmd"), str);

            await Context.Channel.EmbedAsync(embed)
                .ConfigureAwait(false);
        }

        public enum Server { Server }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Priority(1)]
        public async Task Delmsgoncmd(Server _ = Server.Server)
        {
            bool enabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                enabled = conf.DeleteMessageOnCommand = !conf.DeleteMessageOnCommand;

                await uow.CompleteAsync();
            }
            if (enabled)
            {
                _service.DeleteMessagesOnCommand.Add(Context.Guild.Id);
                await ReplyConfirmLocalized("delmsg_on").ConfigureAwait(false);
            }
            else
            {
                _service.DeleteMessagesOnCommand.TryRemove(Context.Guild.Id);
                await ReplyConfirmLocalized("delmsg_off").ConfigureAwait(false);
            }
        }

        public enum Channel { Channel }
        public enum State { Enable, Disable, Inherit }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Priority(0)]
        public Task Delmsgoncmd(Channel _, State s, ITextChannel ch)
            => Delmsgoncmd(_, s, ch.Id);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Priority(0)]
        public async Task Delmsgoncmd(Channel _, State s, ulong? chId = null)
        {
            chId = chId ?? Context.Channel.Id;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(Context.Guild.Id, 
                    set => set.Include(x => x.DelMsgOnCmdChannels));

                var obj = new DelMsgOnCmdChannel()
                {
                    ChannelId = chId.Value,
                    State = s == State.Enable,
                };
                conf.DelMsgOnCmdChannels.Remove(obj);
                if (s != State.Inherit)
                    conf.DelMsgOnCmdChannels.Add(obj);

                await uow.CompleteAsync();
            }
            if (s == State.Disable)
            {
                _service.DeleteMessagesOnCommandChannels.AddOrUpdate(chId.Value, false, delegate { return false; });
                await ReplyConfirmLocalized("delmsg_channel_off").ConfigureAwait(false);
            }
            else if (s == State.Enable)
            {
                _service.DeleteMessagesOnCommandChannels.AddOrUpdate(chId.Value, true, delegate { return true; });
                await ReplyConfirmLocalized("delmsg_channel_on").ConfigureAwait(false);
            }
            else
            {
                _service.DeleteMessagesOnCommandChannels.TryRemove(chId.Value, out var _);
                await ReplyConfirmLocalized("delmsg_channel_inherit").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.DeafenMembers)]
        [RequireBotPermission(GuildPermission.DeafenMembers)]
        public async Task Deafen(params IGuildUser[] users)
        {
            if (!users.Any())
                return;
            foreach (var u in users)
            {
                try
                {
                    await u.ModifyAsync(usr => usr.Deaf = true).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
            await ReplyConfirmLocalized("deafen").ConfigureAwait(false);

        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.DeafenMembers)]
        [RequireBotPermission(GuildPermission.DeafenMembers)]
        public async Task UnDeafen(params IGuildUser[] users)
        {
            if (!users.Any())
                return;

            foreach (var u in users)
            {
                try
                {
                    await u.ModifyAsync(usr => usr.Deaf = false).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
            await ReplyConfirmLocalized("undeafen").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task DelVoiChanl([Remainder] IVoiceChannel voiceChannel)
        {
            await voiceChannel.DeleteAsync().ConfigureAwait(false);
            await ReplyConfirmLocalized("delvoich", Format.Bold(voiceChannel.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task CreatVoiChanl([Remainder] string channelName)
        {
            var ch = await Context.Guild.CreateVoiceChannelAsync(channelName).ConfigureAwait(false);
            await ReplyConfirmLocalized("createvoich", Format.Bold(ch.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task DelTxtChanl([Remainder] ITextChannel toDelete)
        {
            await toDelete.DeleteAsync().ConfigureAwait(false);
            await ReplyConfirmLocalized("deltextchan", Format.Bold(toDelete.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task CreaTxtChanl([Remainder] string channelName)
        {
            var txtCh = await Context.Guild.CreateTextChannelAsync(channelName).ConfigureAwait(false);
            await ReplyConfirmLocalized("createtextchan", Format.Bold(txtCh.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task SetTopic([Remainder] string topic = null)
        {
            var channel = (ITextChannel)Context.Channel;
            topic = topic ?? "";
            await channel.ModifyAsync(c => c.Topic = topic);
            await ReplyConfirmLocalized("set_topic").ConfigureAwait(false);

        }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task SetChanlName([Remainder] string name)
        {
            var channel = (ITextChannel)Context.Channel;
            await channel.ModifyAsync(c => c.Name = name).ConfigureAwait(false);
            await ReplyConfirmLocalized("set_channel_name").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Donators()
        {
            IEnumerable<Donator> donatorsOrdered;

            using (var uow = _db.UnitOfWork)
            {
                donatorsOrdered = uow.Donators.GetDonatorsOrdered();
            }
            await Context.Channel.SendConfirmAsync(GetText("donators"), string.Join("⭐", donatorsOrdered.Select(d => d.Name))).ConfigureAwait(false);

            _nadekoSupportServer = _nadekoSupportServer ?? (await Context.Client.GetGuildAsync(117523346618318850));

            var patreonRole = _nadekoSupportServer?.GetRole(236667642088259585);
            if (patreonRole == null)
                return;

            var usrs = (await _nadekoSupportServer.GetUsersAsync()).Where(u => u.RoleIds.Contains(236667642088259585u));
            await Context.Channel.SendConfirmAsync("Patreon supporters", string.Join("⭐", usrs.Select(d => d.Username))).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task Donadd(IUser donator, int amount)
        {
            Donator don;
            using (var uow = _db.UnitOfWork)
            {
                don = uow.Donators.AddOrUpdateDonator(donator.Id, donator.Username, amount);
                await uow.CompleteAsync();
            }
            await ReplyConfirmLocalized("donadd", don.Amount).ConfigureAwait(false);
        }
    }
}