using AngleSharp;
using AngleSharp.Dom.Html;
using Discord;
using Discord.WebSocket;
using NadekoBot.DataStructures;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Services.CustomReactions
{
    public static class Extensions
    {
        public static Dictionary<string, Func<IUserMessage, string, string>> responsePlaceholders = new Dictionary<string, Func<IUserMessage, string, string>>()
        {
            {"%target%", (ctx, trigger) => { return ctx.Content.Substring(trigger.Length).Trim().SanitizeMentions(); } },
            {"%rnduser%", (ctx, client) => {
                //var ch = ctx.Channel as ITextChannel;
                //if(ch == null)
                //    return "";

                //var g = ch.Guild as SocketGuild;
                //if(g == null)
                //    return "";
                //try {
                //    var usr = g.Users.Skip(new NadekoRandom().Next(0, g.Users.Count)).FirstOrDefault();
                //    return usr.Mention;
                //}
                //catch {
                return "[%rnduser% is temp. disabled]";
                //}

                //var users = g.Users.ToArray();

                //return users[new NadekoRandom().Next(0, users.Length-1)].Mention;
            } },
        };

        public static Dictionary<string, Func<IUserMessage, DiscordShardedClient, string>> placeholders = new Dictionary<string, Func<IUserMessage, DiscordShardedClient, string>>()
        {
            {"%mention%", (ctx, client) => { return $"<@{client.CurrentUser.Id}>"; } },
            {"%user%", (ctx, client) => { return ctx.Author.Mention; } },
            //{"%rng%", (ctx) => { return new NadekoRandom().Next(0,10).ToString(); } }
        };

        private static readonly Regex rngRegex = new Regex("%rng(?:(?<from>(?:-)?\\d+)-(?<to>(?:-)?\\d+))?%", RegexOptions.Compiled);
        private static readonly Regex imgRegex = new Regex("%(img|image):(?<tag>.*?)%", RegexOptions.Compiled);

        private static readonly NadekoRandom rng = new NadekoRandom();

        public static Dictionary<Regex, Func<Match, Task<string>>> regexPlaceholders = new Dictionary<Regex, Func<Match, Task<string>>>()
        {
            { rngRegex, (match) => {
                int from = 0;
                int.TryParse(match.Groups["from"].ToString(), out from);

                int to = 0;
                int.TryParse(match.Groups["to"].ToString(), out to);

                if(from == 0 && to == 0)
                {
                    return Task.FromResult(rng.Next(0, 11).ToString());
                }

                if(from >= to)
                    return Task.FromResult(string.Empty);

                return Task.FromResult(rng.Next(from,to+1).ToString());
            } },
            { imgRegex, async (match) => {
                var tag = match.Groups["tag"].ToString();
                if(string.IsNullOrWhiteSpace(tag))
                    return "";

                var fullQueryLink = $"http://imgur.com/search?q={ tag }";
                var config = Configuration.Default.WithDefaultLoader();
                var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink);

                var elems = document.QuerySelectorAll("a.image-list-link").ToArray();

                if (!elems.Any())
                    return "";

                var img = (elems.ElementAtOrDefault(new NadekoRandom().Next(0, elems.Length))?.Children?.FirstOrDefault() as IHtmlImageElement);

                if (img?.Source == null)
                    return "";

                return " "+img.Source.Replace("b.", ".") + " ";
            } }
        };

        private static string ResolveTriggerString(this string str, IUserMessage ctx, DiscordShardedClient client)
        {
            foreach (var ph in placeholders)
            {
                if (str.Contains(ph.Key))
                    str = str.ToLowerInvariant().Replace(ph.Key, ph.Value(ctx, client));
            }
            return str;
        }

        private static async Task<string> ResolveResponseStringAsync(this string str, IUserMessage ctx, DiscordShardedClient client, string resolvedTrigger)
        {
            foreach (var ph in placeholders)
            {
                var lowerKey = ph.Key.ToLowerInvariant();
                if (str.Contains(lowerKey))
                    str = str.Replace(lowerKey, ph.Value(ctx, client));
            }

            foreach (var ph in responsePlaceholders)
            {
                var lowerKey = ph.Key.ToLowerInvariant();
                if (str.Contains(lowerKey))
                    str = str.Replace(lowerKey, ph.Value(ctx, resolvedTrigger));
            }

            foreach (var ph in regexPlaceholders)
            {
                str = await ph.Key.ReplaceAsync(str, ph.Value);
            }
            return str;
        }

        public static string TriggerWithContext(this CustomReaction cr, IUserMessage ctx, DiscordShardedClient client)
            => cr.Trigger.ResolveTriggerString(ctx, client);

        public static Task<string > ResponseWithContextAsync(this CustomReaction cr, IUserMessage ctx, DiscordShardedClient client)
            => cr.Response.ResolveResponseStringAsync(ctx, client, cr.Trigger.ResolveTriggerString(ctx, client));

        public static async Task<IUserMessage> Send(this CustomReaction cr, IUserMessage context, DiscordShardedClient client, CustomReactionsService crs)
        {
            var channel = cr.DmResponse ? await context.Author.CreateDMChannelAsync() : context.Channel;

            crs.ReactionStats.AddOrUpdate(cr.Trigger, 1, (k, old) => ++old);

            if (CREmbed.TryParse(cr.Response, out CREmbed crembed))
            {
                return await channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText ?? "");
            }
            return await channel.SendMessageAsync((await cr.ResponseWithContextAsync(context, client)).SanitizeMentions());
        }
    }
}
