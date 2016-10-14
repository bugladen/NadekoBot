using Discord;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.CustomReactions
{
    public static class Extensions
    {
        public static Dictionary<string, Func<IUserMessage, string>> placeholders = new Dictionary<string, Func<IUserMessage, string>>()
        {
            {"%mention%", (ctx) => { return $"<@{NadekoBot.Client.GetCurrentUserAsync().Id}>"; } },
            {"%user%", (ctx) => { return ctx.Author.Mention; } },
            {"%target%", (ctx) => { return ctx.MentionedUsers.Shuffle().FirstOrDefault()?.Mention ?? "Nobody"; } },
            {"%rng%", (ctx) => { return new NadekoRandom().Next(0,10).ToString(); } }
        };

        private static string ResolveCRString(this string str, IUserMessage ctx)
        {
            foreach (var ph in placeholders)
            {
                str = str.Replace(ph.Key, ph.Value(ctx));
            }
            return str;
        }

        public static string TriggerWithContext(this CustomReaction cr, IUserMessage ctx)
            => cr.Trigger.ResolveCRString(ctx);

        public static string ResponseWithContext(this CustomReaction cr, IUserMessage ctx)
            => cr.Response.ResolveCRString(ctx);
    }
}
