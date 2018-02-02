using Discord;
using NadekoBot.Core.Services.Database.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NadekoBot.Extensions
{
    public static class IUserExtensions
    {
        public static async Task<IUserMessage> SendConfirmAsync(this IUser user, string text)
             => await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: new EmbedBuilder().WithOkColor().WithDescription(text).Build());

        public static async Task<IUserMessage> SendConfirmAsync(this IUser user, string title, string text, string url = null)
        {
            var eb = new EmbedBuilder().WithOkColor().WithDescription(text);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            return await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: eb.Build());
        }

        public static async Task<IUserMessage> SendErrorAsync(this IUser user, string title, string error, string url = null)
        {
            var eb = new EmbedBuilder().WithErrorColor().WithDescription(error);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            return await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: eb.Build());
        }

        public static async Task<IUserMessage> SendErrorAsync(this IUser user, string error)
             => await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: new EmbedBuilder().WithErrorColor().WithDescription(error).Build());

        public static async Task<IUserMessage> SendFileAsync(this IUser user, string filePath, string caption = null, string text = null, bool isTTS = false) =>
            await (await user.GetOrCreateDMChannelAsync().ConfigureAwait(false)).SendFileAsync(File.Open(filePath, FileMode.Open), caption ?? "x", text, isTTS).ConfigureAwait(false);

        public static async Task<IUserMessage> SendFileAsync(this IUser user, Stream fileStream, string fileName, string caption = null, bool isTTS = false) =>
            await (await user.GetOrCreateDMChannelAsync().ConfigureAwait(false)).SendFileAsync(fileStream, fileName, caption, isTTS).ConfigureAwait(false);

        public static string RealAvatarUrl(this IUser usr) =>
            usr.AvatarId == null 
                ? null
                : usr.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{usr.Id}/{usr.AvatarId}.gif"
                    : usr.GetAvatarUrl(ImageFormat.Auto);

        public static string RealAvatarUrl(this DiscordUser usr) =>
            usr.AvatarId == null
                ? null
                : usr.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{usr.UserId}/{usr.AvatarId}.gif"
                    : $"{DiscordConfig.CDNUrl}avatars/{usr.UserId}/{usr.AvatarId}.png";
    }
}
