using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Xp.Common;
using NadekoBot.Modules.Xp.Services;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Xp
{
    public partial class Xp : NadekoTopLevelModule<XpService>
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        public Xp(DiscordSocketClient client,DbService db)
        {
            _client = client;
            _db = db;
        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Experience([Remainder]IUser user = null)
        {
            user = user ?? Context.User;
            await Context.Channel.TriggerTypingAsync();
            using (var img = await _service.GenerateImageAsync((IGuildUser)user))
            {
                await Context.Channel.SendFileAsync(img, $"{user.Id}_xp.png")
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

            var rewards = _service.GetRoleRewards(Context.Guild.Id)
                .OrderBy(x => x.Level)
                .Select(x =>
                {
                    var str = Context.Guild.GetRole(x.RoleId)?.ToString();
                    if (str != null)
                        str = GetText("role_reward", Format.Bold(str));
                    return (x.Level, RoleStr: str);
                })
                .Where(x => x.RoleStr != null)
                .Concat(_service.GetCurrencyRewards(Context.Guild.Id)
                    .OrderBy(x => x.Level)
                    .Select(x => (x.Level, Format.Bold(x.Amount + _bc.BotConfig.CurrencySign))))
                    .GroupBy(x => x.Level)
                    .OrderBy(x => x.Key)
                    .Skip(page * 9)
                    .Take(9)
                    .ForEach(x => embed.AddField(GetText("level_x", x.Key), string.Join("\n", x.Select(y => y.Item2))));

            if (!rewards.Any())
                return Context.Channel.EmbedAsync(embed.WithDescription(GetText("no_level_up_rewards")));

            return Context.Channel.EmbedAsync(embed);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task XpRoleReward(int level, [Remainder] IRole role = null)
        {
            if (level < 1)
                return;

            _service.SetRoleReward(Context.Guild.Id, level, role?.Id);

            if(role == null)
                await ReplyConfirmLocalized("role_reward_cleared", level).ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("role_reward_added", level, Format.Bold(role.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task XpCurrencyReward(int level, int amount=0)
        {
            if (level < 1 || amount < 0)
                return;

            _service.SetCurrencyReward(Context.Guild.Id, level, amount);

            if (amount == 0)
                await ReplyConfirmLocalized("cur_reward_cleared", level, _bc.BotConfig.CurrencySign).ConfigureAwait(false);
            else
                await ReplyConfirmLocalized("cur_reward_added", level, Format.Bold(amount + _bc.BotConfig.CurrencySign)).ConfigureAwait(false);
        }

        public enum NotifyPlace
        {
            Server = 0,
            Guild = 0,
            Global = 1,
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpNotify(NotifyPlace place = NotifyPlace.Guild, XpNotificationType type = XpNotificationType.Channel)
        {
            if (place == NotifyPlace.Guild)
                await _service.ChangeNotificationType(Context.User.Id, Context.Guild.Id, type);
            else
                await _service.ChangeNotificationType(Context.User, type);
            await Context.Channel.SendConfirmAsync("👌").ConfigureAwait(false);
        }

        public enum Server { Server };

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
        public async Task XpExclude(Channel _, [Remainder] ITextChannel channel = null)
        {
            if (channel == null)
                channel = (ITextChannel)Context.Channel;

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

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task XpLeaderboard(int page = 1)
        {
            if (--page < 0 || page > 100)
                return Task.CompletedTask;

            return Context.SendPaginatedConfirmAsync(page, (curPage) =>
            {
                var users = _service.GetUserXps(Context.Guild.Id, curPage);

                var embed = new EmbedBuilder()
                    .WithTitle(GetText("server_leaderboard"))
                    .WithOkColor();

                if (!users.Any())
                    return embed.WithDescription("-");
                else
                {
                    for (int i = 0; i < users.Length; i++)
                    {
                        var levelStats = LevelStats.FromXp(users[i].Xp + users[i].AwardedXp);
                        var user = ((SocketGuild)Context.Guild).GetUser(users[i].UserId);

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
                        $"{GetText("level_x", LevelStats.FromXp(users[i].TotalXp).Level)} - {users[i].TotalXp}xp");
                }
            }

            await Context.Channel.EmbedAsync(embed);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task XpAdd(int amount, ulong userId)
        {
            if (amount == 0)
                return;

            _service.AddXp(userId, Context.Guild.Id, amount);
            var usr = ((SocketGuild)Context.Guild).GetUser(userId)?.ToString()
                ?? userId.ToString();
            await ReplyConfirmLocalized("modified", Format.Bold(usr), Format.Bold(amount.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task XpAdd(int amount, [Remainder] IGuildUser user) 
            => XpAdd(amount, user.Id);
    }
}
