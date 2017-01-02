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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        public class PlantPickCommands : ModuleBase
        {
            private static ConcurrentHashSet<ulong> generationChannels { get; } = new ConcurrentHashSet<ulong>();
            //channelid/message
            private static ConcurrentDictionary<ulong, List<IUserMessage>> plantedFlowers { get; } = new ConcurrentDictionary<ulong, List<IUserMessage>>();
            //channelId/last generation
            private static ConcurrentDictionary<ulong, DateTime> lastGenerations { get; } = new ConcurrentDictionary<ulong, DateTime>();

            private static ConcurrentHashSet<ulong> usersRecentlyPicked { get; } = new ConcurrentHashSet<ulong>();

            private static float chance { get; }
            private static int cooldown { get; }
            private static Logger _log { get; }

            static PlantPickCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();
#if !GLOBAL_NADEKO
                NadekoBot.Client.MessageReceived += PotentialFlowerGeneration;
#endif
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.BotConfig.GetOrCreate();
                    var x =
                    generationChannels = new ConcurrentHashSet<ulong>(NadekoBot.AllGuildConfigs
                        .SelectMany(c => c.GenerateCurrencyChannelIds.Select(obj => obj.ChannelId)));
                    chance = conf.CurrencyGenerationChance;
                    cooldown = conf.CurrencyGenerationCooldown;
                }

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }

            private static async void PotentialFlowerGeneration(SocketMessage imsg)
            {
                try
                {
                    var msg = imsg as SocketUserMessage;
                    if (msg == null || msg.IsAuthor() || msg.Author.IsBot)
                        return;

                    var channel = imsg.Channel as ITextChannel;
                    if (channel == null)
                        return;

                    if (!generationChannels.Contains(channel.Id))
                        return;

                    var lastGeneration = lastGenerations.GetOrAdd(channel.Id, DateTime.MinValue);
                    var rng = new NadekoRandom();

                    if (DateTime.Now - TimeSpan.FromSeconds(cooldown) < lastGeneration) //recently generated in this channel, don't generate again
                        return;

                    var num = rng.Next(1, 101) + chance * 100;

                    if (num > 100)
                    {
                        lastGenerations.AddOrUpdate(channel.Id, DateTime.Now, (id, old) => DateTime.Now);

                        var sent = await channel.SendFileAsync(
                            GetRandomCurrencyImagePath(),
                            $"❗ A random { Gambling.Gambling.CurrencyName } appeared! Pick it up by typing `{NadekoBot.ModulePrefixes[typeof(Games).Name]}pick`")
                                .ConfigureAwait(false);
                        plantedFlowers.AddOrUpdate(channel.Id, new List<IUserMessage>() { sent }, (id, old) => { old.Add(sent); return old; });


                    }
                }
                catch { }
            }
#if !GLOBAL_NADEKO
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Pick()
            {
                var channel = (ITextChannel)Context.Channel;

                if (!(await channel.Guild.GetCurrentUserAsync()).GetPermissions(channel).ManageMessages || !usersRecentlyPicked.Add(Context.User.Id))
                    return;

                try
                {

                    List<IUserMessage> msgs;

                try { await Context.Message.DeleteAsync().ConfigureAwait(false); } catch { }
                if (!plantedFlowers.TryRemove(channel.Id, out msgs))
                    return;

                    await Task.WhenAll(msgs.Select(toDelete => toDelete.DeleteAsync())).ConfigureAwait(false);

                    await CurrencyHandler.AddCurrencyAsync((IGuildUser)Context.User, "Picked flower(s).", msgs.Count, false).ConfigureAwait(false);
                    var msg = await channel.SendConfirmAsync($"**{Context.User}** picked {msgs.Count}{Gambling.Gambling.CurrencySign}!").ConfigureAwait(false);
                    msg.DeleteAfter(10);
                }
                finally
                {
                    await Task.Delay(60000);
                    usersRecentlyPicked.TryRemove(Context.User.Id);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Plant()
            {
                var removed = await CurrencyHandler.RemoveCurrencyAsync((IGuildUser)Context.User, "Planted a flower.", 1, false).ConfigureAwait(false);
                if (!removed)
                {
                    await Context.Channel.SendErrorAsync($"You don't have any {Gambling.Gambling.CurrencyPluralName}.").ConfigureAwait(false);
                    return;
                }

                var file = GetRandomCurrencyImagePath();
                IUserMessage msg;
                var vowelFirst = new[] { 'a', 'e', 'i', 'o', 'u' }.Contains(Gambling.Gambling.CurrencyName[0]);
                
                var msgToSend = $"Oh how Nice! **{Context.User.Username}** planted {(vowelFirst ? "an" : "a")} {Gambling.Gambling.CurrencyName}. Pick it using {NadekoBot.ModulePrefixes[typeof(Games).Name]}pick";
                if (file == null)
                {
                    msg = await Context.Channel.SendConfirmAsync(Gambling.Gambling.CurrencySign).ConfigureAwait(false);
                }
                else
                {
                    msg = await Context.Channel.SendFileAsync(file, msgToSend).ConfigureAwait(false);
                }
                plantedFlowers.AddOrUpdate(Context.Channel.Id, new List<IUserMessage>() { msg }, (id, old) => { old.Add(msg); return old; });
            }
#endif
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task GenCurrency()
            {
                var channel = (ITextChannel)Context.Channel;

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