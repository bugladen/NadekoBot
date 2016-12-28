using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class CrossServerTextChannel
        {
            static CrossServerTextChannel()
            {
                _log = LogManager.GetCurrentClassLogger();
                NadekoBot.Client.MessageReceived += async (imsg) =>
                {
                    try
                    {
                        if (imsg.Author.IsBot)
                            return;
                        var msg = imsg as IUserMessage;
                        if (msg == null)
                            return;
                        var channel = imsg.Channel as ITextChannel;
                        if (channel == null)
                            return;
                        if (msg.Author.Id == NadekoBot.Client.GetCurrentUser().Id) return;
                        foreach (var subscriber in Subscribers)
                        {
                            var set = subscriber.Value;
                            if (!set.Contains(msg.Channel))
                                continue;
                            foreach (var chan in set.Except(new[] { channel }))
                            {
                                try { await chan.SendMessageAsync(GetText(channel.Guild, channel, (IGuildUser)msg.Author, msg)).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                            }
                        }
                    }
                    catch (Exception ex) {
                        _log.Warn(ex);
                    }
                };
            }

            private static string GetText(IGuild server, ITextChannel channel, IGuildUser user, IUserMessage message) =>
                $"**{server.Name} | {channel.Name}** `{user.Username}`: " + message.Content;
            
            public static readonly ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>> Subscribers = new ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>>();
            private static Logger _log { get; }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Scsc(IUserMessage msg)
            {
                var channel = (ITextChannel)msg.Channel;
                var token = new NadekoRandom().Next();
                var set = new ConcurrentHashSet<ITextChannel>();
                if (Subscribers.TryAdd(token, set))
                {
                    set.Add(channel);
                    await ((IGuildUser)msg.Author).SendConfirmAsync("This is your CSC token", token.ToString()).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task Jcsc(IUserMessage imsg, int token)
            {
                var channel = (ITextChannel)imsg.Channel;

                ConcurrentHashSet<ITextChannel> set;
                if (!Subscribers.TryGetValue(token, out set))
                    return;
                set.Add(channel);
                await channel.SendConfirmAsync("Joined cross server channel.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task Lcsc(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                foreach (var subscriber in Subscribers)
                {
                    subscriber.Value.TryRemove(channel);
                }
                await channel.SendMessageAsync("Left cross server channel.").ConfigureAwait(false);
            }
        }
    }
}