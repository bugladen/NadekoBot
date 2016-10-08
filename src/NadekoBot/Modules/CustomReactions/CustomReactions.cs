using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Services;
using NadekoBot.Attributes;
using NadekoBot.Services.Database;
using System.Collections.Concurrent;
using NadekoBot.Services.Database.Models;
using Discord;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.CustomReactions
{
    [NadekoModule("CustomReactions",".")]
    public class CustomReactions : DiscordModule
    {
        public static HashSet<CustomReaction> GlobalReactions { get; } = new HashSet<CustomReaction>();
        public static ConcurrentDictionary<ulong, HashSet<CustomReaction>> AllReactions { get; } = new ConcurrentDictionary<ulong, HashSet<CustomReaction>>();
        static CustomReactions()
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var items = uow.CustomReactions.GetAll();
                AllReactions = new ConcurrentDictionary<ulong, HashSet<CustomReaction>>(items.Where(g => g.GuildId != null).GroupBy(k => k.GuildId.Value).ToDictionary(g => g.Key, g => new HashSet<CustomReaction>(g)));
                GlobalReactions = new HashSet<CustomReaction>(items.Where(g => g.GuildId == null));
            }
        }
        public CustomReactions(ILocalization loc, CommandService cmds, ShardedDiscordClient client) : base(loc, cmds, client)
        {
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequirePermission(GuildPermission.Administrator)]
        public async Task AddCustReact(IUserMessage imsg, string key, [Remainder] string message)
        {
            var channel = imsg.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
                return;

            if ((channel == null && NadekoBot.Credentials.IsOwner(imsg.Author)) || (channel != null && ((IGuildUser)imsg.Author).GuildPermissions.Administrator))
            {
                try { await channel.SendMessageAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
                return;
            }

            var cr = new CustomReaction()
            {
                GuildId = channel?.Guild.Id,
                IsRegex = false,
                Trigger = key.ToLowerInvariant(),
                Response = message,
            };

            using (var uow = DbHandler.UnitOfWork())
            {
                uow.CustomReactions.Add(cr);

                await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (channel == null)
            {
                GlobalReactions.Add(cr);
            }
            else
            {
                var reactions = AllReactions.GetOrAdd(channel.Guild.Id, new HashSet<CustomReaction>());
                reactions.Add(cr);
            }

            await channel.SendMessageAsync($"`Added new custom reaction:`\n\t`Trigger:` {key}\n\t`Response:` {message}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ListCustReact(IUserMessage imsg,int page = 1)
        {
            var channel = imsg.Channel as ITextChannel;

            if (page < 1 || page > 1000)
                return;
            HashSet<CustomReaction> customReactions;
            if (channel == null)
                customReactions = GlobalReactions;
            else
                customReactions = AllReactions.GetOrAdd(channel.Guild.Id, new HashSet<CustomReaction>());

            if (customReactions == null || !customReactions.Any())
                await channel.SendMessageAsync("`No custom reactions found`").ConfigureAwait(false);
            else
                await channel.SendTableAsync(customReactions.OrderBy(cr => cr.Trigger).Skip((page - 1) * 10).Take(10), c => c.ToString())
                             .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DelCustReact(IUserMessage imsg, int id)
        {
            var channel = imsg.Channel as ITextChannel;

            if ((channel == null && NadekoBot.Credentials.IsOwner(imsg.Author)) || (channel != null && ((IGuildUser)imsg.Author).GuildPermissions.Administrator))
            {
                try { await channel.SendMessageAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
                return;
            }

            var success = false;
            CustomReaction toDelete;
            using (var uow = DbHandler.UnitOfWork())
            {
                toDelete = uow.CustomReactions.Get(id);
                if (toDelete == null) //not found
                    return;

                if (toDelete.GuildId == null && channel == null)
                {
                    uow.CustomReactions.Remove(toDelete);
                    success = true;
                }
                else if (toDelete.GuildId != null && channel.Guild.Id == toDelete.GuildId)
                {
                    uow.CustomReactions.Remove(toDelete);
                    success = true;
                }
                if(success)
                    await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (success)
                await channel.SendMessageAsync("**Successfully deleted custom reaction** " + toDelete.ToString()).ConfigureAwait(false);
            else
                await channel.SendMessageAsync("Failed to find that custom reaction.").ConfigureAwait(false);
        }
    }
}
