using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class RatelimitCommands : NadekoSubmodule
        {
            public static ConcurrentDictionary<ulong, Ratelimiter> RatelimitingChannels = new ConcurrentDictionary<ulong, Ratelimiter>();
            public static ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>();
            public static ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>();

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

                public bool CheckUserRatelimit(ulong id, ulong guildId, SocketGuildUser optUser)
                {
                    HashSet<ulong> ignoreUsers;
                    HashSet<ulong> ignoreRoles;

                    if ((IgnoredUsers.TryGetValue(guildId, out ignoreUsers) && ignoreUsers.Contains(id)) || 
                        (optUser != null && IgnoredRoles.TryGetValue(guildId, out ignoreRoles) && optUser.RoleIds.Any(x => ignoreRoles.Contains(x))))
                        return false;

                    var usr = Users.GetOrAdd(id, (key) => new RatelimitedUser() { UserId = id });
                    if (usr.MessageCount >= MaxMessages)
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

            static RatelimitCommands()
            {
                _log = LogManager.GetCurrentClassLogger();

                IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                    NadekoBot.AllGuildConfigs
                             .ToDictionary(x => x.GuildId,
                                           x => new HashSet<ulong>(x.SlowmodeIgnoredRoles.Select(y => y.RoleId))));

                IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                    NadekoBot.AllGuildConfigs
                             .ToDictionary(x => x.GuildId,
                                           x => new HashSet<ulong>(x.SlowmodeIgnoredUsers.Select(y => y.UserId))));

                NadekoBot.Client.MessageReceived += async (umsg) =>
                 {
                     try
                     {
                         var usrMsg = umsg as SocketUserMessage;
                         var channel = usrMsg?.Channel as SocketTextChannel;

                         if (channel == null || usrMsg.IsAuthor())
                             return;
                         Ratelimiter limiter;
                         if (!RatelimitingChannels.TryGetValue(channel.Id, out limiter))
                             return;

                         if (limiter.CheckUserRatelimit(usrMsg.Author.Id, channel.Guild.Id, usrMsg.Author as SocketGuildUser))
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(1)]
            public async Task SlowmodeWhitelist(IUser user)
            {
                var siu = new SlowmodeIgnoredUser
                {
                    UserId = user.Id
                };

                HashSet<SlowmodeIgnoredUser> usrs;
                bool removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    usrs = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.SlowmodeIgnoredUsers))
                        .SlowmodeIgnoredUsers;

                    if (!(removed = usrs.Remove(siu)))
                        usrs.Add(siu);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                IgnoredUsers.AddOrUpdate(Context.Guild.Id, new HashSet<ulong>(usrs.Select(x => x.UserId)), (key, old) => new HashSet<ulong>(usrs.Select(x => x.UserId)));

                if(removed)
                    await ReplyConfirmLocalized("slowmodewl_user_stop", Format.Bold(user.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("slowmodewl_user_start", Format.Bold(user.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task SlowmodeWhitelist(IRole role)
            {
                var sir = new SlowmodeIgnoredRole
                {
                    RoleId = role.Id
                };

                HashSet<SlowmodeIgnoredRole> roles;
                bool removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    roles = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.SlowmodeIgnoredRoles))
                        .SlowmodeIgnoredRoles;

                    if (!(removed = roles.Remove(sir)))
                        roles.Add(sir);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                IgnoredRoles.AddOrUpdate(Context.Guild.Id, new HashSet<ulong>(roles.Select(x => x.RoleId)), (key, old) => new HashSet<ulong>(roles.Select(x => x.RoleId)));

                if (removed)
                    await ReplyConfirmLocalized("slowmodewl_role_stop", Format.Bold(role.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("slowmodewl_role_start", Format.Bold(role.ToString())).ConfigureAwait(false);
            }
        }
    }
}