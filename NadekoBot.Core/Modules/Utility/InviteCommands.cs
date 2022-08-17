﻿using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Common;
using NadekoBot.Core.Modules.Utility.Services;
using NadekoBot.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class InviteCommands : NadekoSubmodule<InviteService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [BotPerm(ChannelPerm.CreateInstantInvite)]
            [UserPerm(ChannelPerm.CreateInstantInvite)]
            [NadekoOptions(typeof(InviteService.Options))]
            public async Task InviteCreate(params string[] args)
            {
                var (opts, success) = OptionsParser.ParseFrom(new InviteService.Options(), args);
                if (!success)
                    return;

                var ch = (ITextChannel)ctx.Channel;
                var invite = await ch.CreateInviteAsync(opts.Expire, opts.MaxUses, isTemporary: opts.Temporary, isUnique: opts.Unique).ConfigureAwait(false);

                await ctx.Channel.SendConfirmAsync($"{ctx.User.Mention} https://discord.gg/{invite.Code}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [BotPerm(ChannelPerm.ManageChannels)]
            [UserPerm(ChannelPerm.ManageChannels)]
            public async Task InviteList(int page = 1, [Leftover]ITextChannel ch = null)
            {
                if (--page < 0)
                    return;
                var channel = ch ?? (ITextChannel)ctx.Channel;

                var invites = await channel.GetInvitesAsync().ConfigureAwait(false);

                await ctx.SendPaginatedConfirmAsync(page, (cur) =>
                {
                    var i = 1;
                    var invs = invites.Skip(cur * 9).Take(9);
                    if (!invs.Any())
                    {
                        return new EmbedBuilder()
                            .WithErrorColor()
                            .WithDescription(GetText("no_invites"));
                    }
                    return invs.Aggregate(new EmbedBuilder().WithOkColor(),
                        (acc, inv) => acc.AddField(
                            $"#{i++} {inv.Inviter.ToString().TrimTo(15)} " +
                            $"({inv.Uses} / {(inv.MaxUses == 0 ? "∞" : inv.MaxUses?.ToString())})",
                            inv.Url));
                }, invites.Count, 9).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [BotPerm(ChannelPerm.ManageChannels)]
            [UserPerm(ChannelPerm.ManageChannels)]
            public async Task InviteDelete(int index)
            {
                if (--index < 0)
                    return;
                var ch = (ITextChannel)ctx.Channel;

                var invites = await ch.GetInvitesAsync().ConfigureAwait(false);

                if (invites.Count <= index)
                    return;
                var inv = invites.ElementAt(index);
                await inv.DeleteAsync().ConfigureAwait(false);

                await ReplyAsync(GetText("invite_deleted", Format.Bold(inv.Code.ToString()))).ConfigureAwait(false);
            }
        }
    }
}
