using System;
using System.Collections.Concurrent;
using System.Linq;
using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class VcRoleCommands : NadekoSubmodule
        {
            private static ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>> vcRoles { get; }

            static VcRoleCommands()
            {
                NadekoBot.Client.UserVoiceStateUpdated += ClientOnUserVoiceStateUpdated;
                vcRoles = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>>();
                foreach (var gconf in NadekoBot.AllGuildConfigs)
                {
                    var g = NadekoBot.Client.GetGuild(gconf.GuildId);
                    if (g == null)
                        continue; //todo delete everything from db if guild doesn't exist?

                    var infos = new ConcurrentDictionary<ulong, IRole>();
                    vcRoles.TryAdd(gconf.GuildId, infos);
                    foreach (var ri in gconf.VcRoleInfos)
                    {
                        var role = g.GetRole(ri.RoleId);
                        if (role == null)
                            continue; //todo remove this entry from db

                        infos.TryAdd(ri.VoiceChannelId, role);
                    }
                }
            }

            private static Task ClientOnUserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState,
                SocketVoiceState newState)
            {

                var gusr = usr as SocketGuildUser;
                if (gusr == null)
                    return Task.CompletedTask;

                var oldVc = oldState.VoiceChannel;
                var newVc = newState.VoiceChannel;
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        if (oldVc != newVc)
                        {
                            ulong guildId;
                            guildId = newVc?.Guild.Id ?? oldVc.Guild.Id;

                            ConcurrentDictionary<ulong, IRole> guildVcRoles;
                            if (vcRoles.TryGetValue(guildId, out guildVcRoles))
                            {
                                IRole role;
                                //remove old
                                if (oldVc != null && guildVcRoles.TryGetValue(oldVc.Id, out role))
                                {
                                    if (gusr.RoleIds.Contains(role.Id))
                                    {
                                        await gusr.RemoveRolesAsync(role).ConfigureAwait(false);
                                        await Task.Delay(500).ConfigureAwait(false);
                                    }
                                }
                                //add new
                                if (newVc != null && guildVcRoles.TryGetValue(newVc.Id, out role))
                                {
                                    if (!gusr.RoleIds.Contains(role.Id))
                                        await gusr.AddRolesAsync(role).ConfigureAwait(false);
                                }
                                
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Administration._log.Warn(ex);
                    }
                });
                return Task.CompletedTask;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            // todo wait for the fix [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task VcRole([Remainder]IRole role = null)
            {
                var user = (IGuildUser) Context.User;

                var vc = user.VoiceChannel;

                if (vc == null || vc.GuildId != user.GuildId)
                {
                    await ReplyErrorLocalized("must_be_in_voice").ConfigureAwait(false);
                    return;
                }

                var guildVcRoles = vcRoles.GetOrAdd(user.GuildId, new ConcurrentDictionary<ulong, IRole>());

                if (role == null)
                {
                    if (guildVcRoles.TryRemove(vc.Id, out role))
                    {
                        await ReplyConfirmLocalized("vcrole_removed", Format.Bold(vc.Name)).ConfigureAwait(false);
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            var conf = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.VcRoleInfos));
                            conf.VcRoleInfos.RemoveWhere(x => x.VoiceChannelId == vc.Id);
                            uow.Complete();
                        }
                    }
                }
                else
                {
                    guildVcRoles.AddOrUpdate(vc.Id, role, (key, old) => role);
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var conf = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.VcRoleInfos));
                        conf.VcRoleInfos.RemoveWhere(x => x.VoiceChannelId == vc.Id); // remove old one
                        conf.VcRoleInfos.Add(new VcRoleInfo() 
                        {
                            VoiceChannelId = vc.Id,
                            RoleId = role.Id,
                        }); // add new one
                        uow.Complete();
                    }
                    await ReplyConfirmLocalized("vcrole_added", Format.Bold(vc.Name), Format.Bold(role.Name)).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task VcRoleList()
            {
                var guild = (SocketGuild) Context.Guild;
                string text;
                ConcurrentDictionary<ulong, IRole> roles;
                if (vcRoles.TryGetValue(Context.Guild.Id, out roles))
                {
                    if (!roles.Any())
                    {
                        text = GetText("no_vcroles");
                    }
                    else
                    {
                        text = string.Join("\n", roles.Select(x =>
                            $"{Format.Bold(guild.GetVoiceChannel(x.Key)?.Name ?? x.Key.ToString())} => {x.Value}"));
                    }
                }
                else
                {
                    text = GetText("no_vcroles");
                }
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("vc_role_list"))
                        .WithDescription(text))
                    .ConfigureAwait(false);
            }
        }
    }
}