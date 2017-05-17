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
using NadekoBot.Modules.Permissions;

namespace NadekoBot.Modules.Administration
{
    [NadekoModule("Administration", ".")]
    public partial class Administration : NadekoTopLevelModule
    {
        private static readonly ConcurrentHashSet<ulong> deleteMessagesOnCommand;

        private new static readonly Logger _log;

        static Administration()
        {
            _log = LogManager.GetCurrentClassLogger();
            NadekoBot.CommandHandler.CommandExecuted += DelMsgOnCmd_Handler;

            deleteMessagesOnCommand = new ConcurrentHashSet<ulong>(NadekoBot.AllGuildConfigs.Where(g => g.DeleteMessageOnCommand).Select(g => g.GuildId));

        }

        private static Task DelMsgOnCmd_Handler(IUserMessage msg, CommandInfo cmd)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    var channel = msg.Channel as SocketTextChannel;
                    if (channel == null)
                        return;
                    if (deleteMessagesOnCommand.Contains(channel.Guild.Id) && cmd.Name != "prune" && cmd.Name != "pick")
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
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
                config.Permissions = Permissionv2.GetDefaultPermlist;
                await uow.CompleteAsync();
                UpdateCache(config);
            }
            await ReplyConfirmLocalized("perms_reset").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task ResetGlobalPermissions()
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var gc = uow.BotConfig.GetOrCreate();
                gc.BlockedCommands.Clear();
                gc.BlockedModules.Clear();

                GlobalPermissionCommands.BlockedCommands.Clear();
                GlobalPermissionCommands.BlockedModules.Clear();
                await uow.CompleteAsync();
            }
            await ReplyConfirmLocalized("global_perms_reset").ConfigureAwait(false);
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
            var guser = (IGuildUser)Context.User;
            var maxRole = guser.GetRoles().Max(x => x.Position);
            if ((Context.User.Id != Context.Guild.OwnerId) && (maxRole < role.Position || maxRole <= usr.GetRoles().Max(x => x.Position)))
                return;
            try
            {
                await usr.AddRoleAsync(role).ConfigureAwait(false);
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
            var guser = (IGuildUser)Context.User;
            if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= usr.GetRoles().Max(x => x.Position))
                return;
            try
            {
                await usr.RemoveRoleAsync(role).ConfigureAwait(false);
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
            var guser = (IGuildUser)Context.User;
            if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= roleToEdit.Position)
                return;
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
            var guser = (IGuildUser)Context.User;

            var userRoles = user.GetRoles();
            if (guser.Id != Context.Guild.OwnerId && 
                (user.Id == Context.Guild.OwnerId || guser.GetRoles().Max(x => x.Position) <= userRoles.Max(x => x.Position)))
                return;
            try
            {
                await user.RemoveRolesAsync(userRoles).ConfigureAwait(false);
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
        public async Task RoleHoist(IRole role)
        {
            await role.ModifyAsync(r => r.Hoist = !role.IsHoisted).ConfigureAwait(false);
            await ReplyConfirmLocalized("rh", Format.Bold(role.Name), Format.Bold(role.IsHoisted.ToString())).ConfigureAwait(false);
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

            var enumerable = (await Context.Channel.GetMessagesAsync().Flatten())
                .Where(x => x.Author.Id == user.Id && DateTime.Now - x.CreatedAt < twoWeeks);
            await Context.Channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);
            Context.Message.DeleteAfter(3);
        }


        private TimeSpan twoWeeks => TimeSpan.FromDays(14);
        // prune x
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Priority(0)]
        public async Task Prune(int count)
        {
            if (count < 1)
                return;
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            int limit = (count < 100) ? count + 1 : 100;
            var enumerable = (await Context.Channel.GetMessagesAsync(limit: limit).Flatten().ConfigureAwait(false))
                .Where(x => DateTime.Now - x.CreatedAt < twoWeeks);
            if (enumerable.FirstOrDefault()?.Id == Context.Message.Id)
                enumerable = enumerable.Skip(1).ToArray();
            else
                enumerable = enumerable.Take(count);
            await Context.Channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);
        }

        //prune @user [x]
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Priority(1)]
        public async Task Prune(IGuildUser user, int count = 100)
        {
            if (count < 1)
                return;

            if (user.Id == Context.User.Id)
                count += 1;

            int limit = (count < 100) ? count : 100;
            var enumerable = (await Context.Channel.GetMessagesAsync(limit: limit).Flatten())
                .Where(m => m.Author == user && DateTime.Now - m.CreatedAt < twoWeeks);
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
                send += string.Join(", ", (await Context.Guild.GetUsersAsync())
                    .Where(u => u.GetRoles().Contains(role))
                    .Take(50).Select(u => u.Mention));
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