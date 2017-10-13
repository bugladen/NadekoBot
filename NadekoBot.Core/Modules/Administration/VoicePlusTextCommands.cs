using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class VoicePlusTextCommands : NadekoSubmodule<VplusTService>
        {
            private readonly DbService _db;

            public VoicePlusTextCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task VoicePlusText()
            {
                var guild = Context.Guild;

                var botUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                if (!botUser.GuildPermissions.ManageRoles || !botUser.GuildPermissions.ManageChannels)
                {
                    await ReplyErrorLocalized("vt_perms").ConfigureAwait(false);
                    return;
                }

                if (!botUser.GuildPermissions.Administrator)
                {
                    try
                    {
                        await ReplyErrorLocalized("vt_no_admin").ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                try
                {
                    bool isEnabled;
                    using (var uow = _db.UnitOfWork)
                    {
                        var conf = uow.GuildConfigs.For(guild.Id, set => set);
                        isEnabled = conf.VoicePlusTextEnabled = !conf.VoicePlusTextEnabled;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    if (!isEnabled)
                    {
                        _service.VoicePlusTextCache.TryRemove(guild.Id);
                        foreach (var textChannel in (await guild.GetTextChannelsAsync().ConfigureAwait(false)).Where(c => c.Name.EndsWith("-voice")))
                        {
                            try { await textChannel.DeleteAsync().ConfigureAwait(false); } catch { }
                            await Task.Delay(500).ConfigureAwait(false);
                        }

                        foreach (var role in guild.Roles.Where(c => c.Name.StartsWith("nvoice-")))
                        {
                            try { await role.DeleteAsync().ConfigureAwait(false); } catch { }
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                        await ReplyConfirmLocalized("vt_disabled").ConfigureAwait(false);
                        return;
                    }
                    _service.VoicePlusTextCache.Add(guild.Id);
                    await ReplyConfirmLocalized("vt_enabled").ConfigureAwait(false);

                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync(ex.ToString()).ConfigureAwait(false);
                }
            }
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            //[RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task CleanVPlusT()
            {
                var guild = Context.Guild;
                var botUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                if (!botUser.GuildPermissions.Administrator)
                {
                    await ReplyErrorLocalized("need_admin").ConfigureAwait(false);
                    return;
                }

                var textChannels = await guild.GetTextChannelsAsync().ConfigureAwait(false);
                var voiceChannels = await guild.GetVoiceChannelsAsync().ConfigureAwait(false);

                var boundTextChannels = textChannels.Where(c => c.Name.EndsWith("-voice"));
                var validTxtChannelNames = new HashSet<string>(voiceChannels.Select(c => _service.GetChannelName(c.Name).ToLowerInvariant()));
                var invalidTxtChannels = boundTextChannels.Where(c => !validTxtChannelNames.Contains(c.Name));

                foreach (var c in invalidTxtChannels)
                {
                    try { await c.DeleteAsync().ConfigureAwait(false); } catch { }
                    await Task.Delay(500).ConfigureAwait(false);
                }
                
                var boundRoles = guild.Roles.Where(r => r.Name.StartsWith("nvoice-"));
                var validRoleNames = new HashSet<string>(voiceChannels.Select(c => _service.GetRoleName(c).ToLowerInvariant()));
                var invalidRoles = boundRoles.Where(r => !validRoleNames.Contains(r.Name));

                foreach (var r in invalidRoles)
                {
                    try { await r.DeleteAsync().ConfigureAwait(false); } catch { }
                    await Task.Delay(500).ConfigureAwait(false);
                }

                await ReplyConfirmLocalized("cleaned_up").ConfigureAwait(false);
            }
        }
    }
}