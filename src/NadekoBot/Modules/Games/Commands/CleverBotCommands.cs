using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class CleverBotCommands
        {
            private Logger _log { get; }

            class CleverAnswer {
                public string Status { get; set; }
                public string Response { get; set; }
            }
            public CleverBotCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
            }

            //user#discrim is the key
            public static ConcurrentHashSet<string> ChannelsInConversation { get; } = new ConcurrentHashSet<string>();
            public static ConcurrentHashSet<ulong> CleverbotGuilds { get; } = new ConcurrentHashSet<ulong>();

            static CleverBotCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    CleverbotGuilds = new ConcurrentHashSet<ulong>(uow.GuildConfigs.GetAll().Where(gc => gc.CleverbotEnabled).Select(gc => gc.GuildId));
                }
            }

            public static async Task<bool> TryAsk(IUserMessage msg) {
                var channel = msg.Channel as ITextChannel;

                if (channel == null)
                    return false;

                var nick = msg.Channel.Id + "NadekoBot";

                if (!ChannelsInConversation.Contains(nick) && !CleverbotGuilds.Contains(channel.Guild.Id))
                    return false;

                var nadekoId = NadekoBot.Client.GetCurrentUser().Id;
                var normalMention = $"<@{nadekoId}> ";
                var nickMention = $"<@!{nadekoId}> ";
                string message;
                if (msg.Content.StartsWith(normalMention))
                {
                    message = msg.Content.Substring(normalMention.Length);
                }
                else if (msg.Content.StartsWith(nickMention))
                {
                    message = msg.Content.Substring(nickMention.Length);
                }
                else
                {
                    return false;
                }

                await msg.Channel.TriggerTypingAsync().ConfigureAwait(false);

                using (var http = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "user", NadekoBot.Credentials.CleverbotApiUser},
                        { "key", NadekoBot.Credentials.CleverbotApiKey},
                        { "nick", nick},
                        { "text", message},
                    });
                    var res = await http.PostAsync("https://cleverbot.io/1.0/ask", content).ConfigureAwait(false);

                    if (res.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        try
                        {
                            var answer = JsonConvert.DeserializeObject<CleverAnswer>(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
                            try
                            {
                                await msg.Channel.SendMessageAsync(WebUtility.HtmlDecode(answer.Response)).ConfigureAwait(false);
                            }
                            catch
                            {
                                await msg.Channel.SendMessageAsync(answer.Response).ConfigureAwait(false); // try twice :\
                            }
                        }
                        catch { }
                        return true;
                    }
                    return false;
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(ChannelPermission.ManageMessages)]
            public async Task Cleverbot(IUserMessage imsg, string all = null)
            {
                var channel = (ITextChannel)imsg.Channel;
                
                if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.CleverbotApiKey) ||
                    string.IsNullOrWhiteSpace(NadekoBot.Credentials.CleverbotApiKey))
                {
                    await channel.SendMessageAsync(":anger: `Bot owner didn't setup Cleverbot Api keys. Session will not start.`").ConfigureAwait(false);
                    return;
                }

                if (all?.Trim().ToLowerInvariant() == "all")
                {
                    var cleverbotEnabled = CleverbotGuilds.Add(channel.Guild.Id);
                    if (!cleverbotEnabled)
                        CleverbotGuilds.TryRemove(channel.Guild.Id);

                    using (var uow = DbHandler.UnitOfWork())
                    {
                        uow.GuildConfigs.SetCleverbotEnabled(channel.Guild.Id, cleverbotEnabled);
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }

                    await channel.SendMessageAsync($"{imsg.Author.Mention} `{(cleverbotEnabled ? "Enabled" : "Disabled")} cleverbot for all users.`").ConfigureAwait(false);
                    return;
                }


                var nick = channel.Id + "NadekoBot";

                if (ChannelsInConversation.TryRemove(nick))
                {
                    await channel.SendMessageAsync($"{imsg.Author.Mention} `I will no longer reply to your messages starting with my mention.`").ConfigureAwait(false);
                    return;
                }

                using (var http = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                    {
                        { "user", NadekoBot.Credentials.CleverbotApiUser},
                        { "key", NadekoBot.Credentials.CleverbotApiKey},
                        { "nick", nick},
                    });
                    var res = await http.PostAsync("https://cleverbot.io/1.0/create", content).ConfigureAwait(false);
                    if (res.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        await channel.SendMessageAsync($"{imsg.Author.Mention} `Something went wrong in starting your cleverbot session :\\`");
                        _log.Warn(await res.Content.ReadAsStringAsync());
                        return;
                    }

                    ChannelsInConversation.Add(nick);
                }
                await channel.SendMessageAsync($"{imsg.Author.Mention} `I will reply to your messages starting with my mention.`").ConfigureAwait(false);
            }
            
        }
    }
}
