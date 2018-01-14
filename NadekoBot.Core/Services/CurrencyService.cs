using System;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Database;
using Discord.WebSocket;

namespace NadekoBot.Core.Services
{
    public class CurrencyService : INService
    {
        private readonly IBotConfigProvider _config;
        private readonly DbService _db;
        private readonly ulong _botId;

        public CurrencyService(IBotConfigProvider config, DbService db, DiscordSocketClient c)
        {
            _config = config;
            _db = db;
            _botId = c.CurrentUser.Id;
        }

        public async Task<bool> RemoveAsync(IUser author, string reason, long amount, bool sendMessage, bool gamble = false)
        {
            var success = Remove(author.Id, reason, amount, gamble: gamble, user: author);

            if (success && sendMessage)
                try { await author.SendErrorAsync($"`You lost:` {amount} {_config.BotConfig.CurrencySign}\n`Reason:` {reason}").ConfigureAwait(false); } catch { }

            return success;
        }

        public bool Remove(ulong authorId, string reason, long amount, IUnitOfWork uow = null, bool gamble = false, IUser user = null)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));
            
            if (uow == null)
            {
                using (uow = _db.UnitOfWork)
                {
                    if (user != null)
                        uow.DiscordUsers.GetOrCreate(user);
                    var toReturn = InternalRemoveCurrency(authorId, reason, amount, uow, gamble);
                    uow.Complete();
                    return toReturn;
                }
            }

            if (user != null)
                uow.DiscordUsers.GetOrCreate(user);
            return InternalRemoveCurrency(authorId, reason, amount, uow, gamble);
        }

        private bool InternalRemoveCurrency(ulong authorId, string reason, long amount, IUnitOfWork uow, bool addToBot)
        {
            var success = uow.DiscordUsers.TryUpdateCurrencyState(authorId, -amount);
            if (!success)
                return false;

            var transaction = new CurrencyTransaction()
            {
                UserId = authorId,
                Reason = reason,
                Amount = -amount,
            };

            if (addToBot)
            {
                var botTr = transaction.Clone();
                botTr.UserId = _botId;
                botTr.Amount *= -1;

                uow.DiscordUsers.TryUpdateCurrencyState(_botId, amount);
                uow.CurrencyTransactions.Add(botTr);
            }

            uow.CurrencyTransactions.Add(transaction);
            return true;
        }

        public async Task AddToManyAsync(string reason, long amount, params ulong[] userIds)
        {
            using (var uow = _db.UnitOfWork)
            {
                foreach (var userId in userIds)
                {
                    var transaction = new CurrencyTransaction()
                    {
                        UserId = userId,
                        Reason = reason,
                        Amount = amount,
                    };
                    uow.DiscordUsers.TryUpdateCurrencyState(userId, amount);
                    uow.CurrencyTransactions.Add(transaction);
                }

                await uow.CompleteAsync();
            }
        }

        public async Task AddAsync(IUser author, string reason, long amount, bool sendMessage, string note = null, bool gamble = false)
        {
            await AddAsync(author.Id, reason, amount, gamble: gamble, user: author);

            if (sendMessage)
                try { await author.SendConfirmAsync($"`You received:` {amount} {_config.BotConfig.CurrencySign}\n`Reason:` {reason}\n`Note:`{(note ?? "-")}").ConfigureAwait(false); } catch { }
        }

        public async Task AddAsync(ulong receiverId, string reason, long amount, IUnitOfWork uow = null, bool gamble = false, IUser user = null)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));

            var transaction = new CurrencyTransaction()
            {
                UserId = receiverId,
                Reason = reason,
                Amount = amount,
            };

            if (uow == null)
                using (uow = _db.UnitOfWork)
                {
                    if (user != null)
                        uow.DiscordUsers.GetOrCreate(user);
                    uow.DiscordUsers.TryUpdateCurrencyState(receiverId, amount);
                    if (gamble)
                    {
                        var botTr = transaction.Clone();
                        botTr.UserId = _botId;
                        botTr.Amount *= -1;

                        uow.DiscordUsers.TryUpdateCurrencyState(_botId, -amount, true);
                        uow.CurrencyTransactions.Add(botTr);
                    }
                    uow.CurrencyTransactions.Add(transaction);
                    await uow.CompleteAsync();
                }
            else
            {
                uow.DiscordUsers.TryUpdateCurrencyState(receiverId, amount);
                if (gamble)
                {
                    var botTr = transaction.Clone();
                    botTr.UserId = _botId;
                    botTr.Amount *= -1;

                    uow.DiscordUsers.TryUpdateCurrencyState(_botId, -amount, true);
                    uow.CurrencyTransactions.Add(botTr);
                }
                uow.CurrencyTransactions.Add(transaction);
            }
        }
    }
}
