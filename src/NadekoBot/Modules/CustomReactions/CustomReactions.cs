using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Services;
using NadekoBot.Attributes;
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
                GuildReactions = new ConcurrentDictionary<ulong, ConcurrentHashSet<CustomReaction>>(items.Where(g => g.GuildId != null && g.GuildId != 0).GroupBy(k => k.GuildId.Value).ToDictionary(g => g.Key, g => new ConcurrentHashSet<CustomReaction>(g)));
                GlobalReactions = new ConcurrentHashSet<CustomReaction>(items.Where(g => g.GuildId == null || g.GuildId == 0));
            }
        }
        public CustomReactions() : base()
        {
        }

        public static async Task<bool> TryExecuteCustomReaction(IUserMessage umsg)
        {
            var channel = umsg.Channel as ITextChannel;
            if (channel == null)
                return false;

            var content = umsg.Content.Trim().ToLowerInvariant();
            ConcurrentHashSet<CustomReaction> reactions;
            GuildReactions.TryGetValue(channel.Guild.Id, out reactions);
            if (reactions != null && reactions.Any())
            {
                var reaction = reactions.Where(cr => {
                    var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                    var trigger = cr.TriggerWithContext(umsg).Trim().ToLowerInvariant();
                    return ((hasTarget && content.StartsWith(trigger + " ")) || content == trigger);
                }).Shuffle().FirstOrDefault();
                if (reaction != null)
                {
                    try { await channel.SendMessageAsync(reaction.ResponseWithContext(umsg)).ConfigureAwait(false); } catch { }
                    return true;
                }
            }
            var greaction = GlobalReactions.Where(cr =>
            {
                var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                var trigger = cr.TriggerWithContext(umsg).Trim().ToLowerInvariant();
                return ((hasTarget && content.StartsWith(trigger + " ")) || content == trigger);
            }).Shuffle().FirstOrDefault();

            if (greaction != null)
            {
                try { await channel.SendMessageAsync(greaction.ResponseWithContext(umsg)).ConfigureAwait(false); } catch { }
                return true;
            }
            return false;
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
                try { await imsg.Channel.SendErrorAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
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

            await imsg.Channel.EmbedAsync(new EmbedBuilder().WithColor(NadekoBot.OkColor)
                .WithTitle("New Custom Reaction")
                .WithDescription($"#{cr.Id}")
                .AddField(efb => efb.WithName("Trigger").WithValue(key))
                .AddField(efb => efb.WithName("Response").WithValue(message))
                .Build()).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task ListCustReact(IUserMessage imsg, int page = 1)
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
                await imsg.Channel.SendErrorAsync("No custom reactions found").ConfigureAwait(false);
            else
                await imsg.Channel.SendConfirmAsync(
                    $"Page {page} of custom reactions:",
                    string.Join("\n", customReactions.OrderBy(cr => cr.Trigger)
                                                     .Skip((page - 1) * 20)
                                                     .Take(20)
                                                     .Select(cr => $"`#{cr.Id}`  `Trigger:` {cr.Trigger}")))
                                 .ConfigureAwait(false);
        }

        public enum All
        {
            All
        }

        [NadekoCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task ListCustReact(IUserMessage imsg, All x)
        {
            var channel = imsg.Channel as ITextChannel;

            ConcurrentHashSet<CustomReaction> customReactions;
            if (channel == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CustomReaction>());

            if (customReactions == null || !customReactions.Any())
                await imsg.Channel.SendErrorAsync("No custom reactions found").ConfigureAwait(false);
            else
            {
                var txtStream = await customReactions.GroupBy(cr => cr.Trigger)
                                                          .OrderBy(cr => cr.Key)
                                                          .Select(cr => new { Trigger = cr.Key, Responses = cr.Select(y => y.Response).ToList() })
                                                          .ToJson()
                                                          .ToStream()
                                                          .ConfigureAwait(false);
                if (channel == null) // its a private one, just send back
                    await imsg.Channel.SendFileAsync(txtStream, "customreactions.txt", "List of all custom reactions").ConfigureAwait(false);
                else
                    await ((IGuildUser)imsg.Author).SendFileAsync(txtStream, "customreactions.txt", "List of all custom reactions").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ListCustReactG(IUserMessage imsg, int page = 1)
        {
            var channel = imsg.Channel as ITextChannel;
            if (page < 1 || page > 10000)
                return;
            ConcurrentHashSet<CustomReaction> customReactions;
            if (channel == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CustomReaction>());

            if (customReactions == null || !customReactions.Any())
                await imsg.Channel.SendErrorAsync("No custom reactions found").ConfigureAwait(false);
            else
                await imsg.Channel.SendConfirmAsync($"Page {page} of custom reactions (grouped):", 
                                    string.Join("\r\n", customReactions
                                                        .GroupBy(cr=>cr.Trigger)
                                                        .OrderBy(cr => cr.Key)
                                                        .Skip((page - 1) * 20)
                                                        .Take(20)
                                                        .Select(cr => $"**{cr.Key.Trim().ToLowerInvariant()}** `x{cr.Count()}`")))
                             .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ShowCustReact(IUserMessage imsg, int id)
        {
            var channel = imsg.Channel as ITextChannel;

            ConcurrentHashSet<CustomReaction> customReactions;
            if (channel == null)
                customReactions = GlobalReactions;
            else
                customReactions = GuildReactions.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CustomReaction>());

            var found = customReactions.FirstOrDefault(cr => cr.Id == id);

            if (found == null)
                await imsg.Channel.SendErrorAsync("No custom reaction found with that id.").ConfigureAwait(false);
            else
            {
                await imsg.Channel.EmbedAsync(new EmbedBuilder().WithColor(NadekoBot.OkColor)
                    .WithDescription($"#{id}")
                    .AddField(efb => efb.WithName("Trigger").WithValue(found.Trigger))
                    .AddField(efb => efb.WithName("Response").WithValue(found.Response + "\n```css\n" + found.Response + "```" ))
                    .Build()).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DelCustReact(IUserMessage imsg, int id)
        {
            var channel = imsg.Channel as ITextChannel;

            if ((channel == null && !NadekoBot.Credentials.IsOwner(imsg.Author)) || (channel != null && !((IGuildUser)imsg.Author).GuildPermissions.Administrator))
            {
                try { await imsg.Channel.SendErrorAsync("Insufficient permissions. Requires Bot ownership for global custom reactions, and Administrator for guild custom reactions."); } catch { }
                return;
            }

            var success = false;
            CustomReaction toDelete;
            using (var uow = DbHandler.UnitOfWork())
            {
                toDelete = uow.CustomReactions.Get(id);
                if (toDelete == null) //not found
                    return;

                if ((toDelete.GuildId == null || toDelete.GuildId == 0) && channel == null)
                {
                    uow.CustomReactions.Remove(toDelete);
                    GlobalReactions.RemoveWhere(cr => cr.Id == toDelete.Id);
                    success = true;
                }
                else if ((toDelete.GuildId != null && toDelete.GuildId != 0) && channel?.Guild.Id == toDelete.GuildId)
                {
                    uow.CustomReactions.Remove(toDelete);
                    GuildReactions.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CustomReaction>()).RemoveWhere(cr => cr.Id == toDelete.Id);
                    success = true;
                }
                if(success)
                    await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (success)
                await imsg.Channel.SendConfirmAsync("Deleted custom reaction", toDelete.ToString()).ConfigureAwait(false);
            else
                await imsg.Channel.SendErrorAsync("Failed to find that custom reaction.").ConfigureAwait(false);
        }
    }
}
