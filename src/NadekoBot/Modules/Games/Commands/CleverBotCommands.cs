using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NLog;
using Services.CleverBotApi;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class CleverBotCommands
        {
            private static Logger _log { get; }

            class CleverAnswer {
                public string Status { get; set; }
                public string Response { get; set; }
            }

            public static ConcurrentDictionary<ulong, Lazy<ChatterBotSession>> CleverbotGuilds { get; } = new ConcurrentDictionary<ulong, Lazy<ChatterBotSession>>();

            static CleverBotCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();

                using (var uow = DbHandler.UnitOfWork())
                {
                    var bot = ChatterBotFactory.Create(ChatterBotType.CLEVERBOT);
                    CleverbotGuilds = new ConcurrentDictionary<ulong, Lazy<ChatterBotSession>>(
                        NadekoBot.AllGuildConfigs
                            .Where(gc => gc.CleverbotEnabled)
                            .ToDictionary(gc => gc.GuildId, gc => new Lazy<ChatterBotSession>(() => bot.CreateSession(), true)));
                }

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }

            public static async Task<bool> TryAsk(IUserMessage msg) {
                var channel = msg.Channel as ITextChannel;

                if (channel == null)
                    return false;

                Lazy<ChatterBotSession> cleverbot;
                if (!CleverbotGuilds.TryGetValue(channel.Guild.Id, out cleverbot))
                    return false;

                var nadekoId = NadekoBot.Client.GetCurrentUser().Id;
                var normalMention = $"<@{nadekoId}> ";
                var nickMention = $"<@!{nadekoId}> ";
                string message;
                if (msg.Content.StartsWith(normalMention))
                {
                    message = msg.Content.Substring(normalMention.Length).Trim();
                }
                else if (msg.Content.StartsWith(nickMention))
                {
                    message = msg.Content.Substring(nickMention.Length).Trim();
                }
                else
                {
                    return false;
                }

                await msg.Channel.TriggerTypingAsync().ConfigureAwait(false);

                var response = await cleverbot.Value.Think(message).ConfigureAwait(false);
                try
                {
                    await msg.Channel.SendConfirmAsync(response.SanitizeMentions()).ConfigureAwait(false);
                }
                catch
                {
                    await msg.Channel.SendConfirmAsync(response.SanitizeMentions()).ConfigureAwait(false); // try twice :\
                }
                return true;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(ChannelPermission.ManageMessages)]
            public async Task Cleverbot(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                Lazy<ChatterBotSession> throwaway;
                if (CleverbotGuilds.TryRemove(channel.Guild.Id, out throwaway))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        uow.GuildConfigs.SetCleverbotEnabled(channel.Guild.Id, false);
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await channel.SendConfirmAsync($"{imsg.Author.Mention} Disabled cleverbot on this server.").ConfigureAwait(false);
                    return;
                }

                var cleverbot = ChatterBotFactory.Create(ChatterBotType.CLEVERBOT);
                var session = cleverbot.CreateSession();

                CleverbotGuilds.TryAdd(channel.Guild.Id, new Lazy<ChatterBotSession>(() => session, true));

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.GuildConfigs.SetCleverbotEnabled(channel.Guild.Id, true);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await channel.SendConfirmAsync($"{imsg.Author.Mention} Enabled cleverbot on this server.").ConfigureAwait(false);
            }
        }
    }
}