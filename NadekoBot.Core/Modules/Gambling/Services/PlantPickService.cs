﻿using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common;
using NadekoBot.Common.Collections;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Database.Repositories;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Extensions;
using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Drawing.Brushes;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Gambling.Services
{
    public class PlantPickService : INService
    {
        private readonly DbService _db;
        private readonly NadekoStrings _strings;
        private readonly IImageCache _images;
        private readonly FontProvider _fonts;
        private readonly IBotConfigProvider _bc;
        private readonly Logger _log;
        private readonly ICurrencyService _cs;
        private readonly CommandHandler _cmdHandler;
        private readonly NadekoRandom _rng;
        private readonly DiscordSocketClient _client;
        public readonly ConcurrentHashSet<ulong> _generationChannels = new ConcurrentHashSet<ulong>();
        //channelId/last generation
        public ConcurrentDictionary<ulong, DateTime> LastGenerations { get; } = new ConcurrentDictionary<ulong, DateTime>();
        private readonly SemaphoreSlim pickLock = new SemaphoreSlim(1, 1);

        public PlantPickService(DbService db, CommandHandler cmd, NadekoBot bot, NadekoStrings strings,
            IDataCache cache, FontProvider fonts, IBotConfigProvider bc, ICurrencyService cs,
            CommandHandler cmdHandler, DiscordSocketClient client)
        {
            _db = db;
            _strings = strings;
            _images = cache.LocalImages;
            _fonts = fonts;
            _bc = bc;
            _log = LogManager.GetCurrentClassLogger();
            _cs = cs;
            _cmdHandler = cmdHandler;
            _rng = new NadekoRandom();
            _client = client;

            cmd.OnMessageNoTrigger += PotentialFlowerGeneration;

            _generationChannels = new ConcurrentHashSet<ulong>(bot
                .AllGuildConfigs
                .SelectMany(c => c.GenerateCurrencyChannelIds.Select(obj => obj.ChannelId)));
        }

        private string GetText(ulong gid, string key, params object[] rep)
            => _strings.GetText(key, gid, "Games".ToLowerInvariant(), rep);

        public bool ToggleCurrencyGeneration(ulong gid, ulong cid)
        {
            bool enabled;
            using (var uow = _db.GetDbContext())
            {
                var guildConfig = uow.GuildConfigs.ForId(gid, set => set.Include(gc => gc.GenerateCurrencyChannelIds));

                var toAdd = new GCChannelId() { ChannelId = cid };
                if (!guildConfig.GenerateCurrencyChannelIds.Contains(toAdd))
                {
                    guildConfig.GenerateCurrencyChannelIds.Add(toAdd);
                    _generationChannels.Add(cid);
                    enabled = true;
                }
                else
                {
                    var toDelete = guildConfig.GenerateCurrencyChannelIds.FirstOrDefault(x => x.Equals(toAdd));
                    if (toDelete != null)
                    {
                        uow._context.Remove(toDelete);
                    }
                    _generationChannels.TryRemove(cid);
                    enabled = false;
                }
                uow.SaveChanges();
            }
            return enabled;
        }

        public IEnumerable<GeneratingChannel> GetAllGeneratingChannels()
        {
            using (var uow = _db.GetDbContext())
            {
                var chs = uow.GuildConfigs.GetGeneratingChannels();
                return chs;
            }
        }

        /// <summary>
        /// Get a random currency image stream, with an optional password sticked onto it.
        /// </summary>
        /// <param name="pass">Optional password to add to top left corner.</param>
        /// <returns>Stream of the currency image</returns>
        public Stream GetRandomCurrencyImage(string pass, out string extension)
        {
            // get a random currency image bytes
            var rng = new NadekoRandom();
            var curImg = _images.Currency[rng.Next(0, _images.Currency.Count)];

            if (string.IsNullOrWhiteSpace(pass))
            {
                // determine the extension
                using (var img = Image.Load(curImg, out var format))
                {
                    extension = format.FileExtensions.FirstOrDefault() ?? "png";
                }
                // return the image
                return curImg.ToStream();
            }

            // get the image stream and extension
            var (s, ext) = AddPassword(curImg, pass);
            // set the out extension parameter to the extension we've got
            extension = ext;
            // return the image
            return s;
        }

        /// <summary>
        /// Add a password to the image.
        /// </summary>
        /// <param name="curImg">Image to add password to.</param>
        /// <param name="pass">Password to add to top left corner.</param>
        /// <returns>Image with the password in the top left corner.</returns>
        private (Stream, string) AddPassword(byte[] curImg, string pass)
        {
            // draw lower, it looks better
            pass = pass.TrimTo(10, true).ToLowerInvariant();
            using (var img = Image.Load(curImg, out var format))
            {
                // choose font size based on the image height, so that it's visible
                var font = _fonts.NotoSans.CreateFont(img.Height / 12, FontStyle.Bold);
                img.Mutate(x =>
                {
                    // measure the size of the text to be drawing
                    var size = TextMeasurer.Measure(pass, new RendererOptions(font, new PointF(0, 0)));

                    // fill the background with black, add 5 pixels on each side to make it look better
                    x.FillPolygon(Rgba32.FromHex("00000080"),
                        new PointF(0, 0),
                        new PointF(size.Width + 5, 0),
                        new PointF(size.Width + 5, size.Height + 10),
                        new PointF(0, size.Height + 10));

                    // draw the password over the background
                    x.DrawText(pass,
                        font,
                        Brushes.Solid(Rgba32.White),
                        new PointF(0, 0));
                });
                // return image as a stream for easy sending
                return (img.ToStream(format), format.FileExtensions.FirstOrDefault() ?? "png");
            }
        }

        private Task PotentialFlowerGeneration(IUserMessage imsg)
        {
            var msg = imsg as SocketUserMessage;
            if (msg == null || msg.Author.IsBot)
                return Task.CompletedTask;

            if (!(imsg.Channel is ITextChannel channel))
                return Task.CompletedTask;

            if (!_generationChannels.Contains(channel.Id))
                return Task.CompletedTask;

            var _ = Task.Run(async () =>
            {
                try
                {
                    var lastGeneration = LastGenerations.GetOrAdd(channel.Id, DateTime.MinValue);
                    var rng = new NadekoRandom();

                    if (DateTime.UtcNow - TimeSpan.FromSeconds(_bc.BotConfig.CurrencyGenerationCooldown) < lastGeneration) //recently generated in this channel, don't generate again
                        return;

                    var num = rng.Next(1, 101) + _bc.BotConfig.CurrencyGenerationChance * 100;
                    if (num > 100 && LastGenerations.TryUpdate(channel.Id, DateTime.UtcNow, lastGeneration))
                    {
                        var dropAmount = _bc.BotConfig.CurrencyDropAmount;
                        var dropAmountMax = _bc.BotConfig.CurrencyDropAmountMax;

                        if (dropAmountMax != null && dropAmountMax > dropAmount)
                            dropAmount = new NadekoRandom().Next(dropAmount, dropAmountMax.Value + 1);

                        if (dropAmount > 0)
                        {
                            var prefix = _cmdHandler.GetPrefix(channel.Guild.Id);
                            var toSend = dropAmount == 1
                                ? GetText(channel.GuildId, "curgen_sn", _bc.BotConfig.CurrencySign)
                                    + " " + GetText(channel.GuildId, "pick_sn", prefix)
                                : GetText(channel.GuildId, "curgen_pl", dropAmount, _bc.BotConfig.CurrencySign)
                                    + " " + GetText(channel.GuildId, "pick_pl", prefix);

                            var pw = _bc.BotConfig.CurrencyGenerationPassword ? GenerateCurrencyPassword().ToUpperInvariant() : null;

                            IUserMessage sent;
                            using (var stream = GetRandomCurrencyImage(pw, out var ext))
                            {
                                sent = await channel.SendFileAsync(stream, $"currency_image.{ext}", toSend).ConfigureAwait(false);
                            }

                            await AddPlantToDatabase(channel.GuildId,
                                channel.Id,
                                _client.CurrentUser.Id,
                                sent.Id,
                                dropAmount,
                                pw).ConfigureAwait(false);
                        }
                    }
                }
                catch
                {
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Generate a hexadecimal string from 1000 to ffff.
        /// </summary>
        /// <returns>A hexadecimal string from 1000 to ffff</returns>
        private string GenerateCurrencyPassword()
        {
            // generate a number from 1000 to ffff
            var num = _rng.Next(4096, 65536);
            // convert it to hexadecimal
            return num.ToString("X");
        }

        public async Task<long> PickAsync(ulong gid, ITextChannel ch, ulong uid, string pass)
        {
            await pickLock.WaitAsync();
            try
            {
                long amount;
                ulong[] ids;
                using (var uow = _db.GetDbContext())
                {
                    // this method will sum all plants with that password, 
                    // remove them, and get messageids of the removed plants

                    (amount, ids) = uow.PlantedCurrency.RemoveSumAndGetMessageIdsFor(ch.Id, pass);


                    if (amount > 0)
                    {
                        // give the picked currency to the user
                        await _cs.AddAsync(uid, "Picked currency", amount, gamble: false);
                    }
                    uow.SaveChanges();
                }

                try
                {
                    // delete all of the plant messages which have just been picked
                    var _ = ch.DeleteMessagesAsync(ids);
                }
                catch { }

                // return the amount of currency the user picked
                return amount;
            }
            finally
            {
                pickLock.Release();
            }
        }

        public async Task<ulong?> SendPlantMessageAsync(ulong gid, IMessageChannel ch, string user, long amount, string pass)
        {
            try
            {
                // get the text
                var prefix = _cmdHandler.GetPrefix(gid);
                var msgToSend = GetText(gid,
                    "planted",
                    Format.Bold(user),
                    amount + _bc.BotConfig.CurrencySign,
                    prefix);

                if (amount > 1)
                    msgToSend += " " + GetText(gid, "pick_pl", prefix);
                else
                    msgToSend += " " + GetText(gid, "pick_sn", prefix);

                //get the image
                using (var stream = GetRandomCurrencyImage(pass, out var ext))
                {
                    // send it
                    var msg = await ch.SendFileAsync(stream, $"img.{ext}", msgToSend).ConfigureAwait(false);
                    // return sent message's id (in order to be able to delete it when it's picked)
                    return msg.Id;
                }
            }
            catch
            {
                // if sending fails, return null as message id
                return null;
            }
        }

        public async Task<bool> PlantAsync(ulong gid, IMessageChannel ch, ulong uid, string user, long amount, string pass)
        {
            // normalize it - no more than 10 chars, uppercase 
            pass = pass?.Trim().TrimTo(10, hideDots: true).ToUpperInvariant();
            // has to be either null or alphanumeric
            if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric())
                return false;

            // remove currency from the user who's planting
            if (await _cs.RemoveAsync(uid, "Planted currency", amount, gamble: false))
            {
                // try to send the message with the currency image
                var msgId = await SendPlantMessageAsync(gid, ch, user, amount, pass).ConfigureAwait(false);
                if (msgId == null)
                {
                    // if it fails it will return null, if it returns null, refund
                    await _cs.AddAsync(uid, "Planted currency refund", amount, gamble: false);
                    return false;
                }
                // if it doesn't fail, put the plant in the database for other people to pick
                await AddPlantToDatabase(gid, ch.Id, uid, msgId.Value, amount, pass).ConfigureAwait(false);
                return true;
            }
            // if user doesn't have enough currency, fail
            return false;
        }

        private async Task AddPlantToDatabase(ulong gid, ulong cid, ulong uid, ulong mid, long amount, string pass)
        {
            using (var uow = _db.GetDbContext())
            {
                uow.PlantedCurrency.Add(new PlantedCurrency
                {
                    Amount = amount,
                    GuildId = gid,
                    ChannelId = cid,
                    Password = pass,
                    UserId = uid,
                    MessageId = mid,
                });
                await uow.SaveChangesAsync();
            }
        }
    }
}
