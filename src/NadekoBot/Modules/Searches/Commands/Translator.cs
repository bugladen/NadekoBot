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
        public class TranslateCommands : NadekoSubmodule
        {
            private static ConcurrentDictionary<ulong, bool> translatedChannels { get; } = new ConcurrentDictionary<ulong, bool>();
            private static ConcurrentDictionary<UserChannelPair, string> userLanguages { get; } = new ConcurrentDictionary<UserChannelPair, string>();

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
                        if (!translatedChannels.TryGetValue(umsg.Channel.Id, out autoDelete))
                            return;
                        var key = new UserChannelPair()
                        {
                            UserId = umsg.Author.Id,
                            ChannelId = umsg.Channel.Id,
                        };

                        string langs;
                        if (!userLanguages.TryGetValue(key, out langs))
                            return;

                        var text = await TranslateInternal(langs, umsg.Resolve(TagHandling.Ignore))
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
                    await Context.Channel.SendConfirmAsync(GetText("translation") + " " + langs, translation).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("bad_input_format").ConfigureAwait(false);
                }
            }

            private static async Task<string> TranslateInternal(string langs, [Remainder] string text = null)
            {
                var langarr = langs.ToLowerInvariant().Split('>');
                if (langarr.Length != 2)
                    throw new ArgumentException();
                var from = langarr[0];
                var to = langarr[1];
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
                    translatedChannels.AddOrUpdate(channel.Id, true, (key, val) => true);
                    await ReplyConfirmLocalized("atl_ad_started").ConfigureAwait(false);
                    return;
                }

                bool throwaway;
                if (translatedChannels.TryRemove(channel.Id, out throwaway))
                {
                    await ReplyConfirmLocalized("atl_stopped").ConfigureAwait(false);
                    return;
                }
                if (translatedChannels.TryAdd(channel.Id, autoDelete == AutoDeleteAutoTranslate.Del))
                {
                    await ReplyConfirmLocalized("atl_started").ConfigureAwait(false);
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
                    if (userLanguages.TryRemove(ucp, out langs))
                        await ReplyConfirmLocalized("atl_removed").ConfigureAwait(false);
                    return;
                }

                var langarr = langs.ToLowerInvariant().Split('>');
                if (langarr.Length != 2)
                    return;
                var from = langarr[0];
                var to = langarr[1];

                if (!GoogleTranslator.Instance.Languages.Contains(from) || !GoogleTranslator.Instance.Languages.Contains(to))
                {
                    await ReplyErrorLocalized("invalid_lang").ConfigureAwait(false);
                    return;
                }

                userLanguages.AddOrUpdate(ucp, langs, (key, val) => langs);

                await ReplyConfirmLocalized("atl_set", from, to).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Translangs()
            {
                await Context.Channel.SendTableAsync(GoogleTranslator.Instance.Languages, str => $"{str,-15}", 3);
            }

        }
    }
}