using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class RepeatCommands : ModuleBase
        {
            public static ConcurrentDictionary<ulong, RepeatRunner> repeaters { get; }

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
                            try { oldMsg = await Channel.SendMessageAsync(toSend).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
            }

            static RepeatCommands()
            {
                var _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();
                using (var uow = DbHandler.UnitOfWork())
                {
                    repeaters = new ConcurrentDictionary<ulong, RepeatRunner>(uow.Repeaters.GetAll().Select(r => new RepeatRunner(r)).Where(r => r != null).ToDictionary(r => r.Repeater.ChannelId));
                }

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatInvoke()
            {
                RepeatRunner rep;
                if (!repeaters.TryGetValue(Context.Channel.Id, out rep))
                {
                    await Context.Channel.SendErrorAsync("ℹ️ **No repeating message found on this server.**").ConfigureAwait(false);
                    return;
                }
                rep.Reset();
                await Context.Channel.SendMessageAsync("🔄 " + rep.Repeater.Message).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Repeat()
            {
                RepeatRunner rep;
                if (repeaters.TryRemove(Context.Channel.Id, out rep))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        uow.Repeaters.Remove(rep.Repeater);
                        await uow.CompleteAsync();
                    }
                    rep.Stop();
                    await Context.Channel.SendConfirmAsync("✅ **Stopped repeating a message.**").ConfigureAwait(false);
                }
                else
                    await Context.Channel.SendConfirmAsync("ℹ️ **No message is repeating.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Repeat(int minutes, [Remainder] string message)
            {
                if (minutes < 1 || minutes > 10080)
                    return;

                if (string.IsNullOrWhiteSpace(message))
                    return;

                RepeatRunner rep;

                rep = repeaters.AddOrUpdate(Context.Channel.Id, (cid) =>
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var localRep = new Repeater
                        {
                            ChannelId = Context.Channel.Id,
                            GuildId = Context.Guild.Id,
                            Interval = TimeSpan.FromMinutes(minutes),
                            Message = message,
                        };
                        uow.Repeaters.Add(localRep);
                        uow.Complete();
                        return new RepeatRunner(localRep, (ITextChannel)Context.Channel);
                    }
                }, (cid, old) =>
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        old.Repeater.Message = message;
                        old.Repeater.Interval = TimeSpan.FromMinutes(minutes);
                        uow.Repeaters.Update(old.Repeater);
                        uow.Complete();
                    }
                    old.Reset();
                    return old;
                });

                await Context.Channel.SendConfirmAsync($"🔁 Repeating **\"{rep.Repeater.Message}\"** every `{rep.Repeater.Interval.Days} day(s), {rep.Repeater.Interval.Hours} hour(s) and {rep.Repeater.Interval.Minutes} minute(s)`.").ConfigureAwait(false);
            }
        }
    }
}
