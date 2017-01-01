using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        class SelfCommands : ModuleBase
        {
            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Leave([Remainder] string guildStr)
            {
                guildStr = guildStr.Trim().ToUpperInvariant();
                var server = NadekoBot.Client.GetGuilds().FirstOrDefault(g => g.Id.ToString().Trim().ToUpperInvariant() == guildStr) ??
                    NadekoBot.Client.GetGuilds().FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == guildStr);

                if (server == null)
                {
                    await Context.Channel.SendErrorAsync("⚠️ Cannot find that server").ConfigureAwait(false);
                    return;
                }
                if (server.OwnerId != NadekoBot.Client.CurrentUser().Id)
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

                await NadekoBot.Client.CurrentUser().ModifyAsync(u => u.Username = newName).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync($"ℹ️ Successfully changed name to **{newName}**").ConfigureAwait(false);
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

                        await NadekoBot.Client.CurrentUser().ModifyAsync(u => u.Avatar = new Image(imgStream)).ConfigureAwait(false);
                    }
                }

                await Context.Channel.SendConfirmAsync("🆒 **New avatar set.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetGame([Remainder] string game = null)
            {
                game = game ?? "";

                await NadekoBot.Client.SetGame(game).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("👾 **New game set.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetStream(string url, [Remainder] string name = null)
            {
                name = name ?? "";

                await NadekoBot.Client.SetStream(name, url).ConfigureAwait(false);

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
                var server = NadekoBot.Client.GetGuilds().Where(s => s.Id == sid).FirstOrDefault();

                if (server == null)
                    return;

                if (ids[1].ToUpperInvariant().StartsWith("C:"))
                {
                    var cid = ulong.Parse(ids[1].Substring(2));
                    var ch = (await server.GetTextChannelsAsync()).Where(c => c.Id == cid).FirstOrDefault();
                    if (ch == null)
                    {
                        return;
                    }
                    await ch.SendMessageAsync(msg).ConfigureAwait(false);
                }
                else if (ids[1].ToUpperInvariant().StartsWith("U:"))
                {
                    var uid = ulong.Parse(ids[1].Substring(2));
                    var user = server.Users.Where(u => u.Id == uid).FirstOrDefault();
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
                var channels = await Task.WhenAll(NadekoBot.Client.GetGuilds().Select(g =>
                    g.GetDefaultChannelAsync()
                )).ConfigureAwait(false);
                if (channels == null)
                    return;
                await Task.WhenAll(channels.Where(c => c != null).Select(c => c.SendConfirmAsync($"🆕 Message from {Context.User} `[Bot Owner]`:", message)))
                        .ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("🆗").ConfigureAwait(false);
            }
        }
    }
}
