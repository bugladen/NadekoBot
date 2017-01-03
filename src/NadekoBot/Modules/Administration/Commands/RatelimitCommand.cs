using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class RatelimitCommand : ModuleBase
        {
            public static ConcurrentDictionary<ulong, Ratelimiter> RatelimitingChannels = new ConcurrentDictionary<ulong, Ratelimiter>();
            private static Logger _log { get; }

            public class Ratelimiter
            {
                public class RatelimitedUser
                {
                    public ulong UserId { get; set; }
                    public int MessageCount { get; set; } = 0;
                }

                public ulong ChannelId { get; set; }

                public int MaxMessages { get; set; }
                public int PerSeconds { get; set; }

                public CancellationTokenSource cancelSource { get; set; } = new CancellationTokenSource();

                public ConcurrentDictionary<ulong, RatelimitedUser> Users { get; set; } = new ConcurrentDictionary<ulong, RatelimitedUser>();

                public bool CheckUserRatelimit(ulong id)
                {
                    RatelimitedUser usr = Users.GetOrAdd(id, (key) => new RatelimitedUser() { UserId = id });
                    if (usr.MessageCount == MaxMessages)
                    {
                        return true;
                    }
                    else
                    {
                        usr.MessageCount++;
                        var t = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(PerSeconds * 1000, cancelSource.Token);
                            }
                            catch (OperationCanceledException) { }
                            usr.MessageCount--;
                        });
                        return false;
                    }

                }
            }

            static RatelimitCommand()
            {
                _log = LogManager.GetCurrentClassLogger();

                NadekoBot.Client.MessageReceived += async (umsg) =>
                 {
                     try
                     {
                         var usrMsg = umsg as IUserMessage;
                         if (usrMsg == null)
                             return;
                         var channel = usrMsg.Channel as ITextChannel;

                         if (channel == null || usrMsg.IsAuthor())
                             return;
                         Ratelimiter limiter;
                         if (!RatelimitingChannels.TryGetValue(channel.Id, out limiter))
                             return;

                         if (limiter.CheckUserRatelimit(usrMsg.Author.Id))
                             await usrMsg.DeleteAsync();
                     }
                     catch (Exception ex) { _log.Warn(ex); }
                 };
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Slowmode()
            {
                Ratelimiter throwaway;
                if (RatelimitingChannels.TryRemove(Context.Channel.Id, out throwaway))
                {
                    throwaway.cancelSource.Cancel();
                    await Context.Channel.SendConfirmAsync("ℹ️ Slow mode disabled.").ConfigureAwait(false);
                    return;
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Slowmode(int msg, int perSec)
            {
                await Slowmode().ConfigureAwait(false); // disable if exists
                
                if (msg < 1 || perSec < 1 || msg > 100 || perSec > 3600)
                {
                    await Context.Channel.SendErrorAsync("⚠️ Invalid parameters.");
                    return;
                }
                var toAdd = new Ratelimiter()
                {
                    ChannelId = Context.Channel.Id,
                    MaxMessages = msg,
                    PerSeconds = perSec,
                };
                if(RatelimitingChannels.TryAdd(Context.Channel.Id, toAdd))
                {
                    await Context.Channel.SendConfirmAsync("Slow mode initiated",
                                                $"Users can't send more than `{toAdd.MaxMessages} message(s)` every `{toAdd.PerSeconds} second(s)`.")
                                                .ConfigureAwait(false);
                }
            }
        }
    }
}