using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Database;
using NadekoBot.Extensions;
using System.Linq;

namespace NadekoBot.Core.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IBotConfigProvider _config;
        private readonly DbService _db;
        private readonly IUser _bot;

        public CurrencyService(IBotConfigProvider config, DbService db, DiscordSocketClient c)
        {
            _config = config;
            _db = db;
            _bot = c.CurrentUser;
        }

        private CurrencyTransaction GetCurrencyTransaction(ulong userId, string reason, long amount) =>
            new CurrencyTransaction
            {
                Amount = amount,
                UserId = userId,
                Reason = reason ?? "-",
            };

        private bool InternalChange(ulong userId, string userName, string discrim, string avatar, string reason, long amount, bool gamble, IUnitOfWork uow)
        {
            var result = uow.DiscordUsers.TryUpdateCurrencyState(userId, userName, discrim, avatar, amount);
            if(result)
            {
                var t = GetCurrencyTransaction(userId, reason, amount);
                uow.CurrencyTransactions.Add(t);

                if(gamble)
                {
                    var t2 = GetCurrencyTransaction(_bot.Id, reason, -amount);
                    uow.CurrencyTransactions.Add(t2);
                    uow.DiscordUsers.TryUpdateCurrencyState(_bot.Id, _bot.Username, _bot.Discriminator, _bot.AvatarId, -amount, true);
                }
            }
            return result;
        }

        private async Task InternalAddAsync(ulong userId, string userName, string discrim, string avatar, string reason, long amount, bool gamble)
        {
            if (amount < 0)
            {
                throw new ArgumentException("You can't add negative amounts. Use RemoveAsync method for that.", nameof(amount));
            }

            using (var uow = _db.UnitOfWork)
            {
                InternalChange(userId, userName, discrim, avatar, reason, amount, gamble, uow);
                await uow.CompleteAsync();
            }
        }

        public Task AddAsync(ulong userId, string reason, long amount, bool gamble = false)
        {
            return InternalAddAsync(userId, null, null, null, reason, amount, gamble);
        }

        public async Task AddAsync(IUser user, string reason, long amount, bool sendMessage = false, bool gamble = false)
        {
            await InternalAddAsync(user.Id, user.Username, user.Discriminator, user.AvatarId, reason, amount, gamble);
            if(sendMessage)
            {
                try
                {
                    await (await user.GetOrCreateDMChannelAsync())
                        .EmbedAsync(new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle($"Received {_config.BotConfig.CurrencySign}")
                            .AddField("Amount", amount)
                            .AddField("Reason", reason));
                }
                catch
                {
                    // ignored
                }
            }
        }

        public async Task AddBulkAsync(IEnumerable<ulong> userIds, IEnumerable<string> reasons, IEnumerable<long> amounts, bool gamble = false)
        {
            ulong[] idArray = userIds as ulong[] ?? userIds.ToArray();
            string[] reasonArray = reasons as string[] ?? reasons.ToArray();
            long[] amountArray = amounts as long[] ?? amounts.ToArray();

            if (idArray.Length != reasonArray.Length || reasonArray.Length != amountArray.Length)
                throw new ArgumentException("Cannot perform bulk operation. Arrays are not of equal length.");
            
            using (var uow = _db.UnitOfWork)
            {
                for (int i = 0; i < idArray.Length; i++)
                {
                    //todo if there is a duplicate id, it will fail as 2 user objects for the same id will be created
                    InternalChange(idArray[i], null, null, null, reasonArray[i], amountArray[i], gamble, uow);
                }
                await uow.CompleteAsync();
            }
        }

        private async Task<bool> InternalRemoveAsync(ulong userId, string userName, string userDiscrim, string avatar, string reason, long amount, bool gamble = false)
        {
            if(amount < 0)
            {
                throw new ArgumentException("You can't remove negative amounts. Use AddAsync method for that.", nameof(amount));
            }

            bool result;
            using (var uow = _db.UnitOfWork)
            {
                result = InternalChange(userId, userName, userDiscrim, avatar, reason, -amount, gamble, uow);
                await uow.CompleteAsync();
            }
            return result;
        }

        public Task<bool> RemoveAsync(ulong userId, string reason, long amount, bool gamble = false)
        {
            return InternalRemoveAsync(userId, null, null, null, reason, amount, gamble);
        }

        public Task<bool> RemoveAsync(IUser user, string reason, long amount, bool sendMessage = false, bool gamble = false)
        {
            return InternalRemoveAsync(user.Id, user.Username, user.Discriminator,user.AvatarId, reason, amount, gamble);
        }
        //public async Task<bool> RemoveAsync(IUser author, string reason, long amount, bool sendMessage, bool gamble = false)
        //{
        //    var success = Remove(author.Id, reason, amount, gamble: gamble, user: author);

        //    if (success && sendMessage)
        //        try { await author.SendErrorAsync($"`You lost:` {amount} {_config.BotConfig.CurrencySign}\n`Reason:` {reason}").ConfigureAwait(false); } catch { }

        //    return success;
        //}

        //public bool Remove(ulong authorId, string reason, long amount, IUnitOfWork uow = null, bool gamble = false, IUser user = null)
        //{
        //    if (amount < 0)
        //        throw new ArgumentNullException(nameof(amount));

        //    if (uow == null)
        //    {
        //        using (uow = _db.UnitOfWork)
        //        {
        //            if (user != null)
        //                uow.DiscordUsers.GetOrCreate(user);
        //            var toReturn = InternalRemoveCurrency(authorId, reason, amount, uow, gamble);
        //            uow.Complete();
        //            return toReturn;
        //        }
        //    }

        //    if (user != null)
        //        uow.DiscordUsers.GetOrCreate(user);
        //    return InternalRemoveCurrency(authorId, reason, amount, uow, gamble);
        //}

        //private bool InternalRemoveCurrency(ulong authorId, string reason, long amount, IUnitOfWork uow, bool addToBot)
        //{
        //    var success = uow.DiscordUsers.TryUpdateCurrencyState(authorId, -amount);
        //    if (!success)
        //        return false;

        //    var transaction = new CurrencyTransaction()
        //    {
        //        UserId = authorId,
        //        Reason = reason,
        //        Amount = -amount,
        //    };

        //    if (addToBot)
        //    {
        //        var botTr = transaction.Clone();
        //        botTr.UserId = _botId;
        //        botTr.Amount *= -1;

        //        uow.DiscordUsers.TryUpdateCurrencyState(_botId, amount);
        //        uow.CurrencyTransactions.Add(botTr);
        //    }

        //    uow.CurrencyTransactions.Add(transaction);
        //    return true;
        //}

        //public async Task AddToManyAsync(string reason, long amount, params ulong[] userIds)
        //{
        //    using (var uow = _db.UnitOfWork)
        //    {
        //        foreach (var userId in userIds)
        //        {
        //            var transaction = new CurrencyTransaction()
        //            {
        //                UserId = userId,
        //                Reason = reason,
        //                Amount = amount,
        //            };
        //            uow.DiscordUsers.TryUpdateCurrencyState(userId, amount);
        //            uow.CurrencyTransactions.Add(transaction);
        //        }

        //        await uow.CompleteAsync();
        //    }
        //}

        //public async Task AddAsync(IUser author, string reason, long amount, bool sendMessage, string note = null, bool gamble = false)
        //{
        //    await AddAsync(author.Id, reason, amount, gamble: gamble, user: author);

        //    if (sendMessage)
        //        try { await author.SendConfirmAsync($"`You received:` {amount} {_config.BotConfig.CurrencySign}\n`Reason:` {reason}\n`Note:`{(note ?? "-")}").ConfigureAwait(false); } catch { }
        //}

        //public async Task AddAsync(ulong receiverId, string reason, long amount, IUnitOfWork uow = null, bool gamble = false, IUser user = null)
        //{
        //    if (amount < 0)
        //        throw new ArgumentNullException(nameof(amount));

        //    var transaction = new CurrencyTransaction()
        //    {
        //        UserId = receiverId,
        //        Reason = reason,
        //        Amount = amount,
        //    };

        //    if (uow == null)
        //        using (uow = _db.UnitOfWork)
        //        {
        //            if (user != null)
        //                uow.DiscordUsers.GetOrCreate(user);
        //            uow.DiscordUsers.TryUpdateCurrencyState(receiverId, amount);
        //            if (gamble)
        //            {
        //                var botTr = transaction.Clone();
        //                botTr.UserId = _botId;
        //                botTr.Amount *= -1;

        //                uow.DiscordUsers.TryUpdateCurrencyState(_botId, -amount, true);
        //                uow.CurrencyTransactions.Add(botTr);
        //            }
        //            uow.CurrencyTransactions.Add(transaction);
        //            await uow.CompleteAsync();
        //        }
        //    else
        //    {
        //        uow.DiscordUsers.TryUpdateCurrencyState(receiverId, amount);
        //        if (gamble)
        //        {
        //            var botTr = transaction.Clone();
        //            botTr.UserId = _botId;
        //            botTr.Amount *= -1;

        //            uow.DiscordUsers.TryUpdateCurrencyState(_botId, -amount, true);
        //            uow.CurrencyTransactions.Add(botTr);
        //        }
        //        uow.CurrencyTransactions.Add(transaction);
        //    }
        //}
    }
}
