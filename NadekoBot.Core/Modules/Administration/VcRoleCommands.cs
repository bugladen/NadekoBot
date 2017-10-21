using System.Collections.Concurrent;
using System.Linq;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class VcRoleCommands : NadekoSubmodule<VcRoleService>
        {
            private readonly DbService _db;

            public VcRoleCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            //todo 999 discord.net [RequireBotPermission(GuildPermission.ManageChannels)]
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

                var guildVcRoles = _service.VcRoles.GetOrAdd(user.GuildId, new ConcurrentDictionary<ulong, IRole>());

                if (role == null)
                {
                    if (guildVcRoles.TryRemove(vc.Id, out role))
                    {
                        await ReplyConfirmLocalized("vcrole_removed", Format.Bold(vc.Name)).ConfigureAwait(false);
                        using (var uow = _db.UnitOfWork)
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
                    using (var uow = _db.UnitOfWork)
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
                if (_service.VcRoles.TryGetValue(Context.Guild.Id, out ConcurrentDictionary<ulong, IRole> roles))
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