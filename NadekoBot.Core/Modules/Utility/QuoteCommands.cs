using Discord;
using Discord.Commands;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class QuoteCommands : NadekoSubmodule
        {
            private readonly DbService _db;

            public QuoteCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public Task ListQuotes(OrderType order = OrderType.Keyword)
                => ListQuotes(1, order);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task ListQuotes(int page = 1, OrderType order = OrderType.Keyword)
            {
                page -= 1;
                if (page < 0)
                    return;

                IEnumerable<Quote> quotes;
                using (var uow = _db.GetDbContext())
                {
                    quotes = uow.Quotes.GetGroup(ctx.Guild.Id, page, order);
                }

                if (quotes.Any())
                    await ctx.Channel.SendConfirmAsync(GetText("quotes_page", page + 1),
                            string.Join("\n", quotes.Select(q => $"`#{q.Id}` {Format.Bold(q.Keyword.SanitizeMentions()),-20} by {q.AuthorName.SanitizeMentions()}")))
                        .ConfigureAwait(false);
                else
                    await ReplyErrorLocalizedAsync("quotes_page_none").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ShowQuote([Leftover] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote quote;
                using (var uow = _db.GetDbContext())
                {
                    quote = await uow.Quotes.GetRandomQuoteByKeywordAsync(ctx.Guild.Id, keyword);
                    //if (quote != null)
                    //{
                    //    quote.UseCount += 1;
                    //    uow.Complete();
                    //}
                }

                if (quote == null)
                    return;

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);
                    await ctx.Channel.EmbedAsync(crembed.ToEmbed(), $"`#{quote.Id}` 📣 " + crembed.PlainText?.SanitizeMentions() ?? "")
                        .ConfigureAwait(false);
                    return;
                }
                await ctx.Channel.SendMessageAsync($"`#{quote.Id}` 📣 " + rep.Replace(quote.Text)?.SanitizeMentions()).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteSearch(string keyword, [Leftover] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote keywordquote;
                using (var uow = _db.GetDbContext())
                {
                    keywordquote = await uow.Quotes.SearchQuoteKeywordTextAsync(ctx.Guild.Id, keyword, text);
                }

                if (keywordquote == null)
                    return;

                await ctx.Channel.SendMessageAsync($"`#{keywordquote.Id}` 💬 " + keyword.ToLowerInvariant() + ":  " +
                                                       keywordquote.Text.SanitizeMentions()).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteId(int id)
            {
                if (id < 0)
                    return;

                Quote quote;

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                using (var uow = _db.GetDbContext())
                {
                    quote = uow.Quotes.GetById(id);
                    if (quote.GuildId != ctx.Guild.Id)
                        quote = null;
                }

                if (quote == null)
                {
                    await ctx.Channel.SendErrorAsync(GetText("quotes_notfound")).ConfigureAwait(false);
                    return;
                }

                var infoText = $"`#{quote.Id} added by {quote.AuthorName.SanitizeMentions()}` 🗯️ " + quote.Keyword.ToLowerInvariant().SanitizeMentions() + ":\n";

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);

                    await ctx.Channel.EmbedAsync(crembed.ToEmbed(), infoText + crembed.PlainText?.SanitizeMentions())
                        .ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(infoText + rep.Replace(quote.Text)?.SanitizeMentions())
                        .ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AddQuote(string keyword, [Leftover] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;

                keyword = keyword.ToUpperInvariant();

                using (var uow = _db.GetDbContext())
                {
                    uow.Quotes.Add(new Quote
                    {
                        AuthorId = ctx.Message.Author.Id,
                        AuthorName = ctx.Message.Author.Username,
                        GuildId = ctx.Guild.Id,
                        Keyword = keyword,
                        Text = text,
                    });
                    await uow.SaveChangesAsync();
                }
                await ReplyConfirmLocalizedAsync("quote_added").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteDelete(int id)
            {
                var isAdmin = ((IGuildUser)ctx.Message.Author).GuildPermissions.Administrator;

                var success = false;
                string response;
                using (var uow = _db.GetDbContext())
                {
                    var q = uow.Quotes.GetById(id);

                    if ((q?.GuildId != ctx.Guild.Id) || (!isAdmin && q.AuthorId != ctx.Message.Author.Id))
                    {
                        response = GetText("quotes_remove_none");
                    }
                    else
                    {
                        uow.Quotes.Remove(q);
                        await uow.SaveChangesAsync();
                        success = true;
                        response = GetText("quote_deleted", id);
                    }
                }
                if (success)
                    await ctx.Channel.SendConfirmAsync(response).ConfigureAwait(false);
                else
                    await ctx.Channel.SendErrorAsync(response).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task DelAllQuotes([Leftover] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                using (var uow = _db.GetDbContext())
                {
                    uow.Quotes.RemoveAllByKeyword(ctx.Guild.Id, keyword.ToUpperInvariant());

                    await uow.SaveChangesAsync();
                }

                await ReplyConfirmLocalizedAsync("quotes_deleted", Format.Bold(keyword.SanitizeMentions())).ConfigureAwait(false);
            }
        }
    }
}
