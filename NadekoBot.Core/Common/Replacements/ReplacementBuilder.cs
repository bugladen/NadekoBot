using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Modules.Music.Services;
using NadekoBot.Modules.Administration.Services;

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

        public ReplacementBuilder WithDefault(IUser usr, IMessageChannel ch, IGuild g, DiscordSocketClient client)
        {
            return this.WithUser(usr)
                .WithChannel(ch)
                .WithServer(client, g)
                .WithClient(client);
        }

        public ReplacementBuilder WithDefault(ICommandContext ctx) =>
            WithDefault(ctx.User, ctx.Channel, ctx.Guild, (DiscordSocketClient)ctx.Client);

        public ReplacementBuilder WithClient(DiscordSocketClient client)
        {
            _reps.TryAdd("%mention%", () => $"<@{client.CurrentUser.Id}>");
            _reps.TryAdd("%shardid%", () => client.ShardId.ToString());
            _reps.TryAdd("%time%", () => DateTime.Now.ToString("HH:mm " + TimeZoneInfo.Local.StandardName.GetInitials()));
            return this;
        }

        public ReplacementBuilder WithServer(DiscordSocketClient client, IGuild g)
        {

            _reps.TryAdd("%sid%", () => g == null ? "DM" : g.Id.ToString());
            _reps.TryAdd("%server%", () => g == null ? "DM" : g.Name);
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
            return this;
        }

        public ReplacementBuilder WithChannel(IMessageChannel ch)
        {
            _reps.TryAdd("%channel%", () => (ch as ITextChannel)?.Mention ?? "#" + ch.Name);
            _reps.TryAdd("%chname%", () => ch.Name);
            _reps.TryAdd("%cid%", () => ch?.Id.ToString());
            return this;
        }

        public ReplacementBuilder WithUser(IUser user)
        {
            _reps.TryAdd("%user%", () => user.Mention);
            _reps.TryAdd("%userfull%", () => user.ToString());
            _reps.TryAdd("%username%", () => user.Username);
            _reps.TryAdd("%userdiscrim%", () => user.Discriminator);
            _reps.TryAdd("%id%", () => user.Id.ToString());
            _reps.TryAdd("%uid%", () => user.Id.ToString());
            return this;
        }

        public ReplacementBuilder WithStats(DiscordSocketClient c)
        {
            _reps.TryAdd("%servers%", () => c.Guilds.Count.ToString());
            _reps.TryAdd("%users%", () => c.Guilds.Sum(s => s.Users.Count).ToString());
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
            return this;
        }

        public ReplacementBuilder WithRngRegex()
        {
            var rng = new NadekoRandom();
            _regex.TryAdd(rngRegex, (match) =>
            {
                int from = 0;
                int.TryParse(match.Groups["from"].ToString(), out from);

                int to = 0;
                int.TryParse(match.Groups["to"].ToString(), out to);

                if (from == 0 && to == 0)
                {
                    return rng.Next(0, 11).ToString();
                }

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
