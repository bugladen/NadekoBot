using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Discord.WebSocket;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        public struct UserChannelPair
        {
            public ulong UserId { get; set; }
            public ulong ChannelId { get; set; }
        }

        [Group]
        public class TranslateCommands : ModuleBase
        {
            private static ConcurrentDictionary<ulong, bool> TranslatedChannels { get; } = new ConcurrentDictionary<ulong, bool>();
            private static ConcurrentDictionary<UserChannelPair, string> UserLanguages { get; } = new ConcurrentDictionary<UserChannelPair, string>();

            static TranslateCommands()
            {
                NadekoBot.Client.MessageReceived += async (msg) =>
                {
                    try
                    {
                        var umsg = msg as SocketUserMessage;
                        if (umsg == null)
                            return;

                        bool autoDelete;
                        if (!TranslatedChannels.TryGetValue(umsg.Channel.Id, out autoDelete))
                            return;
                        var key = new UserChannelPair()
                        {
                            UserId = umsg.Author.Id,
                            ChannelId = umsg.Channel.Id,
                        };

                        string langs;
                        if (!UserLanguages.TryGetValue(key, out langs))
                            return;

                        var text = await TranslateInternal(langs, umsg.Resolve(TagHandling.Ignore), true)
                                            .ConfigureAwait(false);
                        if (autoDelete)
                            try { await umsg.DeleteAsync().ConfigureAwait(false); } catch { }
                        await umsg.Channel.SendConfirmAsync($"{umsg.Author.Mention} `:` " + text.Replace("<@ ", "<@").Replace("<@! ", "<@!")).ConfigureAwait(false);
                    }
                    catch { }
                };
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Translate(string langs, [Remainder] string text = null)
            {
                try
                {
                    await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                    var translation = await TranslateInternal(langs, text);
                    await Context.Channel.SendConfirmAsync("Translation " + langs, translation).ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("Bad input format, or something went wrong...").ConfigureAwait(false);
                }
            }

            private static async Task<string> TranslateInternal(string langs, [Remainder] string text = null, bool silent = false)
            {
                var langarr = langs.ToLowerInvariant().Split('>');
                if (langarr.Length != 2)
                    throw new ArgumentException();
                string from = langarr[0];
                string to = langarr[1];
                text = text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    throw new ArgumentException();
                return (await GoogleTranslator.Instance.Translate(text, from, to).ConfigureAwait(false)).SanitizeMentions();
            }

            public enum AutoDeleteAutoTranslate
            {
                Del,
                Nodel
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task AutoTranslate(AutoDeleteAutoTranslate autoDelete = AutoDeleteAutoTranslate.Nodel)
            {
                var channel = (ITextChannel)Context.Channel;

                if (autoDelete == AutoDeleteAutoTranslate.Del)
                {
                    TranslatedChannels.AddOrUpdate(channel.Id, true, (key, val) => true);
                    try { await channel.SendConfirmAsync("Started automatic translation of messages on this channel. User messages will be auto-deleted.").ConfigureAwait(false); } catch { }
                    return;
                }

                bool throwaway;
                if (TranslatedChannels.TryRemove(channel.Id, out throwaway))
                {
                    try { await channel.SendConfirmAsync("Stopped automatic translation of messages on this channel.").ConfigureAwait(false); } catch { }
                    return;
                }
                else if (TranslatedChannels.TryAdd(channel.Id, autoDelete == AutoDeleteAutoTranslate.Del))
                {
                    try { await channel.SendConfirmAsync("Started automatic translation of messages on this channel.").ConfigureAwait(false); } catch { }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AutoTransLang([Remainder] string langs = null)
            {
                var ucp = new UserChannelPair
                {
                    UserId = Context.User.Id,
                    ChannelId = Context.Channel.Id,
                };

                if (string.IsNullOrWhiteSpace(langs))
                {
                    if (UserLanguages.TryRemove(ucp, out langs))
                        await Context.Channel.SendConfirmAsync($"{Context.User.Mention}'s auto-translate language has been removed.").ConfigureAwait(false);
                    return;
                }

                var langarr = langs.ToLowerInvariant().Split('>');
                if (langarr.Length != 2)
                    return;
                var from = langarr[0];
                var to = langarr[1];

                if (!GoogleTranslator.Instance.Languages.Contains(from) || !GoogleTranslator.Instance.Languages.Contains(to))
                {
                    try { await Context.Channel.SendErrorAsync("Invalid source and/or target language.").ConfigureAwait(false); } catch { }
                    return;
                }

                UserLanguages.AddOrUpdate(ucp, langs, (key, val) => langs);

                await Context.Channel.SendConfirmAsync($"Your auto-translate language has been set to {from}>{to}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Translangs()
            {
                await Context.Channel.SendTableAsync(GoogleTranslator.Instance.Languages, str => $"{str,-15}", columns: 3);
            }

        }
    }
}