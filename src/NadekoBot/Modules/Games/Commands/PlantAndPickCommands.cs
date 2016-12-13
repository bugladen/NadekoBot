using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    //todo make currency generation change and cooldown modifyable
    //only by bot owner through commands
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
        public class PlantPickCommands
        {
            private static ConcurrentHashSet<ulong> generationChannels { get; } = new ConcurrentHashSet<ulong>();
            //channelid/message
            private static ConcurrentDictionary<ulong, List<IUserMessage>> plantedFlowers { get; } = new ConcurrentDictionary<ulong, List<IUserMessage>>();
            //channelId/last generation
            private static ConcurrentDictionary<ulong, DateTime> lastGenerations { get; } = new ConcurrentDictionary<ulong, DateTime>();

            private static float chance { get; }
            private static int cooldown { get; }
            private static Logger _log { get; }

            static PlantPickCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                NadekoBot.Client.MessageReceived += PotentialFlowerGeneration;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.BotConfig.GetOrCreate();
                    var x =
                    generationChannels = new ConcurrentHashSet<ulong>(NadekoBot.AllGuildConfigs
                        .SelectMany(c => c.GenerateCurrencyChannelIds.Select(obj=>obj.ChannelId)));
                    chance = conf.CurrencyGenerationChance;
                    cooldown = conf.CurrencyGenerationCooldown;
                }
            }

            private static Task PotentialFlowerGeneration(IMessage imsg)
            {
                var msg = imsg as IUserMessage;
                if (msg == null || msg.IsAuthor() || msg.Author.IsBot)
                    return Task.CompletedTask;

                var channel = imsg.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;

                if (!generationChannels.Contains(channel.Id))
                    return Task.CompletedTask;

                var t = Task.Run(async () =>
                {
                    var lastGeneration = lastGenerations.GetOrAdd(channel.Id, DateTime.MinValue);
                    var rng = new NadekoRandom();

                    if (DateTime.Now - TimeSpan.FromSeconds(cooldown) < lastGeneration) //recently generated in this channel, don't generate again
                        return;

                    var num = rng.Next(1, 101) + chance * 100;

                    if (num > 100)
                    {
                        lastGenerations.AddOrUpdate(channel.Id, DateTime.Now, (id, old) => DateTime.Now);
                        try
                        {
                            var sent = await channel.SendFileAsync(
                                GetRandomCurrencyImagePath(), 
                                $"❗ A random { Gambling.Gambling.CurrencyName } appeared! Pick it up by typing `{NadekoBot.ModulePrefixes[typeof(Games).Name]}pick`")
                                    .ConfigureAwait(false);
                            plantedFlowers.AddOrUpdate(channel.Id, new List<IUserMessage>() { sent }, (id, old) => { old.Add(sent); return old; });
                        }
                        catch { }
                        
                    }
                });
                return Task.CompletedTask;
            }
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Pick(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                if (!channel.Guild.GetCurrentUser().GetPermissions(channel).ManageMessages)
                {
                    await channel.SendErrorAsync("I need manage channel permissions in order to process this command.").ConfigureAwait(false);
                    return;
                }

                List<IUserMessage> msgs;

                try { await imsg.DeleteAsync().ConfigureAwait(false); } catch { }
                if (!plantedFlowers.TryRemove(channel.Id, out msgs))
                    return;

                await Task.WhenAll(msgs.Select(toDelete => toDelete.DeleteAsync())).ConfigureAwait(false);

                await CurrencyHandler.AddCurrencyAsync((IGuildUser)imsg.Author, "Picked flower(s).", msgs.Count, false).ConfigureAwait(false);
                var msg = await channel.SendConfirmAsync($"**{imsg.Author}** picked {msgs.Count}{Gambling.Gambling.CurrencySign}!").ConfigureAwait(false);
                var t = Task.Run(async () =>
                {
                    await Task.Delay(10000).ConfigureAwait(false);
                    try { await msg.DeleteAsync().ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                });
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Plant(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                var removed = await CurrencyHandler.RemoveCurrencyAsync((IGuildUser)imsg.Author, "Planted a flower.", 1, false).ConfigureAwait(false);
                if (!removed)
                {
                    await channel.SendErrorAsync($"You don't have any {Gambling.Gambling.CurrencyPluralName}.").ConfigureAwait(false);
                    return;
                }

                var file = GetRandomCurrencyImagePath();
                IUserMessage msg;
                var vowelFirst = new[] { 'a', 'e', 'i', 'o', 'u' }.Contains(Gambling.Gambling.CurrencyName[0]);
                
                var msgToSend = $"Oh how Nice! **{imsg.Author.Username}** planted {(vowelFirst ? "an" : "a")} {Gambling.Gambling.CurrencyName}. Pick it using {NadekoBot.ModulePrefixes[typeof(Games).Name]}pick";
                if (file == null)
                {
                    msg = await channel.SendConfirmAsync(Gambling.Gambling.CurrencySign).ConfigureAwait(false);
                }
                else
                {
                    msg = await channel.SendFileAsync(file, msgToSend).ConfigureAwait(false);
                }
                plantedFlowers.AddOrUpdate(channel.Id, new List<IUserMessage>() { msg }, (id, old) => { old.Add(msg); return old; });
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageMessages)]
            public async Task GenCurrency(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var guildConfig = uow.GuildConfigs.For(channel.Id, set => set.Include(gc => gc.GenerateCurrencyChannelIds));

                    var toAdd = new GCChannelId() { ChannelId = channel.Id };
                    if (!guildConfig.GenerateCurrencyChannelIds.Contains(toAdd))
                    {
                        guildConfig.GenerateCurrencyChannelIds.Add(toAdd);
                        generationChannels.Add(channel.Id);
                        enabled = true;
                    }
                    else
                    {
                        guildConfig.GenerateCurrencyChannelIds.Remove(toAdd);
                        generationChannels.TryRemove(channel.Id);
                        enabled = false;
                    }
                    await uow.CompleteAsync();
                }
                if (enabled)
                {
                    await channel.SendConfirmAsync("Currency generation enabled on this channel.").ConfigureAwait(false);
                }
                else
                {
                    await channel.SendConfirmAsync("Currency generation disabled on this channel.").ConfigureAwait(false);
                }
            }

            private static string GetRandomCurrencyImagePath()
            {
                var rng = new NadekoRandom();
                return Directory.GetFiles("data/currency_images").OrderBy(s => rng.Next()).FirstOrDefault();
            }

            int GetRandomNumber()
            {
                using (var rg = RandomNumberGenerator.Create())
                {
                    byte[] rno = new byte[4];
                    rg.GetBytes(rno);
                    int randomvalue = BitConverter.ToInt32(rno, 0);
                    return randomvalue;
                }
            }
        }
    }
}