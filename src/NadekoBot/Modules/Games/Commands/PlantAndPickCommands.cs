using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Games;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        /// <summary>
        /// Flower picking/planting idea is given to me by its
        /// inceptor Violent Crumble from Game Developers League discord server
        /// (he has !cookie and !nom) Thanks a lot Violent!
        /// Check out GDL (its a growing gamedev community):
        /// https://discord.gg/0TYNJfCU4De7YIk8
        /// </summary>
        [Group]
        public class PlantPickCommands : NadekoSubmodule
        {
            private readonly CurrencyHandler _ch;
            private readonly BotConfig _bc;
            private readonly GamesService _games;
            private readonly DbHandler _db;

            public PlantPickCommands(BotConfig bc, CurrencyHandler ch, GamesService games,
                DbHandler db)
            {
                _bc = bc;
                _ch = ch;
                _games = games;
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Pick()
            {
                var channel = (ITextChannel)Context.Channel;

                if (!(await channel.Guild.GetCurrentUserAsync()).GetPermissions(channel).ManageMessages)
                    return;


                try { await Context.Message.DeleteAsync().ConfigureAwait(false); } catch { }
                if (!_games.PlantedFlowers.TryRemove(channel.Id, out List<IUserMessage> msgs))
                    return;

                await Task.WhenAll(msgs.Where(m => m != null).Select(toDelete => toDelete.DeleteAsync())).ConfigureAwait(false);

                await _ch.AddCurrencyAsync((IGuildUser)Context.User, $"Picked {_bc.CurrencyPluralName}", msgs.Count, false).ConfigureAwait(false);
                var msg = await ReplyConfirmLocalized("picked", msgs.Count + _bc.CurrencySign)
                    .ConfigureAwait(false);
                msg.DeleteAfter(10);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Plant(int amount = 1)
            {
                if (amount < 1)
                    return;

                var removed = await _ch.RemoveCurrencyAsync((IGuildUser)Context.User, $"Planted a {_bc.CurrencyName}", amount, false).ConfigureAwait(false);
                if (!removed)
                {
                    await ReplyErrorLocalized("not_enough", _bc.CurrencySign).ConfigureAwait(false);
                    return;
                }

                var imgData = _games.GetRandomCurrencyImage();

                //todo upload all currency images to transfer.sh and use that one as cdn
                //and then 

                var msgToSend = GetText("planted",
                    Format.Bold(Context.User.ToString()),
                    amount + _bc.CurrencySign,
                    Prefix);

                if (amount > 1)
                    msgToSend += " " + GetText("pick_pl", Prefix);
                else
                    msgToSend += " " + GetText("pick_sn", Prefix);

                IUserMessage msg;
                using (var toSend = imgData.Value.ToStream())
                {
                    msg = await Context.Channel.SendFileAsync(toSend, imgData.Key, msgToSend).ConfigureAwait(false);
                }

                var msgs = new IUserMessage[amount];
                msgs[0] = msg;

                _games.PlantedFlowers.AddOrUpdate(Context.Channel.Id, msgs.ToList(), (id, old) =>
                {
                    old.AddRange(msgs);
                    return old;
                });
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
#if GLOBAL_NADEKO
            [OwnerOnly]
#endif
            public async Task GenCurrency()
            {
                var channel = (ITextChannel)Context.Channel;

                bool enabled;
                using (var uow = _db.UnitOfWork)
                {
                    var guildConfig = uow.GuildConfigs.For(channel.Id, set => set.Include(gc => gc.GenerateCurrencyChannelIds));

                    var toAdd = new GCChannelId() { ChannelId = channel.Id };
                    if (!guildConfig.GenerateCurrencyChannelIds.Contains(toAdd))
                    {
                        guildConfig.GenerateCurrencyChannelIds.Add(toAdd);
                        _games.GenerationChannels.Add(channel.Id);
                        enabled = true;
                    }
                    else
                    {
                        guildConfig.GenerateCurrencyChannelIds.Remove(toAdd);
                        _games.GenerationChannels.TryRemove(channel.Id);
                        enabled = false;
                    }
                    await uow.CompleteAsync();
                }
                if (enabled)
                {
                    await ReplyConfirmLocalized("curgen_enabled").ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("curgen_disabled").ConfigureAwait(false);
                }
            }
        }
    }
}