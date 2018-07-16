using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Core.Services;
using NLog;
using System.Collections.Concurrent;
using NadekoBot.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Common.Replacements;
using NadekoBot.Common;

namespace NadekoBot.Modules.Administration.Services
{
    public class AdministrationService : INService
    {
        public ConcurrentHashSet<ulong> DeleteMessagesOnCommand { get; }
        public ConcurrentDictionary<ulong, bool> DeleteMessagesOnCommandChannels { get; }

        private readonly Logger _log;
        private readonly NadekoBot _bot;
        private readonly DbService _db;

        public AdministrationService(NadekoBot bot, CommandHandler cmdHandler, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _bot = bot;
            _db = db;

            DeleteMessagesOnCommand = new ConcurrentHashSet<ulong>(bot.AllGuildConfigs
                .Where(g => g.DeleteMessageOnCommand)
                .Select(g => g.GuildId));

            DeleteMessagesOnCommandChannels = new ConcurrentDictionary<ulong, bool>(bot.AllGuildConfigs
                .SelectMany(x => x.DelMsgOnCmdChannels)
                .ToDictionary(x => x.ChannelId, x => x.State)
                .ToConcurrent());

            cmdHandler.CommandExecuted += DelMsgOnCmd_Handler;
        }

        public (bool DelMsgOnCmd, IEnumerable<DelMsgOnCmdChannel> channels) GetDelMsgOnCmdData(ulong guildId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.ForId(guildId,
                    set => set.Include(x => x.DelMsgOnCmdChannels));

                return (conf.DeleteMessageOnCommand, conf.DelMsgOnCmdChannels);
            }
        }

        private Task DelMsgOnCmd_Handler(IUserMessage msg, CommandInfo cmd)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(msg.Channel is SocketTextChannel channel))
                        return;

                    if (DeleteMessagesOnCommandChannels.TryGetValue(channel.Id, out var state))
                    {
                        if (state)
                        {
                            await msg.DeleteAsync().ConfigureAwait(false);
                        }
                        //if state is false, that means do not do it
                    }
                    else if (DeleteMessagesOnCommand.Contains(channel.Guild.Id) && cmd.Name != "prune" && cmd.Name != "pick")
                    {
                        try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn("Delmsgoncmd errored...");
                    _log.Warn(ex);
                }
            });
            return Task.CompletedTask;
        }

        public bool ToggleDeleteMessageOnCommand(ulong guildId)
        {
            bool enabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.ForId(guildId, set => set);
                enabled = conf.DeleteMessageOnCommand = !conf.DeleteMessageOnCommand;

                uow.Complete();
            }
            return enabled;
        }

        public async Task SetDelMsgOnCmdState(ulong guildId, ulong chId, Administration.State s)
        {
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.ForId(guildId,
                    set => set.Include(x => x.DelMsgOnCmdChannels));

                var obj = new DelMsgOnCmdChannel()
                {
                    ChannelId = chId,
                    State = s == Administration.State.Enable,
                };
                conf.DelMsgOnCmdChannels.Remove(obj);
                if (s != Administration.State.Inherit)
                    conf.DelMsgOnCmdChannels.Add(obj);

                await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (s == Administration.State.Disable)
            {
                DeleteMessagesOnCommandChannels.AddOrUpdate(chId, false, delegate { return false; });
            }
            else if (s == Administration.State.Enable)
            {
                DeleteMessagesOnCommandChannels.AddOrUpdate(chId, true, delegate { return true; });
            }
            else
            {
                DeleteMessagesOnCommandChannels.TryRemove(chId, out var _);
            }
        }

        public async Task DeafenUsers(bool value, params IGuildUser[] users)
        {
            if (!users.Any())
                return;
            foreach (var u in users)
            {
                try
                {
                    await u.ModifyAsync(usr => usr.Deaf = value).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
        }

        public async Task EditMessage(ICommandContext context, ulong messageId, string text)
        {
            var msgs = await context.Channel.GetMessagesAsync().FlattenAsync()
                   .ConfigureAwait(false);

            IUserMessage msg = (IUserMessage)msgs.FirstOrDefault(x => x.Id == messageId
                && x.Author.Id == context.Client.CurrentUser.Id
                && x is IUserMessage);

            if (msg == null)
                return;

            var rep = new ReplacementBuilder()
                    .WithDefault(context)
                    .Build();

            if (CREmbed.TryParse(text, out var crembed))
            {
                rep.Replace(crembed);
                await msg.ModifyAsync(x =>
                {
                    x.Embed = crembed.ToEmbed().Build();
                    x.Content = crembed.PlainText?.SanitizeMentions() ?? "";
                }).ConfigureAwait(false);
            }
            else
            {
                await msg.ModifyAsync(x => x.Content = text.SanitizeMentions())
                    .ConfigureAwait(false);
            }
        }
    }
}
