using System;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Database;

namespace NadekoBot.Services
{
    public static class CurrencyHandler
    {
        public static async Task<bool> RemoveCurrencyAsync(IUser author, string reason, long amount, bool sendMessage)
        {
            var success = await RemoveCurrencyAsync(author.Id, reason, amount);

            if (success && sendMessage)
                try { await author.SendErrorAsync($"`You lost:` {amount} {NadekoBot.BotConfig.CurrencySign}\n`Reason:` {reason}").ConfigureAwait(false); } catch { }

            return success;
        }

        public static async Task<bool> RemoveCurrencyAsync(ulong authorId, string reason, long amount, IUnitOfWork uow = null)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));


            if (uow == null)
            {
                using (uow = DbHandler.UnitOfWork())
                {
                    var toReturn = InternalRemoveCurrency(authorId, reason, amount, uow);
                    await uow.CompleteAsync().ConfigureAwait(false);
                    return toReturn;
                }
            }

            return InternalRemoveCurrency(authorId, reason, amount, uow);
        }

        private static bool InternalRemoveCurrency(ulong authorId, string reason, long amount, IUnitOfWork uow)
        {
            var success = uow.Currency.TryUpdateState(authorId, -amount);
            if (!success)
                return false;
            uow.CurrencyTransactions.Add(new CurrencyTransaction()
            {
                UserId = authorId,
                Reason = reason,
                Amount = -amount,
            });
            return true;
        }

        public static async Task AddCurrencyAsync(IUser author, string reason, long amount, bool sendMessage)
        {
            await AddCurrencyAsync(author.Id, reason, amount);

            if (sendMessage)
                try { await author.SendConfirmAsync($"`You received:` {amount} {NadekoBot.BotConfig.CurrencySign}\n`Reason:` {reason}").ConfigureAwait(false); } catch { }
        }

        public static async Task AddCurrencyAsync(ulong receiverId, string reason, long amount, IUnitOfWork uow = null)
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
                using (uow = DbHandler.UnitOfWork())
                {
                    uow.Currency.TryUpdateState(receiverId, amount);
                    uow.CurrencyTransactions.Add(transaction);
                    await uow.CompleteAsync();
                }
            else
            {
                uow.Currency.TryUpdateState(receiverId, amount);
                uow.CurrencyTransactions.Add(transaction);
            }
        }
    }
}
