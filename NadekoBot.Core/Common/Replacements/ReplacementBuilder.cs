﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Music.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

namespace NadekoBot.Common.Replacements
{
    public class ReplacementBuilder
    {
        private static readonly Regex rngRegex = new Regex("%rng(?:(?<from>(?:-)?\\d+)-(?<to>(?:-)?\\d+))?%", RegexOptions.Compiled);
        private ConcurrentDictionary<string, Func<string>> _reps = new ConcurrentDictionary<string, Func<string>>();
        private ConcurrentDictionary<Regex, Func<Match, string>> _regex = new ConcurrentDictionary<Regex, Func<Match, string>>();

        public ReplacementBuilder()
        {
            WithRngRegex();
        }

        public ReplacementBuilder WithDefault(IUser usr, IMessageChannel ch, SocketGuild g, DiscordSocketClient client)
        {
            return this.WithUser(usr)
                .WithChannel(ch)
                .WithServer(client, g)
                .WithClient(client);
        }

        public ReplacementBuilder WithDefault(ICommandContext ctx) =>
            WithDefault(ctx.User, ctx.Channel, ctx.Guild as SocketGuild, (DiscordSocketClient)ctx.Client);

        public ReplacementBuilder WithMention(DiscordSocketClient client)
        {
            /*OBSOLETE*/
            _reps.TryAdd("%mention%", () => $"<@{client.CurrentUser.Id}>");
            /*NEW*/
            _reps.TryAdd("%bot.mention%", () => client.CurrentUser.Mention);
            return this;
        }

        public ReplacementBuilder WithClient(DiscordSocketClient client)
        {
            WithMention(client);

            /*OBSOLETE*/
            _reps.TryAdd("%shardid%", () => client.ShardId.ToString());
            _reps.TryAdd("%time%", () => DateTime.Now.ToString("HH:mm " + TimeZoneInfo.Local.StandardName.GetInitials()));

            /*NEW*/
            _reps.TryAdd("%bot.status%", () => client.Status.ToString());
            _reps.TryAdd("%bot.latency%", () => client.Latency.ToString());
            _reps.TryAdd("%bot.name%", () => client.CurrentUser.Username);
            _reps.TryAdd("%bot.fullname%", () => client.CurrentUser.ToString());
            _reps.TryAdd("%bot.time%", () => DateTime.Now.ToString("HH:mm " + TimeZoneInfo.Local.StandardName.GetInitials()));
            _reps.TryAdd("%bot.discrim%", () => client.CurrentUser.Discriminator);
            _reps.TryAdd("%bot.id%", () => client.CurrentUser.Id.ToString());
            _reps.TryAdd("%bot.avatar%", () => client.CurrentUser.RealAvatarUrl()?.ToString());

            WithStats(client);
            return this;
        }

        public ReplacementBuilder WithServer(DiscordSocketClient client, SocketGuild g)
        {
            /*OBSOLETE*/
            _reps.TryAdd("%sid%", () => g == null ? "DM" : g.Id.ToString());
            _reps.TryAdd("%server%", () => g == null ? "DM" : g.Name);
            _reps.TryAdd("%members%", () => g != null && g is SocketGuild sg ? sg.MemberCount.ToString() : "?");
            _reps.TryAdd("%server_time%", () =>
            {
                TimeZoneInfo to = TimeZoneInfo.Local;
                if (g != null)
                {
                    if (GuildTimezoneService.AllServices.TryGetValue(client.CurrentUser.Id, out var tz))
                        to = tz.GetTimeZoneOrDefault(g.Id) ?? TimeZoneInfo.Local;
                }

                return TimeZoneInfo.ConvertTime(DateTime.UtcNow,
                    TimeZoneInfo.Utc,
                    to).ToString("HH:mm ") + to.StandardName.GetInitials();
            });
            /*NEW*/
            _reps.TryAdd("%server.id%", () => g == null ? "DM" : g.Id.ToString());
            _reps.TryAdd("%server.name%", () => g == null ? "DM" : g.Name);
            _reps.TryAdd("%server.members%", () => g != null && g is SocketGuild sg ? sg.MemberCount.ToString() : "?");
            _reps.TryAdd("%server.time%", () =>
            {
                TimeZoneInfo to = TimeZoneInfo.Local;
                if (g != null)
                {
                    if (GuildTimezoneService.AllServices.TryGetValue(client.CurrentUser.Id, out var tz))
                        to = tz.GetTimeZoneOrDefault(g.Id) ?? TimeZoneInfo.Local;
                }

                return TimeZoneInfo.ConvertTime(DateTime.UtcNow,
                    TimeZoneInfo.Utc,
                    to).ToString("HH:mm ") + to.StandardName.GetInitials();
            });
            return this;
        }

        public ReplacementBuilder WithChannel(IMessageChannel ch)
        {
            /*OBSOLETE*/
            _reps.TryAdd("%channel%", () => (ch as ITextChannel)?.Mention ?? "#" + ch.Name);
            _reps.TryAdd("%chname%", () => ch.Name);
            _reps.TryAdd("%cid%", () => ch?.Id.ToString());
            /*NEW*/
            _reps.TryAdd("%channel.mention%", () => (ch as ITextChannel)?.Mention ?? "#" + ch.Name);
            _reps.TryAdd("%channel.name%", () => ch.Name);
            _reps.TryAdd("%channel.id%", () => ch.Id.ToString());
            _reps.TryAdd("%channel.created%", () => ch.CreatedAt.ToString("HH:mm dd.MM.yyyy"));
            _reps.TryAdd("%channel.nsfw%", () => (ch as ITextChannel)?.IsNsfw.ToString() ?? "-");
            _reps.TryAdd("%channel.topic%", () => (ch as ITextChannel)?.Topic ?? "-");
            return this;
        }

        public ReplacementBuilder WithUser(IUser user)
        {
            /*OBSOLETE*/
            _reps.TryAdd("%user%", () => user.Mention);
            _reps.TryAdd("%userfull%", () => user.ToString());
            _reps.TryAdd("%username%", () => user.Username);
            _reps.TryAdd("%userdiscrim%", () => user.Discriminator);
            _reps.TryAdd("%useravatar%", () => user.RealAvatarUrl()?.ToString());
            _reps.TryAdd("%id%", () => user.Id.ToString());
            _reps.TryAdd("%uid%", () => user.Id.ToString());
            /*NEW*/
            _reps.TryAdd("%user.mention%", () => user.Mention);
            _reps.TryAdd("%user.fullname%", () => user.ToString());
            _reps.TryAdd("%user.name%", () => user.Username);
            _reps.TryAdd("%user.discrim%", () => user.Discriminator);
            _reps.TryAdd("%user.avatar%", () => user.RealAvatarUrl()?.ToString());
            _reps.TryAdd("%user.id%", () => user.Id.ToString());
            _reps.TryAdd("%user.created_time%", () => user.CreatedAt.ToString("HH:mm"));
            _reps.TryAdd("%user.created_date%", () => user.CreatedAt.ToString("dd.MM.yyyy"));
            _reps.TryAdd("%user.joined_time%", () => (user as IGuildUser)?.JoinedAt?.ToString("HH:mm") ?? "-");
            _reps.TryAdd("%user.joined_date%", () => (user as IGuildUser)?.JoinedAt?.ToString("dd.MM.yyyy") ?? "-");
            return this;
        }

        private ReplacementBuilder WithStats(DiscordSocketClient c)
        {
            /*OBSOLETE*/
            _reps.TryAdd("%servers%", () => c.Guilds.Count.ToString());
#if !GLOBAL_NADEKO
            _reps.TryAdd("%users%", () => c.Guilds.Sum(s => s.Users.Count).ToString());
#endif

            /*NEW*/
            _reps.TryAdd("%shard.servercount%", () => c.Guilds.Count.ToString());
#if !GLOBAL_NADEKO
            _reps.TryAdd("%shard.usercount%", () => c.Guilds.Sum(s => s.Users.Count).ToString());
#endif
            _reps.TryAdd("%shard.id%", () => c.ShardId.ToString());
            return this;
        }

        public ReplacementBuilder WithMusic(MusicService ms)
        {
            _reps.TryAdd("%playing%", () =>
            {
                var cnt = ms.MusicPlayers.Count(kvp => kvp.Value.Current.Current != null);
                if (cnt != 1) return cnt.ToString();
                try
                {
                    var mp = ms.MusicPlayers.FirstOrDefault();
                    var title = mp.Value?.Current.Current?.Title;
                    return title ?? "No songs";
                }
                catch
                {
                    return "error";
                }
            });
            _reps.TryAdd("%queued%", () => ms.MusicPlayers.Sum(kvp => kvp.Value.QueueArray().Songs.Length).ToString());

            _reps.TryAdd("%music.queued%", () => ms.MusicPlayers.Sum(kvp => kvp.Value.QueueArray().Songs.Length).ToString());
            _reps.TryAdd("%music.playing%", () =>
            {
                var cnt = ms.MusicPlayers.Count(kvp => kvp.Value.Current.Current != null);
                if (cnt != 1) return cnt.ToString();
                try
                {
                    var mp = ms.MusicPlayers.FirstOrDefault();
                    var title = mp.Value?.Current.Current?.Title;
                    return title ?? "No songs";
                }
                catch
                {
                    return "error";
                }
            });
            return this;
        }

        public ReplacementBuilder WithRngRegex()
        {
            var rng = new NadekoRandom();
            _regex.TryAdd(rngRegex, (match) =>
            {
                if (!int.TryParse(match.Groups["from"].ToString(), out var from))
                    from = 0;
                if (!int.TryParse(match.Groups["to"].ToString(), out var to))
                    to = 0;

                if (from == 0 && to == 0)
                    return rng.Next(0, 11).ToString();

                if (from >= to)
                    return string.Empty;

                return rng.Next(from, to + 1).ToString();
            });
            return this;
        }

        public ReplacementBuilder WithOverride(string key, Func<string> output)
        {
            _reps.AddOrUpdate(key, output, delegate { return output; });
            return this;
        }

        public Replacer Build()
        {
            return new Replacer(_reps.Select(x => (x.Key, x.Value)).ToArray(), _regex.Select(x => (x.Key, x.Value)).ToArray());
        }
    }
}
