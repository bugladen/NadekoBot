using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Xp.Extensions;
using NadekoBot.Modules.Xp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Xp
{
    public partial class Xp : NadekoTopLevelModule<XpService>
    {
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Experience(IUser user = null)
        {
            user = user ?? Context.User;
            await Task.Delay(64).ConfigureAwait(false); // wait a bit in case user got XP with this message

            var stats = _service.GetUserStats(Context.Guild.Id, user.Id);

            var levelData = stats.GetLevelData();
            var xpBarStr = _service.GenerateXpBar(levelData.LevelXp, levelData.LevelRequiredXp);

            await Context.Channel.EmbedAsync(new EmbedBuilder()
                .WithTitle(user.ToString())
                //.AddField(GetText("server_level"), stats.ServerLevel.ToString(), true)
                .AddField(GetText("level"), levelData.Level.ToString(), true)
                //.AddField(GetText("club"), stats.ClubName ?? "-", true)
                .AddField(GetText("xp"), xpBarStr, false)
                .WithOkColor())
                .ConfigureAwait(false);
        }

        public enum Server { Server };
        
        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //[OwnerOnly]
        //[Priority(1)]
        //public async Task XpExclude(Server _, IGuild guild)
        //{
        //}

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task XpExclude(Server _)
        {
            var ex = _service.ToggleExcludeServer(Context.Guild.Id);

            await ReplyConfirmLocalized((ex ? "excluded" : "not_excluded"), Format.Bold(Context.Guild.ToString())).ConfigureAwait(false);
        }

        public enum Role { Role };

        [NadekoCommand, Usage, Description, Aliases]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclude(Role _, [Remainder] IRole role)
        {
            var ex = _service.ToggleExcludeRole(Context.Guild.Id, role.Id);

            await ReplyConfirmLocalized((ex ? "excluded" : "not_excluded"), Format.Bold(role.ToString())).ConfigureAwait(false);
        }

        public enum Channel { Channel };

        [NadekoCommand, Usage, Description, Aliases]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclude(Channel _, [Remainder] ITextChannel channel)
        {
            var ex = _service.ToggleExcludeChannel(Context.Guild.Id, channel.Id);

            await ReplyConfirmLocalized((ex ? "excluded" : "not_excluded"), Format.Bold(channel.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclusionList()
        {
            var serverExcluded = _service.IsServerExcluded(Context.Guild.Id);
            var roles = _service.GetExcludedRoles(Context.Guild.Id)
                .Select(x => Context.Guild.GetRole(x)?.Name)
                .Where(x => x != null);

            var chans = (await Task.WhenAll(_service.GetExcludedChannels(Context.Guild.Id)
                .Select(x => Context.Guild.GetChannelAsync(x)))
                .ConfigureAwait(false))
                    .Where(x => x != null)
                    .Select(x => x.Name);

            var embed = new EmbedBuilder()
                .WithTitle(GetText("exclusion_list"))
                .WithDescription((serverExcluded ? GetText("server_is_excluded") : GetText("server_is_not_excluded")))
                .AddField(GetText("excluded_roles"), roles.Any() ? string.Join("\n", roles) : "-", false)
                .AddField(GetText("excluded_channels"), chans.Any() ? string.Join("\n", chans) : "-", false)
                .WithOkColor();

            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }
    }
}
