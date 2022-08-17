﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using NadekoBot.Modules.CustomReactions.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.CustomReactions
{
    public class CustomReactions : NadekoTopLevelModule<CustomReactionsService>
    {
        private readonly IBotCredentials _creds;
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        public CustomReactions(IBotCredentials creds, DbService db,
            DiscordSocketClient client)
        {
            _creds = creds;
            _db = db;
            _client = client;
        }

        private bool AdminInGuildOrOwnerInDm() => (ctx.Guild == null && _creds.IsOwner(ctx.User))
                || (ctx.Guild != null && ((IGuildUser)ctx.User).GuildPermissions.Administrator);

        [NadekoCommand, Usage, Description, Aliases]
        public async Task AddCustReact(string key, [Leftover] string message)
        {
            var channel = ctx.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
                return;

            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.AddCustomReaction(ctx.Guild?.Id, key, message);

            await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle(GetText("new_cust_react"))
                .WithDescription($"#{cr.Id}")
                .AddField(efb => efb.WithName(GetText("trigger")).WithValue(key))
                .AddField(efb => efb.WithName(GetText("response")).WithValue(message.Length > 1024 ? GetText("redacted_too_long") : message))
                ).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task EditCustReact(int id, [Leftover] string message)
        {
            var channel = ctx.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || id < 0)
                return;

            if ((channel == null && !_creds.IsOwner(ctx.User)) || (channel != null && !((IGuildUser)ctx.User).GuildPermissions.Administrator))
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.EditCustomReaction(ctx.Guild?.Id, id, message).ConfigureAwait(false);
            if (cr != null)
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("edited_cust_react"))
                    .WithDescription($"#{id}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(cr.Trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(message.Length > 1024 ? GetText("redacted_too_long") : message))
                    ).ConfigureAwait(false);
            }
            else
            {
                await ReplyErrorLocalizedAsync("edit_fail").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task ListCustReact(int page = 1)
        {
            if (--page < 0 || page > 999)
                return;

            var customReactions = _service.GetCustomReactions(ctx.Guild?.Id);

            if (customReactions == null || !customReactions.Any())
            {
                await ReplyErrorLocalizedAsync("no_found").ConfigureAwait(false);
                return;
            }

            await ctx.SendPaginatedConfirmAsync(page, curPage =>
                new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("name"))
                    .WithDescription(string.Join("\n", customReactions.OrderBy(cr => cr.Trigger)
                                                    .Skip(curPage * 20)
                                                    .Take(20)
                                                    .Select(cr =>
                                                    {
                                                        var str = $"`#{cr.Id}` {cr.Trigger}";
                                                        if (cr.AutoDeleteTrigger)
                                                        {
                                                            str = "🗑" + str;
                                                        }
                                                        if (cr.DmResponse)
                                                        {
                                                            str = "📪" + str;
                                                        }
                                                        return str;
                                                    }))), customReactions.Count(), 20)
                                .ConfigureAwait(false);
        }

        public enum All
        {
            All
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task ListCustReact(All _)
        {
            var customReactions = _service.GetCustomReactions(ctx.Guild?.Id);

            if (customReactions == null || !customReactions.Any())
            {
                await ReplyErrorLocalizedAsync("no_found").ConfigureAwait(false);
                return;
            }

            using (var txtStream = await customReactions.GroupBy(cr => cr.Trigger)
                                                        .OrderBy(cr => cr.Key)
                                                        .Select(cr => new { Trigger = cr.Key, Responses = cr.Select(y => new { id = y.Id, text = y.Response }).ToList() })
                                                        .ToJson()
                                                        .ToStream()
                                                        .ConfigureAwait(false))
            {

                if (ctx.Guild == null) // its a private one, just send back
                    await ctx.Channel.SendFileAsync(txtStream, "customreactions.txt", GetText("list_all")).ConfigureAwait(false);
                else
                    await ((IGuildUser)ctx.User).SendFileAsync(txtStream, "customreactions.txt", GetText("list_all"), false).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ListCustReactG(int page = 1)
        {
            if (--page < 0 || page > 9999)
                return;
            var customReactions = _service.GetCustomReactions(ctx.Guild?.Id);

            if (customReactions == null || !customReactions.Any())
            {
                await ReplyErrorLocalizedAsync("no_found").ConfigureAwait(false);
            }
            else
            {
                var ordered = customReactions
                    .GroupBy(cr => cr.Trigger)
                    .OrderBy(cr => cr.Key)
                    .ToList();

                await ctx.SendPaginatedConfirmAsync(page, (curPage) =>
                    new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("name"))
                        .WithDescription(string.Join("\r\n", ordered
                                                         .Skip(curPage * 20)
                                                         .Take(20)
                                                         .Select(cr => $"**{cr.Key.Trim().ToLowerInvariant()}** `x{cr.Count()}`"))),
                    ordered.Count, 20).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ShowCustReact(int id)
        {
            var found = _service.GetCustomReaction(ctx.Guild?.Id, id);

            if (found == null)
            {
                await ReplyErrorLocalizedAsync("no_found_id").ConfigureAwait(false);
                return;
            }
            else
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription($"#{id}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(found.Trigger.TrimTo(1024)))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue((found.Response + "\n```css\n" + found.Response).TrimTo(1020) + "```"))
                    ).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DelCustReact(int id)
        {
            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.DeleteCustomReactionAsync(ctx.Guild?.Id, id);

            if (cr != null)
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("deleted"))
                    .WithDescription("#" + cr.Id)
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(cr.Trigger.TrimTo(1024)))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(cr.Response.TrimTo(1024)))).ConfigureAwait(false);
            }
            else
            {
                await ReplyErrorLocalizedAsync("no_found_id").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public Task CrCa(int id)
            => InternalCrEdit(id, CustomReactionsService.CrField.ContainsAnywhere);

        [NadekoCommand, Usage, Description, Aliases]
        public Task CrDm(int id)
            => InternalCrEdit(id, CustomReactionsService.CrField.DmResponse);

        [NadekoCommand, Usage, Description, Aliases]
        public Task CrAd(int id)
            => InternalCrEdit(id, CustomReactionsService.CrField.AutoDelete);

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public Task CrsReload()
        {
            _service.TriggerReloadCustomReactions();

            return ctx.Channel.SendConfirmAsync("👌");
        }

        private async Task InternalCrEdit(int id, CustomReactionsService.CrField option)
        {
            var cr = _service.GetCustomReaction(ctx.Guild?.Id, id);
            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }
            var (success, newVal) = await _service.ToggleCrOptionAsync(id, option).ConfigureAwait(false);
            if (!success)
            {
                await ReplyErrorLocalizedAsync("no_found_id").ConfigureAwait(false);
                return;
            }

            if (newVal)
            {
                await ReplyConfirmLocalizedAsync("option_enabled", Format.Code(option.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalizedAsync("option_disabled", Format.Code(option.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task CrClear()
        {
            if (await PromptUserConfirmAsync(new EmbedBuilder()
                .WithTitle("Custom reaction clear")
                .WithDescription("This will delete all custom reactions on this server.")).ConfigureAwait(false))
            {
                var count = _service.ClearCustomReactions(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync("cleared", count).ConfigureAwait(false);
            }
        }
    }
}