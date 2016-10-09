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
        public static ConcurrentDictionary<ulong, HashSet<CustomReaction>> GuildReactions { get; } = new ConcurrentDictionary<ulong, HashSet<CustomReaction>>();
        static CustomReactions()
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var items = uow.CustomReactions.GetAll();
                GuildReactions = new ConcurrentDictionary<ulong, HashSet<CustomReaction>>(items.Where(g => g.GuildId != null).GroupBy(k => k.GuildId.Value).ToDictionary(g => g.Key, g => new HashSet<CustomReaction>(g)));
                GlobalReactions = new HashSet<CustomReaction>(items.Where(g => g.GuildId == null));
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
                    HashSet<CustomReaction> reactions;
                    GuildReactions.TryGetValue(channel.Guild.Id, out reactions);
                    if (reactions != null && reactions.Any())
                    {
                        var reaction = reactions.Where(cr => cr.Trigger == umsg.Content).Shuffle().FirstOrDefault();
                        if (reaction != null)
                        {
                            try { await channel.SendMessageAsync(reaction.Response).ConfigureAwait(false); } catch { }
                            return;
                        }
                    }
                    var greaction = GlobalReactions.Where(cr => cr.Trigger == umsg.Content).Shuffle().FirstOrDefault();
                    if (greaction != null)
                    {
                        try { await channel.SendMessageAsync(greaction.Response).ConfigureAwait(false); } catch { }
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

            if ((channel == null && !NadekoBot.Credentials.IsOwner(imsg.Author)) || (channel != null && !((IGuildUser)imsg.Author).GuildPermissions.Administrator))
            {
                try { await imsg.Channel.SendMessageAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
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
                var reactions = GuildReactions.GetOrAdd(channel.Guild.Id, new HashSet<CustomReaction>());
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
            HashSet<CustomReaction> customReactions;
            if (channel == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(channel.Guild.Id, new HashSet<CustomReaction>());

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
                    
                    success = true;
                }
                else if (toDelete.GuildId != null && channel?.Guild.Id == toDelete.GuildId)
                {
                    uow.CustomReactions.Remove(toDelete);
                    GuildReactions.GetOrAdd(channel.Guild.Id, new HashSet<CustomReaction>()).RemoveWhere(cr => cr.Id == toDelete.Id);
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
