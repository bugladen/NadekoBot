using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Replacements;

namespace NadekoBot.Core.Services
{
    public class GreetSettingsService : INService
    {
        private readonly DbService _db;

        public readonly ConcurrentDictionary<ulong, GreetSettings> GuildConfigsCache;
        private readonly DiscordSocketClient _client;
        private readonly Logger _log;

        public GreetSettingsService(DiscordSocketClient client, NadekoBot bot, DbService db)
        {
            _db = db;
            _client = client;
            _log = LogManager.GetCurrentClassLogger();

            GuildConfigsCache = new ConcurrentDictionary<ulong, GreetSettings>(
                bot.AllGuildConfigs
                    .ToDictionary(g => g.GuildId, GreetSettings.Create));

            _client.UserJoined += UserJoined;
            _client.UserLeft += UserLeft;
        }

        private Task UserLeft(IGuildUser user)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    var conf = GetOrAddSettingsForGuild(user.GuildId);

                    if (!conf.SendChannelByeMessage) return;
                    var channel = (await user.Guild.GetTextChannelsAsync()).SingleOrDefault(c => c.Id == conf.ByeMessageChannelId);

                    if (channel == null) //maybe warn the server owner that the channel is missing
                        return;

                    var rep = new ReplacementBuilder()
                        .WithDefault(user, channel, user.Guild, _client)
                        .Build();

                    if (CREmbed.TryParse(conf.ChannelByeMessageText, out var embedData))
                    {
                        rep.Replace(embedData);
                        try
                        {
                            var toDelete = await channel.EmbedAsync(embedData.ToEmbed(), embedData.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
                            if (conf.AutoDeleteByeMessagesTimer > 0)
                            {
                                toDelete.DeleteAfter(conf.AutoDeleteByeMessagesTimer);
                            }
                        }
                        catch (Exception ex) { _log.Warn(ex); }
                    }
                    else
                    {
                        var msg = rep.Replace(conf.ChannelByeMessageText);
                        if (string.IsNullOrWhiteSpace(msg))
                            return;
                        try
                        {
                            var toDelete = await channel.SendMessageAsync(msg.SanitizeMentions()).ConfigureAwait(false);
                            if (conf.AutoDeleteByeMessagesTimer > 0)
                            {
                                toDelete.DeleteAfter(conf.AutoDeleteByeMessagesTimer);
                            }
                        }
                        catch (Exception ex) { _log.Warn(ex); }
                    }
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        private Task UserJoined(IGuildUser user)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    var conf = GetOrAddSettingsForGuild(user.GuildId);

                    if (conf.SendChannelGreetMessage)
                    {
                        var channel = (await user.Guild.GetTextChannelsAsync()).SingleOrDefault(c => c.Id == conf.GreetMessageChannelId);
                        if (channel != null) //maybe warn the server owner that the channel is missing
                        {
                            var rep = new ReplacementBuilder()
                                .WithDefault(user, channel, user.Guild, _client)
                                .Build();

                            if (CREmbed.TryParse(conf.ChannelGreetMessageText, out var embedData))
                            {
                                rep.Replace(embedData);
                                try
                                {
                                    var toDelete = await channel.EmbedAsync(embedData.ToEmbed(), embedData.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
                                    if (conf.AutoDeleteGreetMessagesTimer > 0)
                                    {
                                        toDelete.DeleteAfter(conf.AutoDeleteGreetMessagesTimer);
                                    }
                                }
                                catch (Exception ex) { _log.Warn(ex); }
                            }
                            else
                            {
                                var msg = rep.Replace(conf.ChannelGreetMessageText);
                                if (!string.IsNullOrWhiteSpace(msg))
                                {
                                    try
                                    {
                                        var toDelete = await channel.SendMessageAsync(msg.SanitizeMentions()).ConfigureAwait(false);
                                        if (conf.AutoDeleteGreetMessagesTimer > 0)
                                        {
                                            toDelete.DeleteAfter(conf.AutoDeleteGreetMessagesTimer);
                                        }
                                    }
                                    catch (Exception ex) { _log.Warn(ex); }
                                }
                            }
                        }
                    }

                    if (conf.SendDmGreetMessage)
                    {
                        var channel = await user.GetOrCreateDMChannelAsync();

                        if (channel != null)
                        {
                            var rep = new ReplacementBuilder()
                                .WithDefault(user, channel, user.Guild, _client)
                                .Build();
                            if (CREmbed.TryParse(conf.DmGreetMessageText, out var embedData))
                            {

                                rep.Replace(embedData);
                                try
                                {
                                    await channel.EmbedAsync(embedData.ToEmbed(), embedData.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    _log.Warn(ex);
                                }
                            }
                            else
                            {
                                var msg = rep.Replace(conf.DmGreetMessageText);
                                if (!string.IsNullOrWhiteSpace(msg))
                                {
                                    await channel.SendConfirmAsync(msg).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            });
            return Task.CompletedTask;
        }

        public GreetSettings GetOrAddSettingsForGuild(ulong guildId)
        {
            if(GuildConfigsCache.TryGetValue(guildId, out var settings) && 
                settings != null)
                return settings;

            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                settings = GreetSettings.Create(gc);
            }

            GuildConfigsCache.TryAdd(guildId, settings);
            return settings;
        }

        public async Task<bool> SetSettings(ulong guildId, GreetSettings settings)
        {
            if (settings.AutoDeleteByeMessagesTimer > 600 ||
                settings.AutoDeleteByeMessagesTimer < 0 ||
                settings.AutoDeleteGreetMessagesTimer > 600 ||
                settings.AutoDeleteGreetMessagesTimer < 0)
            {
                return false;
            }

            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId, set => set);
                conf.DmGreetMessageText = settings.DmGreetMessageText?.SanitizeMentions();
                conf.ChannelGreetMessageText = settings.ChannelGreetMessageText?.SanitizeMentions();
                conf.ChannelByeMessageText = settings.ChannelByeMessageText?.SanitizeMentions();

                conf.AutoDeleteGreetMessagesTimer = settings.AutoDeleteGreetMessagesTimer;
                conf.AutoDeleteGreetMessages = settings.AutoDeleteGreetMessagesTimer > 0;

                conf.AutoDeleteByeMessagesTimer = settings.AutoDeleteByeMessagesTimer;
                conf.AutoDeleteByeMessages = settings.AutoDeleteByeMessagesTimer > 0;

                conf.GreetMessageChannelId = settings.GreetMessageChannelId;
                conf.ByeMessageChannelId = settings.ByeMessageChannelId;

                conf.SendChannelGreetMessage = settings.SendChannelGreetMessage;
                conf.SendChannelByeMessage = settings.SendChannelByeMessage;

                await uow.CompleteAsync().ConfigureAwait(false);

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);
            }

            return true;
        }

        public async Task<bool> SetGreet(ulong guildId, ulong channelId, bool? value = null)
        {
            bool enabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId, set => set);
                enabled = conf.SendChannelGreetMessage = value ?? !conf.SendChannelGreetMessage;
                conf.GreetMessageChannelId = channelId;

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                await uow.CompleteAsync().ConfigureAwait(false);
            }
            return enabled;
        }

        public bool SetGreetMessage(ulong guildId, ref string message)
        {
            message = message?.SanitizeMentions();

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message));

            bool greetMsgEnabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId, set => set);
                conf.ChannelGreetMessageText = message;
                greetMsgEnabled = conf.SendChannelGreetMessage;

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                uow.Complete();
            }
            return greetMsgEnabled;
        }

        public async Task<bool> SetGreetDm(ulong guildId, bool? value = null)
        {
            bool enabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId, set => set);
                enabled = conf.SendDmGreetMessage = value ?? !conf.SendDmGreetMessage;

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                await uow.CompleteAsync().ConfigureAwait(false);
            }
            return enabled;
        }

        public bool SetGreetDmMessage(ulong guildId, ref string message)
        {
            message = message?.SanitizeMentions();

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message));

            bool greetMsgEnabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId);
                conf.DmGreetMessageText = message;
                greetMsgEnabled = conf.SendDmGreetMessage;

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                uow.Complete();
            }
            return greetMsgEnabled;
        }

        public async Task<bool> SetBye(ulong guildId, ulong channelId, bool? value = null)
        {
            bool enabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId, set => set);
                enabled = conf.SendChannelByeMessage = value ?? !conf.SendChannelByeMessage;
                conf.ByeMessageChannelId = channelId;

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                await uow.CompleteAsync();
            }
            return enabled;
        }

        public bool SetByeMessage(ulong guildId, ref string message)
        {
            message = message?.SanitizeMentions();

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message));

            bool byeMsgEnabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId, set => set);
                conf.ChannelByeMessageText = message;
                byeMsgEnabled = conf.SendChannelByeMessage;

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                uow.Complete();
            }
            return byeMsgEnabled;
        }

        public async Task SetByeDel(ulong guildId, int timer)
        {
            if (timer < 0 || timer > 600)
                return;

            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId, set => set);
                conf.AutoDeleteByeMessagesTimer = timer;

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async Task SetGreetDel(ulong id, int timer)
        {
            if (timer < 0 || timer > 600)
                return;

            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(id, set => set);
                conf.AutoDeleteGreetMessagesTimer = timer;

                var toAdd = GreetSettings.Create(conf);
                GuildConfigsCache.AddOrUpdate(id, toAdd, (key, old) => toAdd);

                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }
    }

    public class GreetSettings
    {
        public int AutoDeleteGreetMessagesTimer { get; set; }
        public int AutoDeleteByeMessagesTimer { get; set; }

        public ulong GreetMessageChannelId { get; set; }
        public ulong ByeMessageChannelId { get; set; }

        public bool SendDmGreetMessage { get; set; }
        public string DmGreetMessageText { get; set; }

        public bool SendChannelGreetMessage { get; set; }
        public string ChannelGreetMessageText { get; set; }

        public bool SendChannelByeMessage { get; set; }
        public string ChannelByeMessageText { get; set; }

        public static GreetSettings Create(GuildConfig g) => new GreetSettings()
        {
            AutoDeleteByeMessagesTimer = g.AutoDeleteByeMessagesTimer,
            AutoDeleteGreetMessagesTimer = g.AutoDeleteGreetMessagesTimer,
            GreetMessageChannelId = g.GreetMessageChannelId,
            ByeMessageChannelId = g.ByeMessageChannelId,
            SendDmGreetMessage = g.SendDmGreetMessage,
            DmGreetMessageText = g.DmGreetMessageText,
            SendChannelGreetMessage = g.SendChannelGreetMessage,
            ChannelGreetMessageText = g.ChannelGreetMessageText,
            SendChannelByeMessage = g.SendChannelByeMessage,
            ChannelByeMessageText = g.ChannelByeMessageText,
        };
    }
}