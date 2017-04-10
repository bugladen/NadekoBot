using Discord;
using Discord.Commands;
using Discord.Net;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RepeatCommands : NadekoSubmodule
        {
            //guildid/RepeatRunners
            public static ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>> Repeaters { get; set; }

            private static bool _ready;

            public class RepeatRunner
            {
                private readonly Logger _log;

                private CancellationTokenSource source { get; set; }
                private CancellationToken token { get; set; }
                public Repeater Repeater { get; }
                public SocketGuild Guild { get; }
                public ITextChannel Channel { get; private set; }
                private IUserMessage oldMsg = null;

                public RepeatRunner(Repeater repeater, ITextChannel channel = null)
                {
                    _log = LogManager.GetCurrentClassLogger();
                    Repeater = repeater;
                    Channel = channel;

                    Guild = NadekoBot.Client.GetGuild(repeater.GuildId);
                    if(Guild!=null)
                        Task.Run(Run);
                }

                private async Task Run()
                {
                    source = new CancellationTokenSource();
                    token = source.Token;
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            await Task.Delay(Repeater.Interval, token).ConfigureAwait(false);

                            await Trigger().ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                public async Task Trigger()
                {
                    var toSend = "🔄 " + Repeater.Message;
                    //var lastMsgInChannel = (await Channel.GetMessagesAsync(2)).FirstOrDefault();
                    // if (lastMsgInChannel.Id == oldMsg?.Id) //don't send if it's the same message in the channel
                    //     continue;

                    if (oldMsg != null)
                        try
                        {
                            await oldMsg.DeleteAsync();
                        }
                        catch
                        {
                            // ignored
                        }
                    try
                    {
                        if (Channel == null)
                            Channel = Guild.GetTextChannel(Repeater.ChannelId);

                        if (Channel != null)
                            oldMsg = await Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                    }
                    catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _log.Warn("Missing permissions. Repeater stopped. ChannelId : {0}", Channel?.Id);
                        return;
                    }
                    catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _log.Warn("Channel not found. Repeater stopped. ChannelId : {0}", Channel?.Id);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }

                public void Reset()
                {
                    source.Cancel();
                    var _ = Task.Run(Run);
                }

                public void Stop()
                {
                    source.Cancel();
                }

                public override string ToString() => 
                    $"{Channel?.Mention ?? $"⚠<#{Repeater.ChannelId}>" } " +
                    $"| {(int) Repeater.Interval.TotalHours}:{Repeater.Interval:mm} " +
                    $"| {Repeater.Message.TrimTo(33)}";
            }

            static RepeatCommands()
            {
                var _ = Task.Run(async () =>
                {
#if !GLOBAL_NADEKO
                    await Task.Delay(5000).ConfigureAwait(false);
#else
                    await Task.Delay(30000).ConfigureAwait(false);
#endif
                    //todo this is pretty terrible
                    Repeaters = new ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>>(NadekoBot.AllGuildConfigs
                        .ToDictionary(gc => gc.GuildId,
                            gc => new ConcurrentQueue<RepeatRunner>(gc.GuildRepeaters
                                .Select(gr => new RepeatRunner(gr))
                                .Where(x => x.Guild != null))));
                    _ready = true;
                });
            }

            public static void Unload()
            {
                _ready = false;
                foreach (var kvp in Repeaters)
                {
                    RepeatRunner r;
                    while (kvp.Value.TryDequeue(out r))
                    {
                        r.Stop();
                    }
                }

                Repeaters.Clear();
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatInvoke(int index)
            {
                if (!_ready)
                    return;
                index -= 1;
                ConcurrentQueue<RepeatRunner> rep;
                if (!Repeaters.TryGetValue(Context.Guild.Id, out rep))
                {
                    await ReplyErrorLocalized("repeat_invoke_none").ConfigureAwait(false);
                    return;
                }

                var repList = rep.ToList();

                if (index >= repList.Count)
                {
                    await ReplyErrorLocalized("index_out_of_range").ConfigureAwait(false);
                    return;
                }
                var repeater = repList[index].Repeater;
                repList[index].Reset();
                await repList[index].Trigger();

                await Context.Channel.SendMessageAsync("🔄").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task RepeatRemove(int index)
            {
                if (!_ready)
                    return;
                if (index < 1)
                    return;
                index -= 1;

                ConcurrentQueue<RepeatRunner> rep;
                if (!Repeaters.TryGetValue(Context.Guild.Id, out rep))
                    return;

                var repeaterList = rep.ToList();

                if (index >= repeaterList.Count)
                {
                    await ReplyErrorLocalized("index_out_of_range").ConfigureAwait(false);
                    return;
                }

                var repeater = repeaterList[index];
                repeater.Stop();
                repeaterList.RemoveAt(index);

                using (var uow = DbHandler.UnitOfWork())
                {
                    var guildConfig = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.GuildRepeaters));

                    guildConfig.GuildRepeaters.RemoveWhere(r => r.Id == repeater.Repeater.Id);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (Repeaters.TryUpdate(Context.Guild.Id, new ConcurrentQueue<RepeatRunner>(repeaterList), rep))
                    await Context.Channel.SendConfirmAsync(GetText("message_repeater"),
                        GetText("repeater_stopped", index + 1) + $"\n\n{repeater}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(1)]
            public async Task Repeat(int minutes, [Remainder] string message)
            {
                if (!_ready)
                    return;
                if (minutes < 1 || minutes > 10080)
                    return;

                if (string.IsNullOrWhiteSpace(message))
                    return;

                var toAdd = new GuildRepeater()
                {
                    ChannelId = Context.Channel.Id,
                    GuildId = Context.Guild.Id,
                    Interval = TimeSpan.FromMinutes(minutes),
                    Message = message
                };

                using (var uow = DbHandler.UnitOfWork())
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.GuildRepeaters));

                    if (gc.GuildRepeaters.Count >= 5)
                        return;
                    gc.GuildRepeaters.Add(toAdd);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                var rep = new RepeatRunner(toAdd, (ITextChannel) Context.Channel);

                Repeaters.AddOrUpdate(Context.Guild.Id, new ConcurrentQueue<RepeatRunner>(new[] {rep}), (key, old) =>
                {
                    old.Enqueue(rep);
                    return old;
                });

                await Context.Channel.SendConfirmAsync(
                    "🔁 " + GetText("repeater",
                        Format.Bold(((IGuildUser)Context.User).GuildPermissions.MentionEveryone ? rep.Repeater.Message : rep.Repeater.Message.SanitizeMentions()),
                        Format.Bold(rep.Repeater.Interval.Days.ToString()),
                        Format.Bold(rep.Repeater.Interval.Hours.ToString()),
                        Format.Bold(rep.Repeater.Interval.Minutes.ToString()))).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatList()
            {
                if (!_ready)
                    return;
                ConcurrentQueue<RepeatRunner> repRunners;
                if (!Repeaters.TryGetValue(Context.Guild.Id, out repRunners))
                {
                    await ReplyConfirmLocalized("repeaters_none").ConfigureAwait(false);
                    return;
                }

                var replist = repRunners.ToList();
                var sb = new StringBuilder();

                for (var i = 0; i < replist.Count; i++)
                {
                    var rep = replist[i];

                    sb.AppendLine($"`{i + 1}.` {rep}");
                }
                var desc = sb.ToString();

                if (string.IsNullOrWhiteSpace(desc))
                    desc = GetText("no_active_repeaters");

                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("list_of_repeaters"))
                        .WithDescription(desc))
                    .ConfigureAwait(false);
            }
        }
    }
}