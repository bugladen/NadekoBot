using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using System;
using System.Linq;
using System.Text;

namespace NadekoBot.Modules.Utility.Commands
{
    class InfoCommands : DiscordCommand
    {
        public InfoCommands(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "serverinfo")
                .Alias(Module.Prefix + "sinfo")
                .Description($"Shows info about the server the bot is on. If no channel is supplied, it defaults to current one. |`{Module.Prefix}sinfo Some Server`")
                .Parameter("server", ParameterType.Optional)
                .Do(async e =>
                {
                    var servText = e.GetArg("server")?.Trim();
                    var server = string.IsNullOrWhiteSpace(servText)
                             ? e.Server
                             : NadekoBot.Client.FindServers(servText).FirstOrDefault();
                    if (server == null)
                        return;
                    var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(server.Id >> 22);
                    var sb = new StringBuilder();
                    sb.AppendLine($"`Name:` **#{server.Name}**");
                    sb.AppendLine($"`Owner:` **{server.Owner}**");
                    sb.AppendLine($"`Id:` **{server.Id}**");
                    sb.AppendLine($"`Icon Url:` **{await server.IconUrl.ShortenUrl().ConfigureAwait(false)}**");
                    sb.AppendLine($"`TextChannels:` **{server.TextChannels.Count()}** `VoiceChannels:` **{server.VoiceChannels.Count()}**");
                    sb.AppendLine($"`Members:` **{server.UserCount}** `Online:` **{server.Users.Count(u => u.Status == UserStatus.Online)}** (may be incorrect)");
                    sb.AppendLine($"`Roles:` **{server.Roles.Count()}**");
                    sb.AppendLine($"`Created At:` **{createdAt}**");
                    if (server.CustomEmojis.Count() > 0)
                        sb.AppendLine($"`Custom Emojis:` **{string.Join(", ", server.CustomEmojis)}**");
                    if (server.Features.Count() > 0)
                        sb.AppendLine($"`Features:` **{string.Join(", ", server.Features)}**");
                    if (!string.IsNullOrWhiteSpace(server.SplashId))
                        sb.AppendLine($"`Region:` **{server.Region.Name}**");
                    await e.Channel.SendMessage(sb.ToString()).ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "channelinfo")
                .Alias(Module.Prefix + "cinfo")
                .Description($"Shows info about the channel. If no channel is supplied, it defaults to current one. |`{Module.Prefix}cinfo #some-channel`")
                .Parameter("channel", ParameterType.Optional)
                .Do(async e =>
                {
                    var chText = e.GetArg("channel")?.Trim();
                    var ch = string.IsNullOrWhiteSpace(chText)
                             ? e.Channel
                             : e.Server.FindChannels(chText, Discord.ChannelType.Text).FirstOrDefault();
                    if (ch == null)
                        return;
                    var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ch.Id >> 22);
                    var sb = new StringBuilder();
                    sb.AppendLine($"`Name:` **#{ch.Name}**");
                    sb.AppendLine($"`Id:` **{ch.Id}**");
                    sb.AppendLine($"`Created At:` **{createdAt}**");
                    sb.AppendLine($"`Topic:` **{ch.Topic}**");
                    sb.AppendLine($"`Users:` **{ch.Users.Count()}**");
                    await e.Channel.SendMessage(sb.ToString()).ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "userinfo")
                .Alias(Module.Prefix + "uinfo")
                .Description($"Shows info about the user. If no user is supplied, it defaults a user running the command. |`{Module.Prefix}uinfo @SomeUser`")
                .Parameter("user", ParameterType.Optional)
                .Do(async e =>
                {
                    var userText = e.GetArg("user")?.Trim();
                    var user = string.IsNullOrWhiteSpace(userText)
                             ? e.User
                             : e.Server.FindUsers(userText).FirstOrDefault();
                    if (user == null)
                        return;
                    var sb = new StringBuilder();
                    sb.AppendLine($"`Name#Discrim:` **#{user.Name}#{user.Discriminator}**");
                    if (!string.IsNullOrWhiteSpace(user.Nickname))
                        sb.AppendLine($"`Nickname:` **{user.Nickname}**");
                    sb.AppendLine($"`Id:` **{user.Id}**");
                    sb.AppendLine($"`Current Game:` **{(user.CurrentGame?.Name == null ? "-" : user.CurrentGame.Value.Name)}**");
                    if (user.LastOnlineAt != null)
                        sb.AppendLine($"`Last Online:` **{user.LastOnlineAt:HH:mm:ss}**");
                    sb.AppendLine($"`Joined At:` **{user.JoinedAt}**");
                    sb.AppendLine($"`Roles:` **({user.Roles.Count()}) - {string.Join(", ", user.Roles.Select(r => r.Name))}**");
                    sb.AppendLine($"`AvatarUrl:` **{await user.AvatarUrl.ShortenUrl().ConfigureAwait(false)}**");
                    await e.Channel.SendMessage(sb.ToString()).ConfigureAwait(false);
                });
        }
    }
}
