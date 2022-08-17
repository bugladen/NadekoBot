﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Extensions
{
    public static class IMessageChannelExtensions
    {
        public static Task<IUserMessage> EmbedAsync(this IMessageChannel ch, EmbedBuilder embed, string msg = "")
            => ch.SendMessageAsync(msg, embed: embed.Build(),
                options: new RequestOptions() { RetryMode  = RetryMode.AlwaysRetry });

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string title, string error, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithErrorColor().WithDescription(error)
                .WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string error)
             => ch.SendMessageAsync("", embed: new EmbedBuilder().WithErrorColor().WithDescription(error).Build());

        public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string title, string text, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithOkColor().WithDescription(text)
                .WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string text)
             => ch.SendMessageAsync("", embed: new EmbedBuilder().WithOkColor().WithDescription(text).Build());

        public static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, string seed, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3)
        {
            var i = 0;
            return ch.SendMessageAsync($@"{seed}```css
{string.Join("\n", items.GroupBy(item => (i++) / columns)
                        .Select(ig => string.Concat(ig.Select(el => howToPrint(el)))))}
```");
        }

        public static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3) =>
            ch.SendTableAsync("", items, howToPrint, columns);
        
        private static readonly IEmote arrow_left = new Emoji("⬅");
        private static readonly IEmote arrow_right = new Emoji("➡");

        public static Task SendPaginatedConfirmAsync(this ICommandContext ctx,
            int currentPage, Func<int, EmbedBuilder> pageFunc, int totalElements,
            int itemsPerPage, bool addPaginatedFooter = true)
            => ctx.SendPaginatedConfirmAsync(currentPage,
                (x) => Task.FromResult(pageFunc(x)), totalElements, itemsPerPage, addPaginatedFooter);
        /// <summary>
        /// danny kamisama
        /// </summary>
        public static async Task SendPaginatedConfirmAsync(this ICommandContext ctx, int currentPage, 
            Func<int, Task<EmbedBuilder>> pageFunc, int totalElements, int itemsPerPage, bool addPaginatedFooter = true)
        {
            var embed = await pageFunc(currentPage).ConfigureAwait(false);

            var lastPage = (totalElements - 1) / itemsPerPage;

            if (addPaginatedFooter)
                embed.AddPaginatedFooter(currentPage, lastPage);

            var msg = await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false) as IUserMessage;

            if (lastPage == 0)
                return;

            await msg.AddReactionAsync(arrow_left).ConfigureAwait(false);
            await msg.AddReactionAsync(arrow_right).ConfigureAwait(false);

            await Task.Delay(2000).ConfigureAwait(false);

            var lastPageChange = DateTime.MinValue;

            async Task changePage(SocketReaction r)
            {
                try
                {
                    if (r.UserId != ctx.User.Id)
                        return;
                    if (DateTime.UtcNow - lastPageChange < TimeSpan.FromSeconds(1))
                        return;
                    if (r.Emote.Name == arrow_left.Name)
                    {
                        if (currentPage == 0)
                            return;
                        lastPageChange = DateTime.UtcNow;
                        var toSend = await pageFunc(--currentPage).ConfigureAwait(false);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                    else if (r.Emote.Name == arrow_right.Name)
                    {
                        if (lastPage > currentPage)
                        {
                            lastPageChange = DateTime.UtcNow;
                            var toSend = await pageFunc(++currentPage).ConfigureAwait(false);
                            if (addPaginatedFooter)
                                toSend.AddPaginatedFooter(currentPage, lastPage);
                            await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception)
                {
                    //ignored
                }
            }

            using (msg.OnReaction((DiscordSocketClient)ctx.Client, changePage, changePage))
            {
                await Task.Delay(30000).ConfigureAwait(false);
            }

            try
            {
                if(msg.Channel is ITextChannel && ((SocketGuild)ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
                {
                    await msg.RemoveAllReactionsAsync().ConfigureAwait(false);
                }
                else
                {
                    await Task.WhenAll(msg.Reactions.Where(x => x.Value.IsMe)
                        .Select(x => msg.RemoveReactionAsync(x.Key, ctx.Client.CurrentUser)));
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
