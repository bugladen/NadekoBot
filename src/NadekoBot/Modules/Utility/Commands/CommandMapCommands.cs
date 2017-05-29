using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Utility;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class CommandMapCommands : NadekoSubmodule
        {
            private readonly UtilityService _service;
            private readonly DbService _db;
            private readonly DiscordShardedClient _client;

            public CommandMapCommands(UtilityService service, DbService db, DiscordShardedClient client)
            {
                _service = service;
                _db = db;
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            public async Task Alias(string trigger, [Remainder] string mapping = null)
            {
                var channel = (ITextChannel)Context.Channel;

                if (string.IsNullOrWhiteSpace(trigger))
                    return;

                trigger = trigger.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(mapping))
                {
                    ConcurrentDictionary<string, string> maps;
                    string throwaway;
                    if (!_service.AliasMaps.TryGetValue(Context.Guild.Id, out maps) ||
                        !maps.TryRemove(trigger, out throwaway))
                    {
                        await ReplyErrorLocalized("alias_remove_fail", Format.Code(trigger)).ConfigureAwait(false);
                        return;
                    }

                    using (var uow = _db.UnitOfWork)
                    {
                        var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.CommandAliases));
                        var toAdd = new CommandAlias()
                        {
                            Mapping = mapping,
                            Trigger = trigger
                        };
                        config.CommandAliases.RemoveWhere(x => x.Trigger == trigger);
                        uow.Complete();
                    }

                    await ReplyConfirmLocalized("alias_removed", Format.Code(trigger)).ConfigureAwait(false);
                    return;
                }
                _service.AliasMaps.AddOrUpdate(Context.Guild.Id, (_) =>
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.CommandAliases));
                        config.CommandAliases.Add(new CommandAlias()
                        {
                            Mapping = mapping,
                            Trigger = trigger
                        });
                        uow.Complete();
                    }
                    return new ConcurrentDictionary<string, string>(new Dictionary<string, string>() {
                        {trigger.Trim().ToLowerInvariant(), mapping.ToLowerInvariant() },
                    });
                }, (_, map) =>
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        var config = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.CommandAliases));
                        var toAdd = new CommandAlias()
                        {
                            Mapping = mapping,
                            Trigger = trigger
                        };
                        config.CommandAliases.RemoveWhere(x => x.Trigger == trigger);
                        config.CommandAliases.Add(toAdd);
                        uow.Complete();
                    }
                    map.AddOrUpdate(trigger, mapping, (key, old) => mapping);
                    return map;
                });

                await ReplyConfirmLocalized("alias_added", Format.Code(trigger), Format.Code(mapping)).ConfigureAwait(false);
            }


            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AliasList(int page = 1)
            {
                var channel = (ITextChannel)Context.Channel;
                page -= 1;

                if (page < 0)
                    return;

                ConcurrentDictionary<string, string> maps;
                if (!_service.AliasMaps.TryGetValue(Context.Guild.Id, out maps) || !maps.Any())
                {
                    await ReplyErrorLocalized("aliases_none").ConfigureAwait(false);
                    return;
                }

                var arr = maps.ToArray();

                await Context.Channel.SendPaginatedConfirmAsync(_client, page + 1, (curPage) =>
                {
                    return new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("alias_list"))
                    .WithDescription(string.Join("\n",
                        arr.Skip((curPage - 1) * 10).Take(10).Select(x => $"`{x.Key}` => `{x.Value}`")));

                }, arr.Length / 10).ConfigureAwait(false);
            }
        }
    }
}