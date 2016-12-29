using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

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
        public class TranslateCommands
        {
            private static ConcurrentDictionary<ulong, bool> TranslatedChannels { get; } = new ConcurrentDictionary<ulong, bool>();
            private static ConcurrentDictionary<UserChannelPair, string> UserLanguages { get; } = new ConcurrentDictionary<UserChannelPair, string>();

            static TranslateCommands()
            {
                NadekoBot.Client.MessageReceived += async (msg) =>
                {
                    try
                    {
                        var umsg = msg as IUserMessage;
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

                        var text = await TranslateInternal(umsg, langs, umsg.Resolve(UserMentionHandling.Ignore), true)
                                            .ConfigureAwait(false);
                        if (autoDelete)
                            try { await umsg.DeleteAsync().ConfigureAwait(false); } catch { }
                        await umsg.Channel.SendConfirmAsync($"{umsg.Author.Mention} `:` " + text.Replace("<@ ", "<@").Replace("<@! ", "<@!")).ConfigureAwait(false);
                    }
                    catch { }
                };
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Translate(IUserMessage umsg, string langs, [Remainder] string text = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                try
                {
                    await umsg.Channel.TriggerTypingAsync().ConfigureAwait(false);
                    var translation = await TranslateInternal(umsg, langs, text);
                    await channel.SendConfirmAsync("Translation " + langs, translation).ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendErrorAsync("Bad input format, or something went wrong...").ConfigureAwait(false);
                }
            }

            private static async Task<string> TranslateInternal(IUserMessage umsg, string langs, [Remainder] string text = null, bool silent = false)
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
            [RequirePermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task AutoTranslate(IUserMessage msg, AutoDeleteAutoTranslate autoDelete = AutoDeleteAutoTranslate.Nodel)
            {
                var channel = (ITextChannel)msg.Channel;

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
            public async Task AutoTransLang(IUserMessage msg, [Remainder] string langs = null)
            {
                var channel = (ITextChannel)msg.Channel;

                var ucp = new UserChannelPair
                {
                    UserId = msg.Author.Id,
                    ChannelId = msg.Channel.Id,
                };

                if (string.IsNullOrWhiteSpace(langs))
                {
                    if (UserLanguages.TryRemove(ucp, out langs))
                        await channel.SendConfirmAsync($"{msg.Author.Mention}'s auto-translate language has been removed.").ConfigureAwait(false);
                    return;
                }

                var langarr = langs.ToLowerInvariant().Split('>');
                if (langarr.Length != 2)
                    return;
                var from = langarr[0];
                var to = langarr[1];

                if (!GoogleTranslator.Instance.Languages.Contains(from) || !GoogleTranslator.Instance.Languages.Contains(to))
                {
                    try { await channel.SendErrorAsync("Invalid source and/or target language.").ConfigureAwait(false); } catch { }
                    return;
                }

                UserLanguages.AddOrUpdate(ucp, langs, (key, val) => langs);

                await channel.SendConfirmAsync($"Your auto-translate language has been set to {from}>{to}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Translangs(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                await channel.SendTableAsync(GoogleTranslator.Instance.Languages, str => $"{str,-15}", columns: 3);
            }

        }
    }
}