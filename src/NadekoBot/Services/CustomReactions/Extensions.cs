using AngleSharp;
using AngleSharp.Dom.Html;
using Discord;
using Discord.WebSocket;
using NadekoBot.DataStructures;
using NadekoBot.DataStructures.Replacements;
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
        private static readonly Regex imgRegex = new Regex("%(img|image):(?<tag>.*?)%", RegexOptions.Compiled);

        private static readonly NadekoRandom rng = new NadekoRandom();

        public static Dictionary<Regex, Func<Match, Task<string>>> regexPlaceholders = new Dictionary<Regex, Func<Match, Task<string>>>()
        {
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

                return " " + img.Source.Replace("b.", ".") + " ";
            } }
        };

        private static string ResolveTriggerString(this string str, IUserMessage ctx, DiscordSocketClient client)
        {
            var rep = new ReplacementBuilder()
                .WithUser(ctx.Author)
                .WithClient(client)
                .Build();

            str = rep.Replace(str.ToLowerInvariant());

            return str;
        }

        private static async Task<string> ResolveResponseStringAsync(this string str, IUserMessage ctx, DiscordSocketClient client, string resolvedTrigger)
        {
            var rep = new ReplacementBuilder()
                .WithDefault(ctx.Author, ctx.Channel, (ctx.Channel as ITextChannel)?.Guild, client)
                .WithOverride("%target%", () => ctx.Content.Substring(resolvedTrigger.Length).Trim())
                .Build();

            str = rep.Replace(str);

            foreach (var ph in regexPlaceholders)
            {
                str = await ph.Key.ReplaceAsync(str, ph.Value);
            }
            return str;
        }

        public static string TriggerWithContext(this CustomReaction cr, IUserMessage ctx, DiscordSocketClient client)
            => cr.Trigger.ResolveTriggerString(ctx, client);

        public static Task<string > ResponseWithContextAsync(this CustomReaction cr, IUserMessage ctx, DiscordSocketClient client)
            => cr.Response.ResolveResponseStringAsync(ctx, client, cr.Trigger.ResolveTriggerString(ctx, client));

        public static async Task<IUserMessage> Send(this CustomReaction cr, IUserMessage ctx, DiscordSocketClient client, CustomReactionsService crs)
        {
            var channel = cr.DmResponse ? await ctx.Author.CreateDMChannelAsync() : ctx.Channel;

            crs.ReactionStats.AddOrUpdate(cr.Trigger, 1, (k, old) => ++old);

            if (CREmbed.TryParse(cr.Response, out CREmbed crembed))
            {
                var rep = new ReplacementBuilder()
                    .WithDefault(ctx.Author, ctx.Channel, (ctx.Channel as ITextChannel)?.Guild, client)
                    .WithOverride("%target%", () => ctx.Content.Substring(cr.Trigger.ResolveTriggerString(ctx, client).Length).Trim())
                    .Build();

                rep.Replace(crembed);

                return await channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText?.SanitizeMentions() ?? "");
            }
            return await channel.SendMessageAsync((await cr.ResponseWithContextAsync(ctx, client)).SanitizeMentions());
        }
    }
}
