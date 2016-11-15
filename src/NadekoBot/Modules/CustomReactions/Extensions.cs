using Discord;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;

namespace NadekoBot.Modules.CustomReactions
{
    public static class Extensions
    {
        public static Dictionary<string, Func<IUserMessage, string, string>> responsePlaceholders = new Dictionary<string, Func<IUserMessage, string, string>>()
        {
            {"%target%", (ctx, trigger) => { return ctx.Content.Substring(trigger.Length).Trim(); } }
        };

        public static Dictionary<string, Func<IUserMessage, string>> placeholders = new Dictionary<string, Func<IUserMessage, string>>()
        {
            {"%mention%", (ctx) => { return $"<@{NadekoBot.Client.GetCurrentUser().Id}>"; } },
            {"%user%", (ctx) => { return ctx.Author.Mention; } },
            {"%rng%", (ctx) => { return new NadekoRandom().Next(0,10).ToString(); } }
        };

        private static string ResolveTriggerString(this string str, IUserMessage ctx)
        {
            foreach (var ph in placeholders)
            {
                str = str.ToLowerInvariant().Replace(ph.Key, ph.Value(ctx));
            }
            return str;
        }

        private static string ResolveResponseString(this string str, IUserMessage ctx, string resolvedTrigger)
        {
            foreach (var ph in placeholders)
            {
                str = str.Replace(ph.Key.ToLowerInvariant(), ph.Value(ctx));
            }

            foreach (var ph in responsePlaceholders)
            {
                str = str.Replace(ph.Key.ToLowerInvariant(), ph.Value(ctx, resolvedTrigger));
            }
            return str;
        }

        public static string TriggerWithContext(this CustomReaction cr, IUserMessage ctx)
            => cr.Trigger.ResolveTriggerString(ctx);

        public static string ResponseWithContext(this CustomReaction cr, IUserMessage ctx)
            => cr.Response.ResolveResponseString(ctx, cr.Trigger.ResolveTriggerString(ctx));
    }
}
