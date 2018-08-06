using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Modules.Utility.Common;
using NadekoBot.Modules.Utility.Services;
using NadekoBot.Core.Common;
using System.Collections.Generic;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RepeatCommands : NadekoSubmodule<MessageRepeaterService>
        {
            private readonly DiscordSocketClient _client;
            private readonly DbService _db;

            public RepeatCommands(DiscordSocketClient client, DbService db)
            {
                _client = client;
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatInvoke(int index)
            {
                if (!_service.RepeaterReady)
                    return;
                index -= 1;
                if (!_service.Repeaters.TryGetValue(Context.Guild.Id, out var rep))
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
                var repeater = repList[index];
                repeater.Value.Reset();
                await repeater.Value.Trigger().ConfigureAwait(false);

                try { await Context.Message.AddReactionAsync(new Emoji("🔄")).ConfigureAwait(false); } catch { }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatRemove(int index)
            {
                if (!_service.RepeaterReady)
                    return;
                if (index < 1)
                    return;
                index -= 1;

                if (!_service.Repeaters.TryGetValue(Context.Guild.Id, out var rep))
                    return;

                var repeaterList = rep.ToList();

                if (index >= repeaterList.Count)
                {
                    await ReplyErrorLocalized("index_out_of_range").ConfigureAwait(false);
                    return;
                }

                var repeater = repeaterList[index];
                if (rep.TryRemove(repeater.Value.Repeater.Id, out var runner))
                    runner.Stop();

                using (var uow = _db.UnitOfWork)
                {
                    var guildConfig = uow.GuildConfigs.ForId(Context.Guild.Id, set => set.Include(gc => gc.GuildRepeaters));

                    var item = guildConfig.GuildRepeaters.FirstOrDefault(r => r.Id == repeater.Value.Repeater.Id);
                    if (item != null)
                        guildConfig.GuildRepeaters.Remove(item);
                    await uow.CompleteAsync();
                }
                await Context.Channel.SendConfirmAsync(GetText("message_repeater"),
                    GetText("repeater_stopped", index + 1) + $"\n\n{repeater.Value}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [NadekoOptions(typeof(Repeater.Options))]
            [Priority(0)]
            public Task Repeat(params string[] options)
                => Repeat(null, options);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [NadekoOptions(typeof(Repeater.Options))]
            [Priority(1)]
            public async Task Repeat(GuildDateTime dt, params string[] options)
            {
                if (!_service.RepeaterReady)
                    return;

                var (opts, _) = OptionsParser.ParseFrom(new Repeater.Options(), options);

                if (string.IsNullOrWhiteSpace(opts.Message))
                    return;

                var toAdd = new Repeater()
                {
                    ChannelId = Context.Channel.Id,
                    GuildId = Context.Guild.Id,
                    Interval = TimeSpan.FromMinutes(opts.Interval),
                    Message = opts.Message,
                    NoRedundant = opts.NoRedundant,
                    StartTimeOfDay = dt?.InputTimeUtc.TimeOfDay,
                };

                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.ForId(Context.Guild.Id, set => set.Include(x => x.GuildRepeaters));

                    if (gc.GuildRepeaters.Count >= 5)
                        return;
                    gc.GuildRepeaters.Add(toAdd);

                    await uow.CompleteAsync();
                }

                var rep = new RepeatRunner((SocketGuild)Context.Guild, toAdd, _service);

                _service.Repeaters.AddOrUpdate(Context.Guild.Id,
                    new ConcurrentDictionary<int, RepeatRunner>(new[] { new KeyValuePair<int, RepeatRunner>(toAdd.Id, rep) }), (key, old) =>
                  {
                      old.TryAdd(rep.Repeater.Id, rep);
                      return old;
                  });

                string secondPart = "";
                if (dt != null)
                {
                    secondPart = GetText("repeater_initial",
                        Format.Bold(rep.InitialInterval.Hours.ToString()),
                        Format.Bold(rep.InitialInterval.Minutes.ToString()));
                }

                await Context.Channel.SendConfirmAsync(
                    "🔁 " + GetText("repeater",
                        Format.Bold(((IGuildUser)Context.User).GuildPermissions.MentionEveryone ? rep.Repeater.Message : rep.Repeater.Message.SanitizeMentions()),
                        Format.Bold(rep.Repeater.Interval.Days.ToString()),
                        Format.Bold(rep.Repeater.Interval.Hours.ToString()),
                        Format.Bold(rep.Repeater.Interval.Minutes.ToString())) + " " + secondPart).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task RepeatList()
            {
                if (!_service.RepeaterReady)
                    return;
                if (!_service.Repeaters.TryGetValue(Context.Guild.Id, out var repRunners))
                {
                    await ReplyConfirmLocalized("repeaters_none").ConfigureAwait(false);
                    return;
                }

                var replist = repRunners.ToList();
                var sb = new StringBuilder();

                for (var i = 0; i < replist.Count; i++)
                {
                    var rep = replist[i];

                    sb.AppendLine($"`{i + 1}.` {rep.Value}");
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