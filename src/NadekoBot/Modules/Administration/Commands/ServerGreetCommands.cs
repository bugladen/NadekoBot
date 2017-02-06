using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.DataStructures;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class ServerGreetCommands : ModuleBase
        {
            //make this to a field in the guildconfig table
            class GreetSettings
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

            private static Logger _log { get; }

            private static ConcurrentDictionary<ulong, GreetSettings> GuildConfigsCache { get; } = new ConcurrentDictionary<ulong, GreetSettings>();

            static ServerGreetCommands()
            {
                NadekoBot.Client.UserJoined += UserJoined;
                NadekoBot.Client.UserLeft += UserLeft;
                _log = LogManager.GetCurrentClassLogger();

                GuildConfigsCache = new ConcurrentDictionary<ulong, GreetSettings>(NadekoBot.AllGuildConfigs.ToDictionary(g => g.GuildId, (g) => GreetSettings.Create(g)));
            }

            private static GreetSettings GetOrAddSettingsForGuild(ulong guildId)
            {
                GreetSettings settings;
                GuildConfigsCache.TryGetValue(guildId, out settings);

                if (settings != null)
                    return settings;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var gc = uow.GuildConfigs.For(guildId, set => set);
                    settings = GreetSettings.Create(gc);
                }

                GuildConfigsCache.TryAdd(guildId, settings);
                return settings;
            }

            private static Task UserLeft(IGuildUser user)
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
                        CREmbed embedData;
                        if (CREmbed.TryParse(conf.ChannelByeMessageText, out embedData))
                        {
                            embedData.PlainText = embedData.PlainText?.Replace("%user%", user.Username).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                            embedData.Description = embedData.Description?.Replace("%user%", user.Username).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                            embedData.Title = embedData.Title?.Replace("%user%", user.Username).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                            try
                            {
                                var toDelete = await channel.EmbedAsync(embedData.ToEmbed(), embedData.PlainText ?? "").ConfigureAwait(false);
                                if (conf.AutoDeleteByeMessagesTimer > 0)
                                {
                                    toDelete.DeleteAfter(conf.AutoDeleteByeMessagesTimer);
                                }
                            }
                            catch (Exception ex) { _log.Warn(ex); }
                        }
                        else
                        {
                            var msg = conf.ChannelByeMessageText.Replace("%user%", user.Username).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
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
                    catch { }
                });
                return Task.CompletedTask;
            }

            private static Task UserJoined(IGuildUser user)
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

                                CREmbed embedData;
                                if (CREmbed.TryParse(conf.ChannelGreetMessageText, out embedData))
                                {
                                    embedData.PlainText = embedData.PlainText?.Replace("%user%", user.Mention).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                    embedData.Description = embedData.Description?.Replace("%user%", user.Mention).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                    embedData.Title = embedData.Title?.Replace("%user%", user.ToString()).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                    try
                                    {
                                        var toDelete = await channel.EmbedAsync(embedData.ToEmbed(), embedData.PlainText ?? "").ConfigureAwait(false);
                                        if (conf.AutoDeleteGreetMessagesTimer > 0)
                                        {
                                            toDelete.DeleteAfter(conf.AutoDeleteGreetMessagesTimer);
                                        }
                                    }
                                    catch (Exception ex) { _log.Warn(ex); }
                                }
                                else
                                {
                                    var msg = conf.ChannelGreetMessageText.Replace("%user%", user.Mention).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
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
                            var channel = await user.CreateDMChannelAsync();

                            if (channel != null)
                            {
                                CREmbed embedData;
                                if (CREmbed.TryParse(conf.ChannelGreetMessageText, out embedData))
                                {
                                    embedData.PlainText = embedData.PlainText?.Replace("%user%", user.ToString()).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                    embedData.Description = embedData.Description?.Replace("%user%", user.ToString()).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                    embedData.Title = embedData.Title?.Replace("%user%", user.ToString()).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                    try
                                    {
                                        await channel.EmbedAsync(embedData.ToEmbed(), embedData.PlainText ?? "").ConfigureAwait(false);
                                    }
                                    catch (Exception ex) { _log.Warn(ex); }
                                }
                                else
                                {
                                    var msg = conf.DmGreetMessageText.Replace("%user%", user.ToString()).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                    if (!string.IsNullOrWhiteSpace(msg))
                                    {
                                        await channel.SendConfirmAsync(msg).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                });
                return Task.CompletedTask;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDel(int timer = 30)
            {
                var channel = (ITextChannel)Context.Channel;
                if (timer < 0 || timer > 600)
                    return;

                await ServerGreetCommands.SetGreetDel(Context.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await Context.Channel.SendConfirmAsync($"🆗 Greet messages **will be deleted** after `{timer} seconds`.").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("ℹ️ Automatic deletion of greet messages has been **disabled**.").ConfigureAwait(false);
            }

            private static async Task SetGreetDel(ulong id, int timer)
            {
                if (timer < 0 || timer > 600)
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(id, set => set);
                    conf.AutoDeleteGreetMessagesTimer = timer;

                    var toAdd = GreetSettings.Create(conf);
                    GuildConfigsCache.AddOrUpdate(id, toAdd, (key, old) => toAdd);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Greet()
            {
                var enabled = await ServerGreetCommands.SetGreet(Context.Guild.Id, Context.Channel.Id).ConfigureAwait(false);

                if (enabled)
                    await Context.Channel.SendConfirmAsync("✅ Greeting messages **enabled** on this channel.").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("ℹ️ Greeting messages **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetGreet(ulong guildId, ulong channelId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetMsg([Remainder] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    string channelGreetMessageText;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        channelGreetMessageText = uow.GuildConfigs.For(Context.Guild.Id, set => set).ChannelGreetMessageText;
                    }
                    await Context.Channel.SendConfirmAsync("Current greet message: ", channelGreetMessageText?.SanitizeMentions());
                    return;
                }

                var sendGreetEnabled = ServerGreetCommands.SetGreetMessage(Context.Guild.Id, ref text);

                await Context.Channel.SendConfirmAsync("🆗 New greet message **set**.").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await Context.Channel.SendConfirmAsync("ℹ️ Enable greet messsages by typing `.greet`").ConfigureAwait(false);
            }

            public static bool SetGreetMessage(ulong guildId, ref string message)
            {
                message = message?.SanitizeMentions();

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentNullException(nameof(message));

                bool greetMsgEnabled;
                using (var uow = DbHandler.UnitOfWork())
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDm()
            {
                var enabled = await ServerGreetCommands.SetGreetDm(Context.Guild.Id).ConfigureAwait(false);

                if (enabled)
                    await Context.Channel.SendConfirmAsync("🆗 DM Greet announcements **enabled**.").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("ℹ️ Greet announcements **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetGreetDm(ulong guildId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId, set => set);
                    enabled = conf.SendDmGreetMessage = value ?? !conf.SendDmGreetMessage;

                    var toAdd = GreetSettings.Create(conf);
                    GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                return enabled;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task GreetDmMsg([Remainder] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    GuildConfig config;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        config = uow.GuildConfigs.For(Context.Guild.Id);
                    }
                    await Context.Channel.SendConfirmAsync("ℹ️ Current **DM greet** message: `" + config.DmGreetMessageText?.SanitizeMentions() + "`");
                    return;
                }

                var sendGreetEnabled = ServerGreetCommands.SetGreetDmMessage(Context.Guild.Id, ref text);

                await Context.Channel.SendConfirmAsync("🆗 New DM greet message **set**.").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await Context.Channel.SendConfirmAsync($"ℹ️ Enable DM greet messsages by typing `{NadekoBot.ModulePrefixes[typeof(Administration).Name]}greetdm`").ConfigureAwait(false);
            }

            public static bool SetGreetDmMessage(ulong guildId, ref string message)
            {
                message = message?.SanitizeMentions();

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentNullException(nameof(message));

                bool greetMsgEnabled;
                using (var uow = DbHandler.UnitOfWork())
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Bye()
            {
                var enabled = await ServerGreetCommands.SetBye(Context.Guild.Id, Context.Channel.Id).ConfigureAwait(false);

                if (enabled)
                    await Context.Channel.SendConfirmAsync("✅ Bye announcements **enabled** on this channel.").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("ℹ️ Bye announcements **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetBye(ulong guildId, ulong channelId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ByeMsg([Remainder] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    string byeMessageText;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        byeMessageText = uow.GuildConfigs.For(Context.Guild.Id, set => set).ChannelByeMessageText;
                    }
                    await Context.Channel.SendConfirmAsync("ℹ️ Current **bye** message: `" + byeMessageText?.SanitizeMentions() + "`");
                    return;
                }

                var sendByeEnabled = ServerGreetCommands.SetByeMessage(Context.Guild.Id, ref text);

                await Context.Channel.SendConfirmAsync("🆗 New bye message **set**.").ConfigureAwait(false);
                if (!sendByeEnabled)
                    await Context.Channel.SendConfirmAsync($"ℹ️ Enable bye messsages by typing `{NadekoBot.ModulePrefixes[typeof(Administration).Name]}bye`").ConfigureAwait(false);
            }

            public static bool SetByeMessage(ulong guildId, ref string message)
            {
                message = message?.SanitizeMentions();

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentNullException(nameof(message));

                bool byeMsgEnabled;
                using (var uow = DbHandler.UnitOfWork())
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ByeDel(int timer = 30)
            {
                await ServerGreetCommands.SetByeDel(Context.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await Context.Channel.SendConfirmAsync($"🆗 Bye messages **will be deleted** after `{timer} seconds`.").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("ℹ️ Automatic deletion of bye messages has been **disabled**.").ConfigureAwait(false);
            }

            private static async Task SetByeDel(ulong guildId, int timer)
            {
                if (timer < 0 || timer > 600)
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId, set => set);
                    conf.AutoDeleteByeMessagesTimer = timer;

                    var toAdd = GreetSettings.Create(conf);
                    GuildConfigsCache.AddOrUpdate(guildId, toAdd, (key, old) => toAdd);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

        }
    }
}