using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Xp.Common;
using NadekoBot.Modules.Xp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Xp
{
    public partial class Xp : NadekoTopLevelModule<XpService>
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        public Xp(DiscordSocketClient client, DbService db)
        {
            _client = client;
            _db = db;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Experience([Leftover]IUser user = null)
        {
            user = user ?? ctx.User;
            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
            var (img, fmt) = await _service.GenerateXpImageAsync((IGuildUser)user).ConfigureAwait(false);
            using (img)
            {
                await ctx.Channel.SendFileAsync(img, $"{ctx.Guild.Id}_{user.Id}_xp.{fmt.FileExtensions.FirstOrDefault()}")
                    .ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task XpLevelUpRewards(int page = 1)
        {
            page--;

            if (page < 0 || page > 100)
                return Task.CompletedTask;

            var embed = new EmbedBuilder()
                .WithTitle(GetText("level_up_rewards"))
                .WithOkColor();

            var rewards = _service.GetRoleRewards(ctx.Guild.Id)
                .OrderBy(x => x.Level)
                .Select(x =>
                {
                    var str = ctx.Guild.GetRole(x.RoleId)?.ToString();
                    if (str != null)
                        str = GetText("role_reward", Format.Bold(str));
                    return (x.Level, RoleStr: str);
                })
                .Where(x => x.RoleStr != null)
                .Concat(_service.GetCurrencyRewards(ctx.Guild.Id)
                    .OrderBy(x => x.Level)
                    .Select(x => (x.Level, Format.Bold(x.Amount + Bc.BotConfig.CurrencySign))))
                    .GroupBy(x => x.Level)
                    .OrderBy(x => x.Key)
                    .Skip(page * 9)
                    .Take(9)
                    .ForEach(x => embed.AddField(GetText("level_x", x.Key), string.Join("\n", x.Select(y => y.Item2))));

            if (!rewards.Any())
                return ctx.Channel.EmbedAsync(embed.WithDescription(GetText("no_level_up_rewards")));

            return ctx.Channel.EmbedAsync(embed);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [UserPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task XpRoleReward(int level, [Leftover] IRole role = null)
        {
            if (level < 1)
                return;

            _service.SetRoleReward(ctx.Guild.Id, level, role?.Id);

            if (role == null)
                await ReplyConfirmLocalizedAsync("role_reward_cleared", level).ConfigureAwait(false);
            else
                await ReplyConfirmLocalizedAsync("role_reward_added", level, Format.Bold(role.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task XpCurrencyReward(int level, int amount = 0)
        {
            if (level < 1 || amount < 0)
                return;

            _service.SetCurrencyReward(ctx.Guild.Id, level, amount);

            if (amount == 0)
                await ReplyConfirmLocalizedAsync("cur_reward_cleared", level, Bc.BotConfig.CurrencySign).ConfigureAwait(false);
            else
                await ReplyConfirmLocalizedAsync("cur_reward_added", level, Format.Bold(amount + Bc.BotConfig.CurrencySign)).ConfigureAwait(false);
        }

        public enum NotifyPlace
        {
            Server = 0,
            Guild = 0,
            Global = 1,
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpNotify(NotifyPlace place = NotifyPlace.Guild, XpNotificationLocation type = XpNotificationLocation.Channel)
        {
            if (place == NotifyPlace.Guild)
                await _service.ChangeNotificationType(ctx.User.Id, ctx.Guild.Id, type).ConfigureAwait(false);
            else
                await _service.ChangeNotificationType(ctx.User, type).ConfigureAwait(false);
            await ctx.Channel.SendConfirmAsync("👌").ConfigureAwait(false);
        }

        public enum Server { Server };

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpExclude(Server _)
        {
            var ex = _service.ToggleExcludeServer(ctx.Guild.Id);

            await ReplyConfirmLocalizedAsync((ex ? "excluded" : "not_excluded"), Format.Bold(ctx.Guild.ToString())).ConfigureAwait(false);
        }

        public enum Role { Role };

        [NadekoCommand, Usage, Description, Aliases]
        [UserPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclude(Role _, [Leftover] IRole role)
        {
            var ex = _service.ToggleExcludeRole(ctx.Guild.Id, role.Id);

            await ReplyConfirmLocalizedAsync((ex ? "excluded" : "not_excluded"), Format.Bold(role.ToString())).ConfigureAwait(false);
        }

        public enum Channel { Channel };

        [NadekoCommand, Usage, Description, Aliases]
        [UserPerm(GuildPerm.ManageChannels)]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclude(Channel _, [Leftover] ITextChannel channel = null)
        {
            if (channel == null)
                channel = (ITextChannel)ctx.Channel;

            var ex = _service.ToggleExcludeChannel(ctx.Guild.Id, channel.Id);

            await ReplyConfirmLocalizedAsync((ex ? "excluded" : "not_excluded"), Format.Bold(channel.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclusionList()
        {
            var serverExcluded = _service.IsServerExcluded(ctx.Guild.Id);
            var roles = _service.GetExcludedRoles(ctx.Guild.Id)
                .Select(x => ctx.Guild.GetRole(x)?.Name)
                .Where(x => x != null);

            var chans = (await Task.WhenAll(_service.GetExcludedChannels(ctx.Guild.Id)
                .Select(x => ctx.Guild.GetChannelAsync(x)))
                .ConfigureAwait(false))
                    .Where(x => x != null)
                    .Select(x => x.Name);

            var embed = new EmbedBuilder()
                .WithTitle(GetText("exclusion_list"))
                .WithDescription((serverExcluded ? GetText("server_is_excluded") : GetText("server_is_not_excluded")))
                .AddField(GetText("excluded_roles"), roles.Any() ? string.Join("\n", roles) : "-", false)
                .AddField(GetText("excluded_channels"), chans.Any() ? string.Join("\n", chans) : "-", false)
                .WithOkColor();

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task XpLeaderboard(int page = 1)
        {
            if (--page < 0 || page > 100)
                return Task.CompletedTask;

            return ctx.SendPaginatedConfirmAsync(page, (curPage) =>
            {
                var users = _service.GetUserXps(ctx.Guild.Id, curPage);

                var embed = new EmbedBuilder()
                    .WithTitle(GetText("server_leaderboard"))
                    .WithOkColor();

                if (!users.Any())
                    return embed.WithDescription("-");
                else
                {
                    for (int i = 0; i < users.Length; i++)
                    {
                        var levelStats = new LevelStats(users[i].Xp + users[i].AwardedXp);
                        var user = ((SocketGuild)ctx.Guild).GetUser(users[i].UserId);

                        var userXpData = users[i];

                        var awardStr = "";
                        if (userXpData.AwardedXp > 0)
                            awardStr = $"(+{userXpData.AwardedXp})";
                        else if (userXpData.AwardedXp < 0)
                            awardStr = $"({userXpData.AwardedXp.ToString()})";

                        embed.AddField(
                            $"#{(i + 1 + curPage * 9)} {(user?.ToString() ?? users[i].UserId.ToString())}",
                            $"{GetText("level_x", levelStats.Level)} - {levelStats.TotalXp}xp {awardStr}");
                    }
                    return embed;
                }
            }, 1000, 10, addPaginatedFooter: false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpGlobalLeaderboard(int page = 1)
        {
            if (--page < 0 || page > 100)
                return;
            var users = _service.GetUserXps(page);

            var embed = new EmbedBuilder()
                .WithTitle(GetText("global_leaderboard"))
                .WithOkColor();

            if (!users.Any())
                embed.WithDescription("-");
            else
            {
                for (int i = 0; i < users.Length; i++)
                {
                    var user = users[i];
                    embed.AddField(
                        $"#{(i + 1 + page * 9)} {(user.ToString())}",
                        $"{GetText("level_x", new LevelStats(users[i].TotalXp).Level)} - {users[i].TotalXp}xp");
                }
            }

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpAdd(int amount, ulong userId)
        {
            if (amount == 0)
                return;

            _service.AddXp(userId, ctx.Guild.Id, amount);
            var usr = ((SocketGuild)ctx.Guild).GetUser(userId)?.ToString()
                ?? userId.ToString();
            await ReplyConfirmLocalizedAsync("modified", Format.Bold(usr), Format.Bold(amount.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public Task XpAdd(int amount, [Leftover] IGuildUser user)
            => XpAdd(amount, user.Id);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task XpTemplateReload()
        {
            _service.ReloadXpTemplate();
            await Task.Delay(1000).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("template_reloaded").ConfigureAwait(false);
        }
    }
}
