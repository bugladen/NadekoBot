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
        public static ConcurrentHashSet<CustomReaction> GlobalReactions { get; } = new ConcurrentHashSet<CustomReaction>();
        public static ConcurrentDictionary<ulong, ConcurrentHashSet<CustomReaction>> GuildReactions { get; } = new ConcurrentDictionary<ulong, ConcurrentHashSet<CustomReaction>>();
        static CustomReactions()
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var items = uow.CustomReactions.GetAll();
                GuildReactions = new ConcurrentDictionary<ulong, ConcurrentHashSet<CustomReaction>>(items.Where(g => g.GuildId != null).GroupBy(k => k.GuildId.Value).ToDictionary(g => g.Key, g => new ConcurrentHashSet<CustomReaction>(g)));
                GlobalReactions = new ConcurrentHashSet<CustomReaction>(items.Where(g => g.GuildId == null));
            }
        }
        public CustomReactions(ILocalization loc, CommandService cmds, ShardedDiscordClient client) : base(loc, cmds, client)
        {
            client.MessageReceived += (imsg) =>
            {
                var umsg = imsg as IUserMessage;
                if (umsg == null)
                    return Task.CompletedTask;

                var channel = umsg.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;

                var t = Task.Run(async () =>
                {
                    var content = umsg.Content.ToLowerInvariant();
                    ConcurrentHashSet<CustomReaction> reactions;
                    GuildReactions.TryGetValue(channel.Guild.Id, out reactions);
                    if (reactions != null && reactions.Any())
                    {
                        var reaction = reactions.Where(cr => cr.TriggerWithContext(umsg) == content).Shuffle().FirstOrDefault();
                        if (reaction != null)
                        {
                            try { await channel.SendMessageAsync(reaction.ResponseWithContext(umsg)).ConfigureAwait(false); } catch { }
                            return;
                        }
                    }
                    var greaction = GlobalReactions.Where(cr => cr.TriggerWithContext(umsg) == content).Shuffle().FirstOrDefault();
                    if (greaction != null)
                    {
                        try { await channel.SendMessageAsync(greaction.ResponseWithContext(umsg)).ConfigureAwait(false); } catch { }
                        return;
                    }
                });
                return Task.CompletedTask;
            };
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task AddCustReact(IUserMessage imsg, string key, [Remainder] string message)
        {
            var channel = imsg.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
                return;

            key = key.ToLowerInvariant();

            if ((channel == null && !NadekoBot.Credentials.IsOwner(imsg.Author)) || (channel != null && !((IGuildUser)imsg.Author).GuildPermissions.Administrator))
            {
                try { await imsg.Channel.SendMessageAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
                return;
            }

            var cr = new CustomReaction()
            {
                GuildId = channel?.Guild.Id,
                IsRegex = false,
                Trigger = key,
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
                var reactions = GuildReactions.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CustomReaction>());
                reactions.Add(cr);
            }

            await imsg.Channel.SendMessageAsync($"`Added new custom reaction:`\n\t`Trigger:` {key}\n\t`Response:` {message}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ListCustReact(IUserMessage imsg,int page = 1)
        {
            var channel = imsg.Channel as ITextChannel;

            if (page < 1 || page > 1000)
                return;
            ConcurrentHashSet<CustomReaction> customReactions;
            if (channel == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CustomReaction>());

            if (customReactions == null || !customReactions.Any())
                await imsg.Channel.SendMessageAsync("`No custom reactions found`").ConfigureAwait(false);
            else
                await imsg.Channel.SendMessageAsync(string.Join("\n", customReactions.OrderBy(cr => cr.Trigger).Skip((page - 1) * 10).Take(10)))
                             .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DelCustReact(IUserMessage imsg, int id)
        {
            var channel = imsg.Channel as ITextChannel;

            if ((channel == null && !NadekoBot.Credentials.IsOwner(imsg.Author)) || (channel != null && !((IGuildUser)imsg.Author).GuildPermissions.Administrator))
            {
                try { await imsg.Channel.SendMessageAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
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
                    var toRemove = GlobalReactions.FirstOrDefault(cr => cr.Id == toDelete.Id);
                    GlobalReactions.TryRemove(toRemove);
                    success = true;
                }
                else if (toDelete.GuildId != null && channel?.Guild.Id == toDelete.GuildId)
                {
                    uow.CustomReactions.Remove(toDelete);
                    var crs = GuildReactions.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CustomReaction>());
                    var toRemove = crs.FirstOrDefault(cr => cr.Id == toDelete.Id);

                    success = true;
                }
                if(success)
                    await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (success)
                await imsg.Channel.SendMessageAsync("**Successfully deleted custom reaction** " + toDelete.ToString()).ConfigureAwait(false);
            else
                await imsg.Channel.SendMessageAsync("`Failed to find that custom reaction.`").ConfigureAwait(false);
        }
    }
}
