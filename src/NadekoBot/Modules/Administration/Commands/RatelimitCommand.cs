using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
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
        public class RatelimitCommand : NadekoSubmodule
        {
            public static ConcurrentDictionary<ulong, Ratelimiter> RatelimitingChannels = new ConcurrentDictionary<ulong, Ratelimiter>();
            private new static readonly Logger _log;

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
                    var usr = Users.GetOrAdd(id, (key) => new RatelimitedUser() { UserId = id });
                    if (usr.MessageCount == MaxMessages)
                    {
                        return true;
                    }
                    usr.MessageCount++;
                    var _ = Task.Run(async () =>
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

            static RatelimitCommand()
            {
                _log = LogManager.GetCurrentClassLogger();

                NadekoBot.Client.MessageReceived += async (umsg) =>
                 {
                     try
                     {
                         var usrMsg = umsg as IUserMessage;
                         var channel = usrMsg?.Channel as ITextChannel;

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
                    await ReplyConfirmLocalized("slowmode_disabled").ConfigureAwait(false);
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
                    await ReplyErrorLocalized("invalid_params").ConfigureAwait(false);
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
                    await Context.Channel.SendConfirmAsync(GetText("slowmode_init"),
                            GetText("slowmode_desc", Format.Bold(toAdd.MaxMessages.ToString()), Format.Bold(toAdd.PerSeconds.ToString())))
                                                .ConfigureAwait(false);
                }
            }

            //[NadekoCommand, Usage, Description, Aliases]
            //[RequireContext(ContextType.Guild)]
            //[RequireUserPermission(GuildPermission.ManageMessages)]
            //public async Task SlowmodeWhitelist(IUser user)
            //{
            //    Ratelimiter throwaway;
            //    if (RatelimitingChannels.TryRemove(Context.Channel.Id, out throwaway))
            //    {
            //        throwaway.cancelSource.Cancel();
            //        await ReplyConfirmLocalized("slowmode_disabled").ConfigureAwait(false);
            //    }
            //}

            //[NadekoCommand, Usage, Description, Aliases]
            //[RequireContext(ContextType.Guild)]
            //[RequireUserPermission(GuildPermission.ManageMessages)]
            //public async Task SlowmodeWhitelist(IRole role)
            //{
            //    using (var uow = DbHandler.UnitOfWork())
            //    {
            //        uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.SlowmodeWhitelists)).
            //    }
            //}
        }
    }
}