using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class ServerGreetCommands
        {
            public static long Greeted = 0;
            private Logger _log;

            public ServerGreetCommands()
            {
                NadekoBot.Client.UserJoined += UserJoined;
                NadekoBot.Client.UserLeft += UserLeft;
                _log = LogManager.GetCurrentClassLogger();
            }

            private Task UserLeft(IGuildUser user)
            {
                var leftTask = Task.Run(async () =>
                {
                    try
                    {
                        GuildConfig conf;
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            conf = uow.GuildConfigs.For(user.Guild.Id);
                        }

                        if (!conf.SendChannelByeMessage) return;
                        var channel = (await user.Guild.GetTextChannelsAsync()).SingleOrDefault(c => c.Id == conf.ByeMessageChannelId);

                        if (channel == null) //maybe warn the server owner that the channel is missing
                            return;

                        var msg = conf.ChannelByeMessageText.Replace("%user%", user.Username).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                        if (string.IsNullOrWhiteSpace(msg))
                            return;
                        try
                        {
                            var toDelete = await channel.SendMessageAsync(msg.SanitizeMentions()).ConfigureAwait(false);
                            if (conf.AutoDeleteByeMessagesTimer > 0)
                            {
                                var t = Task.Run(async () =>
                                {
                                    await Task.Delay(conf.AutoDeleteByeMessagesTimer * 1000).ConfigureAwait(false); // 5 minutes
                                    try { await toDelete.DeleteAsync().ConfigureAwait(false); } catch { }
                                });
                            }
                        }
                        catch (Exception ex) { _log.Warn(ex); }
                    }
                    catch { }
                });
                return Task.CompletedTask;
            }

            private Task UserJoined(IGuildUser user)
            {
                var joinedTask = Task.Run(async () =>
                {
                    try
                    {
                        GuildConfig conf;
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            conf = uow.GuildConfigs.For(user.Guild.Id);
                        }

                        if (conf.SendChannelGreetMessage)
                        {
                            var channel = (await user.Guild.GetTextChannelsAsync()).SingleOrDefault(c => c.Id == conf.GreetMessageChannelId);
                            if (channel != null) //maybe warn the server owner that the channel is missing
                            {
                                var msg = conf.ChannelGreetMessageText.Replace("%user%", user.Mention).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                if (!string.IsNullOrWhiteSpace(msg))
                                {
                                    try
                                    {
                                        var toDelete = await channel.SendMessageAsync(msg.SanitizeMentions()).ConfigureAwait(false);
                                        if (conf.AutoDeleteGreetMessagesTimer > 0)
                                        {
                                            var t = Task.Run(async () =>
                                            {
                                                await Task.Delay(conf.AutoDeleteGreetMessagesTimer * 1000).ConfigureAwait(false); // 5 minutes
                                                try { await toDelete.DeleteAsync().ConfigureAwait(false); } catch { }
                                            });
                                        }
                                    }
                                    catch (Exception ex) { _log.Warn(ex); }
                                }
                            }
                        }

                        if (conf.SendDmGreetMessage)
                        {
                            var channel = await user.CreateDMChannelAsync();

                            if (channel != null)
                            {
                                var msg = conf.DmGreetMessageText.Replace("%user%", user.Username).Replace("%server%", user.Guild.Name);
                                if (!string.IsNullOrWhiteSpace(msg))
                                {
                                    await channel.SendMessageAsync(msg).ConfigureAwait(false);
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
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task GreetDel(IUserMessage umsg, int timer = 30)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (timer < 0 || timer > 600)
                    return;

                await ServerGreetCommands.SetGreetDel(channel.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await channel.SendMessageAsync($"🆗 Greet messages **will be deleted** after `{timer} seconds`.").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("ℹ️ Automatic deletion of greet messages has been **disabled**.").ConfigureAwait(false);
            }

            private static async Task SetGreetDel(ulong id, int timer)
            {
                if (timer < 0 || timer > 600)
                    return;
                
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(id);
                    conf.AutoDeleteGreetMessagesTimer = timer;
                    uow.GuildConfigs.Update(conf);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task Greet(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                var enabled = await ServerGreetCommands.SetGreet(channel.Guild.Id, channel.Id).ConfigureAwait(false);

                if (enabled)
                    await channel.SendMessageAsync("✅ Greeting messages **enabled** on this channel.").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("ℹ️ Greeting messages **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetGreet(ulong guildId, ulong channelId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId);
                    enabled = conf.SendChannelGreetMessage = value ?? !conf.SendChannelGreetMessage;
                    conf.GreetMessageChannelId = channelId;
                    uow.GuildConfigs.Update(conf);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                return enabled;
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task GreetMsg(IUserMessage umsg, [Remainder] string text = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(text))
                {
                    GuildConfig config;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        config = uow.GuildConfigs.For(channel.Guild.Id);
                    }
                    await channel.SendMessageAsync("ℹ️ Current **greet** message: `" + config.ChannelGreetMessageText?.SanitizeMentions() + "`");
                    return;
                }

                var sendGreetEnabled = ServerGreetCommands.SetGreetMessage(channel.Guild.Id, ref text);

                await channel.SendMessageAsync("🆗 New greet message **set**.").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await channel.SendMessageAsync("ℹ️ Enable greet messsages by typing `.greet`").ConfigureAwait(false);
            }

            public static bool SetGreetMessage(ulong guildId, ref string message)
            {
                message = message?.SanitizeMentions();

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentNullException(nameof(message));

                bool greetMsgEnabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId);
                    conf.ChannelGreetMessageText = message;
                    greetMsgEnabled = conf.SendChannelGreetMessage;

                    uow.GuildConfigs.Update(conf);
                    uow.Complete();
                }
                return greetMsgEnabled;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task GreetDm(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                var enabled = await ServerGreetCommands.SetGreetDm(channel.Guild.Id).ConfigureAwait(false);

                if (enabled)
                    await channel.SendMessageAsync("🆗 DM Greet announcements **enabled**.").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("ℹ️ Greet announcements **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetGreetDm(ulong guildId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId);
                    enabled = conf.SendDmGreetMessage = value ?? !conf.SendDmGreetMessage;
                    uow.GuildConfigs.Update(conf);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                return enabled;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task GreetDmMsg(IUserMessage umsg, [Remainder] string text = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(text))
                {
                    GuildConfig config;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        config = uow.GuildConfigs.For(channel.Guild.Id);
                    }
                    await channel.SendMessageAsync("ℹ️ Current **DM greet** message: `" + config.DmGreetMessageText?.SanitizeMentions() + "`");
                    return;
                }

                var sendGreetEnabled = ServerGreetCommands.SetGreetDmMessage(channel.Guild.Id, ref text);

                await channel.SendMessageAsync("🆗 New DM greet message **set**.").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await channel.SendMessageAsync($"ℹ️ Enable DM greet messsages by typing `{NadekoBot.ModulePrefixes[typeof(Administration).Name]}greetdm`").ConfigureAwait(false);
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

                    uow.GuildConfigs.Update(conf);
                    uow.Complete();
                }
                return greetMsgEnabled;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task Bye(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                var enabled = await ServerGreetCommands.SetBye(channel.Guild.Id, channel.Id).ConfigureAwait(false);

                if (enabled)
                    await channel.SendMessageAsync("✅ Bye announcements **enabled** on this channel.").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("ℹ️ Bye announcements **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetBye(ulong guildId, ulong channelId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId);
                    enabled = conf.SendChannelByeMessage = value ?? !conf.SendChannelByeMessage;
                    conf.ByeMessageChannelId = channelId;
                    uow.GuildConfigs.Update(conf);
                    await uow.CompleteAsync();
                }
                return enabled;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task ByeMsg(IUserMessage umsg, [Remainder] string text = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(text))
                {
                    GuildConfig config;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        config = uow.GuildConfigs.For(channel.Guild.Id);
                    }
                    await channel.SendMessageAsync("ℹ️ Current **bye** message: `" + config.ChannelByeMessageText?.SanitizeMentions() + "`");
                    return;
                }

                var sendByeEnabled = ServerGreetCommands.SetByeMessage(channel.Guild.Id, ref text);

                await channel.SendMessageAsync("🆗 New bye message **set**.").ConfigureAwait(false);
                if (!sendByeEnabled)
                    await channel.SendMessageAsync($"ℹ️ Enable bye messsages by typing `{NadekoBot.ModulePrefixes[typeof(Administration).Name]}bye`").ConfigureAwait(false);
            }
            
            public static bool SetByeMessage(ulong guildId, ref string message)
            {
                message = message?.SanitizeMentions();

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentNullException(nameof(message));

                bool byeMsgEnabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId);
                    conf.ChannelByeMessageText = message;
                    byeMsgEnabled = conf.SendChannelByeMessage;

                    uow.GuildConfigs.Update(conf);
                    uow.Complete();
                }
                return byeMsgEnabled;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task ByeDel(IUserMessage umsg, int timer = 30)
            {
                var channel = (ITextChannel)umsg.Channel;

                await ServerGreetCommands.SetByeDel(channel.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await channel.SendMessageAsync($"🆗 Bye messages **will be deleted** after `{timer} seconds`.").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("ℹ️ Automatic deletion of bye messages has been **disabled**.").ConfigureAwait(false);
            }

            private static async Task SetByeDel(ulong id, int timer)
            {
                if (timer < 0 || timer > 600)
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(id);
                    conf.AutoDeleteByeMessagesTimer = timer;
                    uow.GuildConfigs.Update(conf);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

        }
    }
}
