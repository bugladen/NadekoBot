using System.Collections.Concurrent;
using System.Linq;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Core.Services;

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
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task VcRole([Remainder]IRole role = null)
            {
                var user = (IGuildUser)Context.User;

                var vc = user.VoiceChannel;

                if (vc == null || vc.GuildId != user.GuildId)
                {
                    await ReplyErrorLocalized("must_be_in_voice").ConfigureAwait(false);
                    return;
                }

                if (role == null)
                {
                    if (_service.RemoveVcRole(Context.Guild.Id, vc.Id))
                    {
                        await ReplyConfirmLocalized("vcrole_removed", Format.Bold(vc.Name)).ConfigureAwait(false);
                    }
                }
                else
                {
                    _service.AddVcRole(Context.Guild.Id, role, vc.Id);
                    await ReplyConfirmLocalized("vcrole_added", Format.Bold(vc.Name), Format.Bold(role.Name)).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task VcRoleList()
            {
                var guild = (SocketGuild)Context.Guild;
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