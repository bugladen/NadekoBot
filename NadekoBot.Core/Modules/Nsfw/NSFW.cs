﻿using Discord;
using Discord.Commands;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Collections;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Modules.Searches.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.NSFW
{
    // thanks to halitalf for adding autoboob and autobutt features :D
    public class NSFW : NadekoTopLevelModule<SearchesService>
    {
        private static readonly ConcurrentHashSet<ulong> _hentaiBombBlacklist = new ConcurrentHashSet<ulong>();
        private readonly IHttpClientFactory _httpFactory;

        public NSFW(IHttpClientFactory factory)
        {
            _httpFactory = factory;
        }

        private async Task InternalHentai(IMessageChannel channel, string tag)
        {
            // create a random number generator
            var rng = new NadekoRandom();

            // get all of the DAPI search types, except first 3 
            // which are safebooru (not nsfw), and 2 furry ones 🤢
            var listOfProviders = Enum.GetValues(typeof(DapiSearchType))
                .Cast<DapiSearchType>()
                .Skip(3)
                .ToList();

            // now try to get an image, if it fails return an error,
            // keep trying for each provider until one of them is successful, or until 
            // we run out of providers. If we run out, then return an error
            ImageCacherObject img;
            do
            {
                // random index of the providers
                var num = rng.Next(0, listOfProviders.Count);
                // get the type
                var type = listOfProviders[num];
                // remove it 
                listOfProviders.RemoveAt(num);
                // get the image
                img = await _service.DapiSearch(tag, type, ctx.Guild?.Id, true).ConfigureAwait(false);
                // if i can't find the image, ran out of providers, or tag is blacklisted
                // return the error
                if (img == null && !listOfProviders.Any())
                {
                    await ReplyErrorLocalizedAsync("not_found").ConfigureAwait(false);
                    return;
                }

            } while (img == null);

            await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithImageUrl(img.FileUrl)
                .WithDescription($"[{GetText("tag")}: {tag}]({img})"))
                .ConfigureAwait(false);
        }

        private async Task InternalBoobs(IMessageChannel Channel)
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new NadekoRandom().Next(0, 10330)}").ConfigureAwait(false))[0];
                }
                await Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }
        private async Task InternalButts(IMessageChannel Channel)
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{new NadekoRandom().Next(0, 4335)}").ConfigureAwait(false))[0];
                }
                await Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

#if !GLOBAL_NADEKO
        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoHentai(int interval = 0, string tags = null)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoHentaiTimers.TryRemove(ctx.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalizedAsync("stopped").ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            var tagsArr = tags?.Split('|');

            t = new Timer(async (state) =>
            {
                try
                {
                    if (tagsArr == null || tagsArr.Length == 0)
                        await InternalHentai(ctx.Channel, null).ConfigureAwait(false);
                    else
                        await InternalHentai(ctx.Channel, tagsArr[new NadekoRandom().Next(0, tagsArr.Length)]).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoHentaiTimers.AddOrUpdate(ctx.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalizedAsync("autohentai_started",
                interval,
                string.Join(", ", tagsArr)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoBoobs(int interval = 0)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoBoobTimers.TryRemove(ctx.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalizedAsync("stopped").ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            t = new Timer(async (state) =>
            {
                try
                {
                    await InternalBoobs(ctx.Channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoBoobTimers.AddOrUpdate(ctx.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalizedAsync("started", interval).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoButts(int interval = 0)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoButtTimers.TryRemove(ctx.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalizedAsync("stopped").ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            t = new Timer(async (state) =>
            {
                try
                {
                    await InternalButts(ctx.Channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoButtTimers.AddOrUpdate(ctx.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalizedAsync("started", interval).ConfigureAwait(false);
        }
#endif

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Hentai([Leftover] string tag = null) =>
            InternalHentai(ctx.Channel, tag);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task HentaiBomb([Leftover] string tag = null)
        {
            if (!_hentaiBombBlacklist.Add(ctx.Guild?.Id ?? ctx.User.Id))
                return;
            try
            {
                var images = await Task.WhenAll(_service.DapiSearch(tag, DapiSearchType.Gelbooru, ctx.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Danbooru, ctx.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Konachan, ctx.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Yandere, ctx.Guild?.Id, true)).ConfigureAwait(false);

                var linksEnum = images?.Where(l => l != null).ToArray();
                if (images == null || !linksEnum.Any())
                {
                    await ReplyErrorLocalizedAsync("not_found").ConfigureAwait(false);
                    return;
                }

                await ctx.Channel.SendMessageAsync(string.Join("\n\n", linksEnum.Select(x => x.FileUrl))).ConfigureAwait(false);
            }
            finally
            {
                _hentaiBombBlacklist.TryRemove(ctx.Guild?.Id ?? ctx.User.Id);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Yandere([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Yandere, false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Konachan([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Konachan, false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task E621([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.E621, false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Rule34([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Rule34, false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Danbooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Danbooru, false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Gelbooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Gelbooru, false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Derpibooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Derpibooru, false);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Boobs()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new NadekoRandom().Next(0, 12000)}").ConfigureAwait(false))[0];
                }
                await ctx.Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Butts()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{new NadekoRandom().Next(0, 6100)}").ConfigureAwait(false))[0];
                }
                await ctx.Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task NsfwTagBlacklist([Leftover] string tag = null)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                var blTags = _service.GetBlacklistedTags(ctx.Guild.Id);
                await ctx.Channel.SendConfirmAsync(GetText("blacklisted_tag_list"),
                    blTags.Any()
                    ? string.Join(", ", blTags)
                    : "-").ConfigureAwait(false);
            }
            else
            {
                tag = tag.Trim().ToLowerInvariant();
                var added = _service.ToggleBlacklistedTag(ctx.Guild.Id, tag);

                if (added)
                    await ReplyConfirmLocalizedAsync("blacklisted_tag_add", tag).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("blacklisted_tag_remove", tag).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public Task NsfwClearCache()
        {
            _service.ClearCache();
            return ctx.Channel.SendConfirmAsync("👌");
        }

        public async Task InternalDapiCommand(string tag, DapiSearchType type, bool forceExplicit)
        {
            ImageCacherObject imgObj;

            imgObj = await _service.DapiSearch(tag, type, ctx.Guild?.Id, forceExplicit).ConfigureAwait(false);

            if (imgObj == null)
                await ReplyErrorLocalizedAsync("not_found").ConfigureAwait(false);
            else
            {
                var embed = new EmbedBuilder().WithOkColor()
                    .WithDescription($"{ctx.User} [{tag ?? "url"}]({imgObj}) ")
                    .WithFooter(efb => efb.WithText(type.ToString()));

                if (Uri.IsWellFormedUriString(imgObj.FileUrl, UriKind.Absolute))
                    embed.WithImageUrl(imgObj.FileUrl);
                else
                    _log.Error($"Image link from {type} is not a proper Url: {imgObj.FileUrl}");

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }
    }
}