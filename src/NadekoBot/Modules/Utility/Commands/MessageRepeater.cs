using Discord;
using Discord.Commands;
using Discord.Net;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RepeatCommands : ModuleBase
        {
            //guildid/RepeatRunners
            public static ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>> repeaters { get; }

            public class RepeatRunner
            {
                private Logger _log { get; }

                private CancellationTokenSource source { get; set; }
                private CancellationToken token { get; set; }
                public Repeater Repeater { get; }
                public ITextChannel Channel { get; }

                public RepeatRunner(Repeater repeater, ITextChannel channel = null)
                {
                    _log = LogManager.GetCurrentClassLogger();
                    this.Repeater = repeater;
                    this.Channel = channel ?? NadekoBot.Client.GetGuild(repeater.GuildId)?.GetTextChannelAsync(repeater.ChannelId).GetAwaiter().GetResult();
                    if (Channel == null)
                        return;
                    Task.Run(Run);
                }


                private async Task Run()
                {
                    source = new CancellationTokenSource();
                    token = source.Token;
                    IUserMessage oldMsg = null;
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            var toSend = "🔄 " + Repeater.Message;
                            await Task.Delay(Repeater.Interval, token).ConfigureAwait(false);

                            //var lastMsgInChannel = (await Channel.GetMessagesAsync(2)).FirstOrDefault();
                            // if (lastMsgInChannel.Id == oldMsg?.Id) //don't send if it's the same message in the channel
                            //     continue;

                            if (oldMsg != null)
                                try { await oldMsg.DeleteAsync(); } catch { }
                            try
                            {
                                oldMsg = await Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                            }
                            catch (HttpException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                _log.Warn("Missing permissions. Repeater stopped. ChannelId : {0}", Channel?.Id);
                                return;
                            }
                            catch (HttpException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                _log.Warn("Channel not found. Repeater stopped. ChannelId : {0}", Channel?.Id);
                                return;
                            }
                            catch (Exception ex)
                            {
                                _log.Warn(ex);
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                }

                public void Reset()
                {
                    source.Cancel();
                    var t = Task.Run(Run);
                }

                public void Stop()
                {
                    source.Cancel();
                }

                public override string ToString()
                {
                    return $"{this.Channel.Mention} | {(int)this.Repeater.Interval.TotalHours}:{this.Repeater.Interval:mm} | {this.Repeater.Message.TrimTo(33)}";
                }
            }

            static RepeatCommands()
            {
                var _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();

                repeaters = new ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>>(NadekoBot.AllGuildConfigs
                    .ToDictionary(gc => gc.GuildId,
                                    gc => new ConcurrentQueue<RepeatRunner>(gc.GuildRepeaters.Select(gr => new RepeatRunner(gr))
                                    .Where(gr => gr.Channel != null))));

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatInvoke(int index)
            {
                index -= 1;
                ConcurrentQueue<RepeatRunner> rep;
                if (!repeaters.TryGetValue(Context.Guild.Id, out rep))
                {
                    await Context.Channel.SendErrorAsync("ℹ️ **No repeating message found on this server.**").ConfigureAwait(false);
                    return;
                }

                var repList = rep.ToList();

                if (index >= repList.Count)
                {
                    await Context.Channel.SendErrorAsync("Index out of range.").ConfigureAwait(false);
                    return;
                }
                var repeater = repList[index].Repeater;

                await Context.Channel.SendMessageAsync("🔄 " + repeater.Message).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task RepeatRemove(int index)
            {
                if (index < 1)
                    return;
                index -= 1;

                ConcurrentQueue<RepeatRunner> rep;
                if (!repeaters.TryGetValue(Context.Guild.Id, out rep))
                    return;

                var repeaterList = rep.ToList();

                if (index >= repeaterList.Count)
                {
                    await Context.Channel.SendErrorAsync("Index out of range.").ConfigureAwait(false);
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

                if (repeaters.TryUpdate(Context.Guild.Id, new ConcurrentQueue<RepeatRunner>(repeaterList), rep))
                    await Context.Channel.SendConfirmAsync("Message Repeater",$"#{index+1} stopped.\n\n{repeater.ToString()}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(1)]
            public async Task Repeat(int minutes, [Remainder] string message)
            {
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

                var rep = new RepeatRunner(toAdd, (ITextChannel)Context.Channel);

                repeaters.AddOrUpdate(Context.Guild.Id, new ConcurrentQueue<RepeatRunner>(new[] { rep }), (key, old) =>
                {
                    old.Enqueue(rep);
                    return old;
                });

                await Context.Channel.SendConfirmAsync($"🔁 Repeating **\"{rep.Repeater.Message}\"** every `{rep.Repeater.Interval.Days} day(s), {rep.Repeater.Interval.Hours} hour(s) and {rep.Repeater.Interval.Minutes} minute(s)`.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatList()
            {
                ConcurrentQueue<RepeatRunner> repRunners;
                if (!repeaters.TryGetValue(Context.Guild.Id, out repRunners))
                {
                    await Context.Channel.SendConfirmAsync("No repeaters running on this server.").ConfigureAwait(false);
                    return;
                }

                var replist = repRunners.ToList();
                var sb = new StringBuilder();

                for (int i = 0; i < replist.Count; i++)
                {
                    var rep = replist[i];

                    sb.AppendLine($"`{i + 1}.` {rep.ToString()}");
                }

                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle("List Of Repeaters")
                    .WithDescription(sb.ToString()))
                        .ConfigureAwait(false);
            }
        }
    }
}