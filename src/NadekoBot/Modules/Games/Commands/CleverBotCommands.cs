using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NLog;
//using Services.CleverBotApi;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Services.CleverBotApi;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class CleverBotCommands : NadekoSubmodule
        {
            private new static Logger _log { get; }

            public static ConcurrentDictionary<ulong, Lazy<ChatterBotSession>> CleverbotGuilds { get; }

            static CleverBotCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();
                
                CleverbotGuilds = new ConcurrentDictionary<ulong, Lazy<ChatterBotSession>>(
                    NadekoBot.AllGuildConfigs
                        .Where(gc => gc.CleverbotEnabled)
                        .ToDictionary(gc => gc.GuildId, gc => new Lazy<ChatterBotSession>(() => new ChatterBotSession(gc.GuildId), true)));

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }

            public static async Task<bool> TryAsk(IUserMessage msg)
            {
                var channel = msg.Channel as ITextChannel;

                if (channel == null)
                    return false;

                Lazy<ChatterBotSession> cleverbot;
                if (!CleverbotGuilds.TryGetValue(channel.Guild.Id, out cleverbot))
                    return false;

                var nadekoId = NadekoBot.Client.CurrentUser.Id;
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
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Cleverbot()
            {
                var channel = (ITextChannel)Context.Channel;

                Lazy<ChatterBotSession> throwaway;
                if (CleverbotGuilds.TryRemove(channel.Guild.Id, out throwaway))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        uow.GuildConfigs.SetCleverbotEnabled(Context.Guild.Id, false);
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await ReplyConfirmLocalized("cleverbot_disabled").ConfigureAwait(false);
                    return;
                }

                CleverbotGuilds.TryAdd(channel.Guild.Id, new Lazy<ChatterBotSession>(() => new ChatterBotSession(Context.Guild.Id), true));

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.GuildConfigs.SetCleverbotEnabled(Context.Guild.Id, true);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ReplyConfirmLocalized("cleverbot_enabled").ConfigureAwait(false);
            }
        }

        public class ChatterBotSession
        {
            private static NadekoRandom rng { get; } = new NadekoRandom();
            public string ChatterbotId { get; }
            public string ChannelId { get; }
            private int _botId = 6;

            public ChatterBotSession(ulong channelId)
            {
                ChannelId = channelId.ToString().ToBase64();
                ChatterbotId = rng.Next(0, 1000000).ToString().ToBase64();
            }

            private string apiEndpoint => "http://api.program-o.com/v2/chatbot/" +
                                          $"?bot_id={_botId}&" +
                                          "say={0}&" +
                                          $"convo_id=nadekobot_{ChatterbotId}_{ChannelId}&" +
                                          "format=json";

            public async Task<string> Think(string message)
            {
                using (var http = new HttpClient())
                {
                    var res = await http.GetStringAsync(string.Format(apiEndpoint, message)).ConfigureAwait(false);
                    var cbr = JsonConvert.DeserializeObject<ChatterBotResponse>(res);
                    //Console.WriteLine(cbr.Convo_id);
                    return cbr.BotSay.Replace("<br/>", "\n");
                }
            }
        }

        public class ChatterBotResponse
        {
            public string Convo_id { get; set; }
            public string BotSay { get; set; }
        }
    }
}