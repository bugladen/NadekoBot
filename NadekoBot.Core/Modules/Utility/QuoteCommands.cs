using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Replacements;

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
                using (var uow = _db.UnitOfWork)
                {
                    quotes = uow.Quotes.GetGroup(Context.Guild.Id, page, order);
                }

                if (quotes.Any())
                    await Context.Channel.SendConfirmAsync(GetText("quotes_page", page + 1),
                            string.Join("\n", quotes.Select(q => $"`#{q.Id}` {Format.Bold(q.Keyword.SanitizeMentions()),-20} by {q.AuthorName.SanitizeMentions()}")))
                        .ConfigureAwait(false);
                else
                    await ReplyErrorLocalized("quotes_page_none").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ShowQuote([Remainder] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote quote;
                using (var uow = _db.UnitOfWork)
                {
                    quote =
                        await uow.Quotes.GetRandomQuoteByKeywordAsync(Context.Guild.Id, keyword).ConfigureAwait(false);
                }

                if (quote == null)
                    return;

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);
                    await Context.Channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText?.SanitizeMentions() ?? "")
                        .ConfigureAwait(false);
                    return;
                }
                await Context.Channel.SendMessageAsync($"`#{quote.Id}` 📣 " + rep.Replace(quote.Text)?.SanitizeMentions());
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteSearch(string keyword, [Remainder] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote keywordquote;
                using (var uow = _db.UnitOfWork)
                {
                    keywordquote =
                        await uow.Quotes.SearchQuoteKeywordTextAsync(Context.Guild.Id, keyword, text)
                            .ConfigureAwait(false);
                }

                if (keywordquote == null)
                    return;

                await Context.Channel.SendMessageAsync($"`#{keywordquote.Id}` 💬 " + keyword.ToLowerInvariant() + ":  " +
                                                       keywordquote.Text.SanitizeMentions());
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteId(int id)
            {  
                if (id < 0)
                    return;
                
                using (var uow = _db.UnitOfWork)
                { 
                    var qfromid = uow.Quotes.Get(id);

                    var rep = new ReplacementBuilder()
                        .WithDefault(Context)
                        .Build();

                    if (qfromid == null)
                    {
                        await Context.Channel.SendErrorAsync(GetText("quotes_notfound"));
                    }
                    else if (CREmbed.TryParse(qfromid.Text, out var crembed))
                    {
                        rep.Replace(crembed);

                        await Context.Channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText?.SanitizeMentions() ?? "")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"`#{qfromid.Id} added by {qfromid.AuthorName.SanitizeMentions()}` 🗯️ " + qfromid.Keyword.ToLowerInvariant().SanitizeMentions() + ":  " +
                            rep.Replace(qfromid.Text)?.SanitizeMentions());

                    }

                }
            }        
                          
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AddQuote(string keyword, [Remainder] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;

                keyword = keyword.ToUpperInvariant();

                using (var uow = _db.UnitOfWork)
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
                await ReplyConfirmLocalized("quote_added").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteDelete(int id)
            {
                var isAdmin = ((IGuildUser) Context.Message.Author).GuildPermissions.Administrator;
                
                var success = false;
                string response;
                using (var uow = _db.UnitOfWork)
                {
                    var q = uow.Quotes.Get(id);

                    if (q == null || (!isAdmin && q.AuthorId != Context.Message.Author.Id))
                    {
                        response = GetText("quotes_remove_none");
                    }
                    else
                    {
                        uow.Quotes.Remove(q);
                        await uow.CompleteAsync().ConfigureAwait(false);
                        success = true;
                        response = GetText("quote_deleted", id);
                    }
                }
                if (success)
                    await Context.Channel.SendConfirmAsync(response);
                else
                    await Context.Channel.SendErrorAsync(response);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task DelAllQuotes([Remainder] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                using (var uow = _db.UnitOfWork)
                {
                    uow.Quotes.RemoveAllByKeyword(Context.Guild.Id, keyword.ToUpperInvariant());

                    await uow.CompleteAsync();
                }

                await ReplyConfirmLocalized("quotes_deleted", Format.Bold(keyword.SanitizeMentions())).ConfigureAwait(false);
            }
        }
    }
}
