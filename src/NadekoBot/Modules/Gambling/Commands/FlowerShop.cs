using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.DataStructures;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class FlowerShop : NadekoSubmodule
        {
            private readonly BotConfig _bc;
            private readonly DbHandler _db;
            private readonly CurrencyHandler _ch;
            private readonly DiscordShardedClient _client;

            public enum Role
            {
                Role
            }

            public enum List
            {
                List
            }

            public FlowerShop(BotConfig bc, DbHandler db, CurrencyHandler ch, DiscordShardedClient client)
            {
                _db = db;
                _bc = bc;
                _ch = ch;
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Shop(int page = 1)
            {
                if (page <= 0)
                    return;
                page -= 1;
                List<ShopEntry> entries;
                using (var uow = _db.UnitOfWork)
                {
                    entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id, 
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries);
                }

                await Context.Channel.SendPaginatedConfirmAsync(_client, page + 1, (curPage) =>
                {
                    var theseEntries = entries.Skip((curPage - 1) * 9).Take(9);

                    if (!theseEntries.Any())
                        return new EmbedBuilder().WithErrorColor()
                            .WithDescription(GetText("shop_none"));
                    var embed = new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("shop", _bc.CurrencySign));

                    for (int i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        embed.AddField(efb => efb.WithName($"#{i + 1} - {entry.Price}{_bc.CurrencySign}").WithValue(EntryToString(entry)).WithIsInline(true));
                    }
                    return embed;
                }, entries.Count / 9, true);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Buy(int index, [Remainder]string message = null)
            {
                index -= 1;
                if (index < 0)
                    return;
                ShopEntry entry;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries)
                        .ThenInclude(x => x.Items));
                    var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                    entry = entries.ElementAtOrDefault(index);
                    uow.Complete();
                }

                if (entry == null)
                {
                    await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
                    return;
                }

                if (entry.Type == ShopEntryType.Role)
                {
                    var guser = (IGuildUser)Context.User;
                    var role = Context.Guild.GetRole(entry.RoleId);

                    if (role == null)
                    {
                        await ReplyErrorLocalized("shop_role_not_found").ConfigureAwait(false);
                        return;
                    }

                    if (await _ch.RemoveCurrencyAsync(Context.User.Id, $"Shop purchase - {entry.Type}", entry.Price))
                    {
                        try
                        {
                            await guser.AddRoleAsync(role).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _log.Warn(ex);
                            await _ch.AddCurrencyAsync(Context.User.Id, $"Shop error refund", entry.Price);
                            await ReplyErrorLocalized("shop_role_purchase_error").ConfigureAwait(false);
                            return;
                        }
                        await _ch.AddCurrencyAsync(entry.AuthorId, $"Shop sell item - {entry.Type}", GetProfitAmount(entry.Price));
                        await ReplyConfirmLocalized("shop_role_purchase", Format.Bold(role.Name)).ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        await ReplyErrorLocalized("not_enough", _bc.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                }
                else if (entry.Type == ShopEntryType.List)
                {
                    if (entry.Items.Count == 0)
                    {
                        await ReplyErrorLocalized("out_of_stock").ConfigureAwait(false);
                        return;
                    }

                    var item = entry.Items.ToArray()[new NadekoRandom().Next(0, entry.Items.Count)];

                    if (await _ch.RemoveCurrencyAsync(Context.User.Id, $"Shop purchase - {entry.Type}", entry.Price))
                    {
                        int removed;
                        using (var uow = _db.UnitOfWork)
                        {
                            var x = uow._context.Set<ShopEntryItem>().Remove(item);

                            removed = uow.Complete();
                        }
                        try
                        {
                            await (await Context.User.CreateDMChannelAsync())
                                .EmbedAsync(new EmbedBuilder().WithOkColor()
                                .WithTitle(GetText("shop_purchase", Context.Guild.Name))
                                .AddField(efb => efb.WithName(GetText("item")).WithValue(item.Text).WithIsInline(false))
                                .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                                .AddField(efb => efb.WithName(GetText("name")).WithValue(entry.Name).WithIsInline(true)))
                                .ConfigureAwait(false);

                            await _ch.AddCurrencyAsync(entry.AuthorId,
                                    $"Shop sell item - {entry.Name}",
                                    GetProfitAmount(entry.Price)).ConfigureAwait(false);
                        }
                        catch
                        {
                            using (var uow = _db.UnitOfWork)
                            {
                                uow._context.Set<ShopEntryItem>().Add(item);
                                uow.Complete();

                                await _ch.AddCurrencyAsync(Context.User.Id, 
                                    $"Shop error refund - {entry.Name}", 
                                    entry.Price, 
                                    uow).ConfigureAwait(false);
                            }
                            await ReplyErrorLocalized("shop_buy_error").ConfigureAwait(false);
                            return;
                        }
                        await ReplyConfirmLocalized("shop_item_purchase").ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyErrorLocalized("not_enough", _bc.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                }

            }

            private long GetProfitAmount(int price) => 
                (int)(Math.Ceiling(0.90 * price));

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task ShopAdd(Role _, int price, [Remainder] IRole role)
            {
                var entry = new ShopEntry()
                {
                    Name = "-",
                    Price = price,
                    Type = ShopEntryType.Role,
                    AuthorId = Context.User.Id,
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                using (var uow = _db.UnitOfWork)
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries)
                    {
                        entry
                    };
                    uow.GuildConfigs.For(Context.Guild.Id, set => set).ShopEntries = entries;
                    uow.Complete();
                }
                await Context.Channel.EmbedAsync(EntryToEmbed(entry)
                    .WithTitle(GetText("shop_item_add")));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopAdd(List _, int price, [Remainder]string name)
            {
                var entry = new ShopEntry()
                {
                    Name = name.TrimTo(100),
                    Price = price,
                    Type = ShopEntryType.List,
                    AuthorId = Context.User.Id,
                    Items = new HashSet<ShopEntryItem>(),
                };
                using (var uow = _db.UnitOfWork)
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries)
                    {
                        entry
                    };
                    uow.GuildConfigs.For(Context.Guild.Id, set => set).ShopEntries = entries;
                    uow.Complete();
                }
                await Context.Channel.EmbedAsync(EntryToEmbed(entry)
                    .WithTitle(GetText("shop_item_add")));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopListAdd(int index, [Remainder] string itemText)
            {
                index -= 1;
                if (index < 0)
                    return;
                var item = new ShopEntryItem()
                {
                    Text = itemText
                };
                ShopEntry entry;
                bool rightType = false;
                bool added = false;
                using (var uow = _db.UnitOfWork)
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries);
                    entry = entries.ElementAtOrDefault(index);
                    if (entry != null && (rightType = (entry.Type == ShopEntryType.List)))
                    {
                        if (added = entry.Items.Add(item))
                        {
                            uow.Complete();
                        }
                    }
                }
                if (entry == null)
                    await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
                else if (!rightType)
                    await ReplyErrorLocalized("shop_item_wrong_type").ConfigureAwait(false);
                else if (added == false)
                    await ReplyErrorLocalized("shop_list_item_not_unique").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("shop_list_item_added").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopRemove(int index)
            {
                index -= 1;
                if (index < 0)
                    return;
                ShopEntry removed;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries)
                        .ThenInclude(x => x.Items));

                    var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                    removed = entries.ElementAtOrDefault(index);
                    if (removed != null)
                    {
                        entries.Remove(removed);

                        config.ShopEntries = entries;
                        uow.Complete();
                    }
                }

                if (removed == null)
                    await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
                else
                    await Context.Channel.EmbedAsync(EntryToEmbed(removed)
                        .WithTitle(GetText("shop_item_rm")));
            }

            public EmbedBuilder EntryToEmbed(ShopEntry entry)
            {
                var embed = new EmbedBuilder().WithOkColor();

                if (entry.Type == ShopEntryType.Role)
                    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(entry.RoleName))).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("type")).WithValue(entry.Type.ToString()).WithIsInline(true));
                else if (entry.Type == ShopEntryType.List)
                    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(entry.Name).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("type")).WithValue(GetText("random_unique_item")).WithIsInline(true));
                //else if (entry.Type == ShopEntryType.Infinite_List)
                //    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(entry.RoleName))).WithIsInline(true))
                //            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                //            .AddField(efb => efb.WithName(GetText("type")).WithValue(entry.Type.ToString()).WithIsInline(true));
                else return null;
            }

            public string EntryToString(ShopEntry entry)
            {
                if (entry.Type == ShopEntryType.Role)
                {
                    return GetText("shop_role", Format.Bold(entry.RoleName));
                }
                else if (entry.Type == ShopEntryType.List)
                {
                    return GetText("unique_items_left", entry.Items.Count) + "\n" + entry.Name;
                }
                //else if (entry.Type == ShopEntryType.Infinite_List)
                //{

                //}
                return "";
            }
        }
    }
}