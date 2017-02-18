using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Services;
using NadekoBot.Attributes;
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using static NadekoBot.Modules.Permissions.Permissions;
using System.Collections.Concurrent;
using NLog;

namespace NadekoBot.Modules.Administration
{
    [NadekoModule("Administration", ".")]
    public partial class Administration : NadekoModule
    {
        private static ConcurrentHashSet<ulong> deleteMessagesOnCommand { get; }

        private new static readonly Logger _log;

        static Administration()
        {
            _log = LogManager.GetCurrentClassLogger();
            NadekoBot.CommandHandler.CommandExecuted += DelMsgOnCmd_Handler;

            deleteMessagesOnCommand = new ConcurrentHashSet<ulong>(NadekoBot.AllGuildConfigs.Where(g => g.DeleteMessageOnCommand).Select(g => g.GuildId));

        }

        private static Task DelMsgOnCmd_Handler(SocketUserMessage msg, CommandInfo cmd)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    var channel = msg.Channel as SocketTextChannel;
                    if (channel == null)
                        return;
                    if (deleteMessagesOnCommand.Contains(channel.Guild.Id) && cmd.Name != "prune")
                        await msg.DeleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, "Delmsgoncmd errored...");
                }
            });
            return Task.CompletedTask;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetPermissions()
        {
            var channel = (ITextChannel)Context.Channel;
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.PermissionsFor(Context.Guild.Id);
                config.RootPermission = Permission.GetDefaultRoot();
                var toAdd = new PermissionCache()
                {
                    RootPermission = config.RootPermission,
                    PermRole = config.PermissionRole,
                    Verbose = config.VerbosePermissions,
                };
                Cache.AddOrUpdate(channel.Guild.Id,
                    toAdd, (id, old) => toAdd);
                await uow.CompleteAsync();
            }
            await ReplyConfirmLocalized("perms_reset").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Delmsgoncmd()
        {
            bool enabled;
            using (var uow = DbHandler.UnitOfWork())
            {
                var conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                enabled = conf.DeleteMessageOnCommand = !conf.DeleteMessageOnCommand;

                await uow.CompleteAsync();
            }
            if (enabled)
            {
                deleteMessagesOnCommand.Add(Context.Guild.Id);
                await ReplyConfirmLocalized("delmsg_on").ConfigureAwait(false);
            }
            else
            {
                deleteMessagesOnCommand.TryRemove(Context.Guild.Id);
                await ReplyConfirmLocalized("delmsg_off").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Setrole(IGuildUser usr, [Remainder] IRole role)
        {
            try
            {
                await usr.AddRolesAsync(role).ConfigureAwait(false);
                await ReplyConfirmLocalized("setrole", Format.Bold(role.Name), Format.Bold(usr.ToString()))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ReplyErrorLocalized("setrole_err").ConfigureAwait(false);
                Console.WriteLine(ex.ToString());
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Removerole(IGuildUser usr, [Remainder] IRole role)
        {
            try
            {
                await usr.RemoveRolesAsync(role).ConfigureAwait(false);
                await ReplyConfirmLocalized("remrole", Format.Bold(role.Name), Format.Bold(usr.ToString())).ConfigureAwait(false);
            }
            catch
            {
                await ReplyErrorLocalized("remrole_err").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task RenameRole(IRole roleToEdit, string newname)
        {
            try
            {
                if (roleToEdit.Position > (await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false)).GetRoles().Max(r => r.Position))
                {
                    await ReplyErrorLocalized("renrole_perms").ConfigureAwait(false);
                    return;
                }
                await roleToEdit.ModifyAsync(g => g.Name = newname).ConfigureAwait(false);
                await ReplyConfirmLocalized("renrole").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await ReplyErrorLocalized("renrole_err").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task RemoveAllRoles([Remainder] IGuildUser user)
        {
            try
            {
                await user.RemoveRolesAsync(user.GetRoles()).ConfigureAwait(false);
                await ReplyConfirmLocalized("rar", Format.Bold(user.ToString())).ConfigureAwait(false);
            }
            catch
            {
                await ReplyErrorLocalized("rar_err").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task CreateRole([Remainder] string roleName = null)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return;

            var r = await Context.Guild.CreateRoleAsync(roleName).ConfigureAwait(false);
            await ReplyConfirmLocalized("cr", Format.Bold(r.Name)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task RoleColor(params string[] args)
        {
            if (args.Length != 2 && args.Length != 4)
            {
                await ReplyErrorLocalized("rc_params").ConfigureAwait(false);
                return;
            }
            var roleName = args[0].ToUpperInvariant();
            var role = Context.Guild.Roles.FirstOrDefault(r => r.Name.ToUpperInvariant() == roleName);

            if (role == null)
            {
                await ReplyErrorLocalized("rc_not_exist").ConfigureAwait(false);
                return;
            }
            try
            {
                var rgb = args.Length == 4;
                var arg1 = args[1].Replace("#", "");

                var red = Convert.ToByte(rgb ? int.Parse(arg1) : Convert.ToInt32(arg1.Substring(0, 2), 16));
                var green = Convert.ToByte(rgb ? int.Parse(args[2]) : Convert.ToInt32(arg1.Substring(2, 2), 16));
                var blue = Convert.ToByte(rgb ? int.Parse(args[3]) : Convert.ToInt32(arg1.Substring(4, 2), 16));

                await role.ModifyAsync(r => r.Color = new Color(red, green, blue)).ConfigureAwait(false);
                await ReplyConfirmLocalized("rc", Format.Bold(role.Name)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await ReplyErrorLocalized("rc_perms").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Ban(IGuildUser user, [Remainder] string msg = null)
        {
            if (Context.User.Id != user.Guild.OwnerId && (user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max()))
            {
                await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                try
                {
                    await user.SendErrorAsync(GetText("bandm", Format.Bold(Context.Guild.Name), msg));
                }
                catch
                {
                    // ignored
                }
            }

            await Context.Guild.AddBanAsync(user, 7).ConfigureAwait(false);
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle("⛔️ " + GetText("banned_user"))
                    .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Softban(IGuildUser user, [Remainder] string msg = null)
        {
            if (Context.User.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max())
            {
                await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
                return;
            }

            if (!string.IsNullOrWhiteSpace(msg))
            {
                try
                {
                    await user.SendErrorAsync(GetText("sbdm", Format.Bold(Context.Guild.Name), msg));
                }
                catch
                {
                    // ignored
                }
            }

            await Context.Guild.AddBanAsync(user, 7).ConfigureAwait(false);
            try { await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false); }
            catch { await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false); }
            
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle("☣ " + GetText("sb_user"))
                    .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user, [Remainder] string msg = null)
        {
            if (Context.Message.Author.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max())
            {
                await ReplyErrorLocalized("hierarchy").ConfigureAwait(false);
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                try
                {
                    await user.SendErrorAsync(GetText("kickdm", Format.Bold(Context.Guild.Name), msg));
                }
                catch { }
            }

            await user.KickAsync().ConfigureAwait(false);
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("kicked_user"))
                    .AddField(efb => efb.WithName(GetText("username")).WithValue(user.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("ID").WithValue(user.Id.ToString()).WithIsInline(true)))
                .ConfigureAwait(false);
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
            await ReplyConfirmLocalized("createvoich",Format.Bold(ch.Name)).ConfigureAwait(false);
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


        //delets her own messages, no perm required
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Prune()
        {
            var user = await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false);

            var enumerable = (await Context.Channel.GetMessagesAsync().Flatten()).AsEnumerable();
            enumerable = enumerable.Where(x => x.Author.Id == user.Id);
            await Context.Channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);
            Context.Message.DeleteAfter(3);
        }

        // prune x
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Prune(int count)
        {
            if (count < 1)
                return;
            count += 1;
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            int limit = (count < 100) ? count : 100;
            var enumerable = (await Context.Channel.GetMessagesAsync(limit: limit).Flatten().ConfigureAwait(false));
            await Context.Channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);
        }

        //prune @user [x]
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Prune(IGuildUser user, int count = 100)
        {
            if (count < 1)
                return;

            if (user.Id == Context.User.Id)
                count += 1;

            int limit = (count < 100) ? count : 100;
            var enumerable = (await Context.Channel.GetMessagesAsync(limit: limit).Flatten()).Where(m => m.Author == user);
            await Context.Channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);

            Context.Message.DeleteAfter(3);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.MentionEveryone)]
        public async Task MentionRole(params IRole[] roles)
        {
            string send = "❕" +GetText("menrole",Context.User.Mention);
            foreach (var role in roles)
            {
                send += $"\n**{role.Name}**\n";
                send += string.Join(", ", (await Context.Guild.GetUsersAsync()).Where(u => u.GetRoles().Contains(role)).Take(50).Select(u => u.Mention));
            }

            while (send.Length > 2000)
            {
                var curstr = send.Substring(0, 2000);
                await Context.Channel.SendMessageAsync(curstr.Substring(0,
                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                       send.Substring(2000);
            }
            await Context.Channel.SendMessageAsync(send).ConfigureAwait(false);
        }

        private IGuild _nadekoSupportServer;
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Donators()
        {
            IEnumerable<Donator> donatorsOrdered;

            using (var uow = DbHandler.UnitOfWork())
            {
                donatorsOrdered = uow.Donators.GetDonatorsOrdered();
            }
            await Context.Channel.SendConfirmAsync(GetText("donators"), string.Join("⭐", donatorsOrdered.Select(d => d.Name))).ConfigureAwait(false);

            _nadekoSupportServer = _nadekoSupportServer ?? NadekoBot.Client.GetGuild(117523346618318850);

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
            using (var uow = DbHandler.UnitOfWork())
            {
                don = uow.Donators.AddOrUpdateDonator(donator.Id, donator.Username, amount);
                await uow.CompleteAsync();
            }
            await ReplyConfirmLocalized("donadd", don.Amount).ConfigureAwait(false);
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task Timezones(int page = 1)
        //{
        //    page -= 1;

        //    if (page < 0 || page > 20)
        //        return;

        //    var timezones = TimeZoneInfo.GetSystemTimeZones();
        //    var timezonesPerPage = 20;

        //    await Context.Channel.SendPaginatedConfirmAsync(page + 1, (curPage) => new EmbedBuilder()
        //        .WithOkColor()
        //        .WithTitle("Available Timezones")
        //        .WithDescription(string.Join("\n", timezones.Skip((curPage - 1) * timezonesPerPage).Take(timezonesPerPage).Select(x => $"`{x.Id,-25}` UTC{x.BaseUtcOffset:hhmm}"))),
        //        timezones.Count / timezonesPerPage);
        //}

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task Timezone([Remainder] string id)
        //{
        //    TimeZoneInfo tz;
        //    try
        //    {
        //        tz = TimeZoneInfo.FindSystemTimeZoneById(id);
        //        if (tz != null)
        //            await Context.Channel.SendConfirmAsync(tz.ToString()).ConfigureAwait(false);
        //    }
        //    catch (Exception ex)
        //    {
        //        tz = null;
        //        _log.Warn(ex);
        //    }

        //    if (tz == null)
        //        await Context.Channel.SendErrorAsync("Timezone not found. You should specify one of the timezones listed in the 'timezones' command.").ConfigureAwait(false);
        //}
    }
}