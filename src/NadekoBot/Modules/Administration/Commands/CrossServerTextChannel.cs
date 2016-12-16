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
                NadekoBot.Client.MessageReceived += (imsg) =>
                {
                    if (Context.User.IsBot)
                        return Task.CompletedTask;

                    var msg = imsg as IUserMessage;
                    if (msg == null)
                        return Task.CompletedTask;

                    //var channel = Context.Channel as ITextChannel;
                    if (channel == null)
                        return Task.CompletedTask;

                    Task.Run(async () =>
                    {
                        if (Context.User.Id == NadekoBot.Client.CurrentUser().Id) return;
                        foreach (var subscriber in Subscribers)
                        {
                            var set = subscriber.Value;
                            if (!set.Contains(Context.Channel))
                                continue;
                            foreach (var chan in set.Except(new[] { channel }))
                            {
                                try { await chan.SendMessageAsync(GetText(Context.Guild, channel, (IGuildUser)Context.User, msg)).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                            }
                        }
                    });
                    return Task.CompletedTask;
                };
            }

            private static string GetText(IGuild server, ITextChannel channel, IGuildUser user, IUserMessage message) =>
                $"**{server.Name} | {channel.Name}** `{user.Username}`: " + message.Content;
            
            public static readonly ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>> Subscribers = new ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>>();
            private static Logger _log { get; }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Scsc()
            {
                ////var channel = (ITextChannel)Context.Channel;
                var token = new NadekoRandom().Next();
                var set = new ConcurrentHashSet<ITextChannel>();
                if (Subscribers.TryAdd(token, set))
                {
                    set.Add(channel);
                    await ((IGuildUser)Context.User).SendConfirmAsync("This is your CSC token", token.ToString()).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Jcsc(IUserMessage imsg, int token)
            {
                ////var channel = (ITextChannel)Context.Channel;

                ConcurrentHashSet<ITextChannel> set;
                if (!Subscribers.TryGetValue(token, out set))
                    return;
                set.Add(channel);
                await Context.Channel.SendConfirmAsync("Joined cross server channel.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Lcsc()
            {
                ////var channel = (ITextChannel)Context.Channel;

                foreach (var subscriber in Subscribers)
                {
                    subscriber.Value.TryRemove(channel);
                }
                await Context.Channel.SendMessageAsync("Left cross server channel.").ConfigureAwait(false);
            }
        }
    }
}