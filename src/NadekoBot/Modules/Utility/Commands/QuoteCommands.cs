using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class QuoteCommands : ModuleBase
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ListQuotes(int page = 1)
            {
                page -= 1;

                if (page < 0)
                    return;

                IEnumerable<Quote> quotes;
                using (var uow = DbHandler.UnitOfWork())
                {
                    quotes = uow.Quotes.GetGroup(Context.Guild.Id, page * 16, 16);
                }

                if (quotes.Any())
                    await Context.Channel.SendConfirmAsync($"💬 **Page {page + 1} of quotes:**\n```xl\n" + String.Join("\n", quotes.Select((q) => $"{q.Keyword,-20} by {q.AuthorName}")) + "\n```")
                                 .ConfigureAwait(false);
                else
                    await Context.Channel.SendErrorAsync("No quotes on this page.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ShowQuote([Remainder] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote quote;
                using (var uow = DbHandler.Instance.GetUnitOfWork())
                {
                    quote = await uow.Quotes.GetRandomQuoteByKeywordAsync(Context.Guild.Id, keyword).ConfigureAwait(false);
                }

                if (quote == null)
                    return;

                await Context.Channel.SendMessageAsync("📣 " + quote.Text.SanitizeMentions());
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AddQuote(string keyword, [Remainder] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;

                keyword = keyword.ToUpperInvariant();

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.Quotes.Add(new Quote
                    {
                        AuthorId = Context.Message.Author.Id,
                        AuthorName = Context.Message.Author.Username,
                        GuildId = Context.Guild.Id,
                        Keyword = keyword,
                        Text = text,
                    });
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await Context.Channel.SendConfirmAsync("✅ Quote added.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteQuote([Remainder] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                var isAdmin = ((IGuildUser)Context.Message.Author).GuildPermissions.Administrator;

                keyword = keyword.ToUpperInvariant();
                string response;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var qs = uow.Quotes.GetAllQuotesByKeyword(Context.Guild.Id, keyword);

                    if (qs == null || !qs.Any())
                    {
                        await Context.Channel.SendErrorAsync("No quotes found.").ConfigureAwait(false);
                        return;
                    }

                    var q = qs.Shuffle().FirstOrDefault(elem => isAdmin || elem.AuthorId == Context.Message.Author.Id);

                    uow.Quotes.Remove(q);
                    await uow.CompleteAsync().ConfigureAwait(false);
                    response = "🗑 **Deleted a random quote.**";
                }
                await Context.Channel.SendConfirmAsync(response);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task DelAllQuotes([Remainder] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                using (var uow = DbHandler.UnitOfWork())
                {
                    var quotes = uow.Quotes.GetAllQuotesByKeyword(Context.Guild.Id, keyword);

                    uow.Quotes.RemoveRange(quotes.ToArray());//wtf?!

                    await uow.CompleteAsync();
                }

                await Context.Channel.SendConfirmAsync($"🗑 **Deleted all quotes** with **{keyword}** keyword.");
            }
        }
    }
}