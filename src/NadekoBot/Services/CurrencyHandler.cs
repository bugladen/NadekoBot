using System;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services
{
    public static class CurrencyHandler
    {
        public static async Task<bool> RemoveCurrencyAsync(IGuildUser author, string reason, long amount, bool sendMessage)
        {
            var success = await RemoveCurrencyAsync(author.Id, reason, amount);

            if (success && sendMessage)
                try { await author.SendErrorAsync($"`You lost:` {amount} {Gambling.CurrencySign}\n`Reason:` {reason}").ConfigureAwait(false); } catch { }

            return success;
        }

        public static async Task<bool> RemoveCurrencyAsync(ulong authorId, string reason, long amount)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));


            using (var uow = DbHandler.UnitOfWork())
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
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            return true;
        }

        public static async Task AddCurrencyAsync(IGuildUser author, string reason, long amount, bool sendMessage)
        {
            await AddCurrencyAsync(author.Id, reason, amount);

            if (sendMessage)
                try { await author.SendConfirmAsync($"`You received:` {amount} {Gambling.CurrencySign}\n`Reason:` {reason}").ConfigureAwait(false); } catch { }
        }

        public static async Task AddCurrencyAsync(ulong receiverId, string reason, long amount)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));


            using (var uow = DbHandler.UnitOfWork())
            {
                uow.Currency.TryUpdateState(receiverId, amount);
                uow.CurrencyTransactions.Add(new CurrencyTransaction()
                {
                    UserId = receiverId,
                    Reason = reason,
                    Amount = amount,
                });
                await uow.CompleteAsync();
            }
        }
    }
}
