using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SelfCommands : NadekoSubmodule
        {
            private static volatile bool _forwardDMs;
            private static volatile bool _forwardDMsToAllOwners;

            private static readonly object _locker = new object();

            static SelfCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    _forwardDMs = config.ForwardMessages;
                    _forwardDMsToAllOwners = config.ForwardToAllOwners;
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ForwardMessages()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    lock (_locker)
                        _forwardDMs = config.ForwardMessages = !config.ForwardMessages;
                    uow.Complete();
                }
                if (_forwardDMs)
                    await ReplyConfirmLocalized("fwdm_start").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("fwdm_stop").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ForwardToAll()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    lock (_locker)
                        _forwardDMsToAllOwners = config.ForwardToAllOwners = !config.ForwardToAllOwners;
                    uow.Complete();
                }
                if (_forwardDMsToAllOwners)
                    await ReplyConfirmLocalized("fwall_start").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("fwall_stop").ConfigureAwait(false);

            }

            public static async Task HandleDmForwarding(SocketMessage msg, List<IDMChannel> ownerChannels)
            {
                if (_forwardDMs && ownerChannels.Any())
                {
                    var title =
                        GetTextStatic("dm_from", NadekoBot.Localization.DefaultCultureInfo,
                            typeof(Administration).Name.ToLowerInvariant()) + $" [{msg.Author}]({msg.Author.Id})";
                    if (_forwardDMsToAllOwners)
                    {
                        await Task.WhenAll(ownerChannels.Where(ch => ch.Recipient.Id != msg.Author.Id)
                            .Select(ch => ch.SendConfirmAsync(title, msg.Content))).ConfigureAwait(false);
                    }
                    else
                    {
                        var firstOwnerChannel = ownerChannels.First();
                        if (firstOwnerChannel.Recipient.Id != msg.Author.Id)
                            try { await firstOwnerChannel.SendConfirmAsync(title, msg.Content).ConfigureAwait(false); }
                            catch
                            {
                                // ignored
                            }
                    }
                }
            }


            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ConnectShard(int shardid)
            {
                var shard = NadekoBot.Client.GetShard(shardid);

                if (shard == null)
                {
                    await Context.Channel.SendErrorAsync("No shard by that id found.").ConfigureAwait(false);
                    return;
                }
                try
                {
                    await Context.Channel.SendConfirmAsync($"Shard **#{shardid}** reconnecting.").ConfigureAwait(false);
                    await shard.ConnectAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync($"Shard **#{shardid}** reconnected.").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Leave([Remainder] string guildStr)
            {
                guildStr = guildStr.Trim().ToUpperInvariant();
                var server = NadekoBot.Client.GetGuilds().FirstOrDefault(g => g.Id.ToString() == guildStr) ??
                    NadekoBot.Client.GetGuilds().FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == guildStr);

                if (server == null)
                {
                    await Context.Channel.SendErrorAsync("⚠️ Cannot find that server").ConfigureAwait(false);
                    return;
                }
                if (server.OwnerId != NadekoBot.Client.CurrentUser.Id)
                {
                    await server.LeaveAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync("✅ Left server " + server.Name).ConfigureAwait(false);
                }
                else
                {
                    await server.DeleteAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync("Deleted server " + server.Name).ConfigureAwait(false);
                }
            }


            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Die()
            {
                try { await Context.Channel.SendConfirmAsync("ℹ️ **Shutting down.**").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                await Task.Delay(2000).ConfigureAwait(false);
                Environment.Exit(0);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetName([Remainder] string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                    return;

                await NadekoBot.Client.CurrentUser.ModifyAsync(u => u.Username = newName).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync($"Bot name changed to **{newName}**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetStatus([Remainder] SettableUserStatus status)
            {
                await NadekoBot.Client.SetStatusAsync(SettableUserStatusToUserStatus(status)).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync($"Bot status changed to **{status}**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetAvatar([Remainder] string img = null)
            {
                if (string.IsNullOrWhiteSpace(img))
                    return;

                using (var http = new HttpClient())
                {
                    using (var sr = await http.GetStreamAsync(img))
                    {
                        var imgStream = new MemoryStream();
                        await sr.CopyToAsync(imgStream);
                        imgStream.Position = 0;

                        await NadekoBot.Client.CurrentUser.ModifyAsync(u => u.Avatar = new Image(imgStream)).ConfigureAwait(false);
                    }
                }

                await Context.Channel.SendConfirmAsync("🆒 **New avatar set.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetGame([Remainder] string game = null)
            {
                await NadekoBot.Client.SetGameAsync(game).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("👾 **New game set.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetStream(string url, [Remainder] string name = null)
            {
                name = name ?? "";

                await NadekoBot.Client.SetGameAsync(name, url, StreamType.Twitch).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("ℹ️ **New stream set.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Send(string where, [Remainder] string msg = null)
            {
                if (string.IsNullOrWhiteSpace(msg))
                    return;

                var ids = where.Split('|');
                if (ids.Length != 2)
                    return;
                var sid = ulong.Parse(ids[0]);
                var server = NadekoBot.Client.GetGuilds().FirstOrDefault(s => s.Id == sid);

                if (server == null)
                    return;

                if (ids[1].ToUpperInvariant().StartsWith("C:"))
                {
                    var cid = ulong.Parse(ids[1].Substring(2));
                    var ch = server.TextChannels.FirstOrDefault(c => c.Id == cid);
                    if (ch == null)
                    {
                        return;
                    }
                    await ch.SendMessageAsync(msg).ConfigureAwait(false);
                }
                else if (ids[1].ToUpperInvariant().StartsWith("U:"))
                {
                    var uid = ulong.Parse(ids[1].Substring(2));
                    var user = server.Users.FirstOrDefault(u => u.Id == uid);
                    if (user == null)
                    {
                        return;
                    }
                    await user.SendMessageAsync(msg).ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorAsync("⚠️ Invalid format.").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Announce([Remainder] string message)
            {
                var channels = NadekoBot.Client.GetGuilds().Select(g => g.DefaultChannel).ToArray();
                if (channels == null)
                    return;
                await Task.WhenAll(channels.Where(c => c != null).Select(c => c.SendConfirmAsync($"🆕 Message from {Context.User} `[Bot Owner]`:", message)))
                        .ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("🆗").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ReloadImages()
            {
                var time = await NadekoBot.Images.Reload().ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"Images loaded after {time.TotalSeconds:F3}s!").ConfigureAwait(false);
            }

            private static UserStatus SettableUserStatusToUserStatus(SettableUserStatus sus)
            {
                switch (sus)
                {
                    case SettableUserStatus.Online:
                        return UserStatus.Online;
                    case SettableUserStatus.Invisible:
                        return UserStatus.Invisible;
                    case SettableUserStatus.Idle:
                        return UserStatus.AFK;
                    case SettableUserStatus.Dnd:
                        return UserStatus.DoNotDisturb;
                }

                return UserStatus.Online;
            }

            public enum SettableUserStatus
            {
                Online,
                Invisible,
                Idle,
                Dnd
            }
        }
    }
}
