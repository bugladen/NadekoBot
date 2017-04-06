using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.DataStructures;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class FlowerShop : NadekoSubmodule
        {
            public enum Role {
                Role
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Shop(int page = 1)
            {
                if (page <= 0)
                    return;
                page -= 1;
                List<ShopEntry> entries;
                using (var uow = DbHandler.UnitOfWork())
                {
                    entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries)).ShopEntries);
                }

                await Context.Channel.SendPaginatedConfirmAsync(page + 1, (curPage) =>
                {
                    var theseEntries = entries.Skip((curPage - 1) * 9).Take(9);

                    if (!theseEntries.Any())
                        return new EmbedBuilder().WithErrorColor()
                            .WithDescription(GetText("shop_none"));
                    var embed = new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("shop", NadekoBot.BotConfig.CurrencySign));

                    for (int i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        embed.AddField(efb => efb.WithName($"#{i + 1} - {entry.Price}{NadekoBot.BotConfig.CurrencySign}").WithValue(EntryToString(entry)).WithIsInline(true));
                    }
                    return embed;
                }, entries.Count / 9, true);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Buy(int entryNumber)
            {
                var channel = (ITextChannel)Context.Channel;

            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopAdd(ShopEntryType type, int price, string name)
            {
                var entry = new ShopEntry()
                {
                    Name = name,
                    Price = price,
                    Type = type,
                    AuthorId = Context.User.Id,
                };
                using (var uow = DbHandler.UnitOfWork())
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries)).ShopEntries);
                    entries.Add(entry);
                    uow.GuildConfigs.For(Context.Guild.Id, set => set).ShopEntries = entries;
                    uow.Complete();
                }
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("shop_item_add"))
                    .AddField(efb => efb.WithName(GetText("name")).WithValue(name).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("price")).WithValue(price.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("type")).WithValue(type.ToString()).WithIsInline(true)));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
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
                using (var uow = DbHandler.UnitOfWork())
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries)).ShopEntries);
                    entries.Add(entry);
                    uow.GuildConfigs.For(Context.Guild.Id, set => set).ShopEntries = entries;
                    uow.Complete();
                }
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("shop_item_add"))
                    .AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(entry.RoleName))).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("price")).WithValue(price.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("type")).WithValue("Role").WithIsInline(true)));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ShopRemove(int index)
            {
                if (index < 0)
                    return;
                ShopEntry removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries));
                    var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                    removed = entries.ElementAtOrDefault(index);
                    if (removed != null)
                    {
                        entries.Remove(removed);

                        config.ShopEntries = entries;
                        uow.Complete();
                    }
                }

                if(removed == null)
                    await ReplyErrorLocalized("shop_rem_fail").ConfigureAwait(false);
                else
                    await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("shop_item_add"))
                        .AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(removed.RoleName))).WithIsInline(true))
                        .AddField(efb => efb.WithName(GetText("price")).WithValue(removed.Price.ToString()).WithIsInline(true))
                        .AddField(efb => efb.WithName(GetText("type")).WithValue(removed.Type.ToString()).WithIsInline(true)));
            }

            public string EntryToString(ShopEntry entry)
            {
                if (entry.Type == ShopEntryType.Role)
                {
                    return Format.Bold(entry.Name) + "\n" + GetText("shop_role", Format.Bold(entry.RoleName));
                }
                else if (entry.Type == ShopEntryType.List)
                {

                }
                else if (entry.Type == ShopEntryType.Infinite_List)
                {

                }
                return "";
            }
        }
    }
}
