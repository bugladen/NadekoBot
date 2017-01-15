using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
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
        public class ServerGreetCommands : ModuleBase
        {
            private static Logger _log { get; }

            static ServerGreetCommands()
            {
                NadekoBot.Client.UserJoined += UserJoined;
                NadekoBot.Client.UserLeft += UserLeft;
                _log = LogManager.GetCurrentClassLogger();
            }
            //todo optimize ASAP
            private static async Task UserLeft(IGuildUser user)
            {
                try
                {
                    GuildConfig conf;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        conf = uow.GuildConfigs.For(user.Guild.Id, set => set);
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
                            toDelete.DeleteAfter(conf.AutoDeleteByeMessagesTimer);
                        }
                    }
                    catch (Exception ex) { _log.Warn(ex); }
                }
                catch { }
            }

            private static async Task UserJoined(IGuildUser user)
            {
                try
                {
                    GuildConfig conf;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        conf = uow.GuildConfigs.For(user.Guild.Id, set => set);
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
                                        toDelete.DeleteAfter(conf.AutoDeleteGreetMessagesTimer);
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
                                await channel.SendConfirmAsync(msg).ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch { }
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

            private static async Task SetByeDel(ulong id, int timer)
            {
                if (timer < 0 || timer > 600)
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(id, set => set);
                    conf.AutoDeleteByeMessagesTimer = timer;

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

        }
    }
}