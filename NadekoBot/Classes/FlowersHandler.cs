using System.Threading.Tasks;

namespace NadekoBot.Classes
{
    internal static class FlowersHandler
    {
        public static async Task AddFlowersAsync(Discord.User u, string reason, int amount, bool silent = false)
        {
            if (amount <= 0)
                return;
            await Task.Run(() =>
            {
                DbHandler.Instance.Connection.Insert(new DataModels.CurrencyTransaction
                {
                    Reason = reason,
                    UserId = (long)u.Id,
                    Value = amount,
                });
            }).ConfigureAwait(false);

            if (silent)
                return;

            var flows = amount + " " + NadekoBot.Config.CurrencySign;

            await u.SendMessage("👑Congratulations!👑\nYou received: " + flows).ConfigureAwait(false);
        }

        public static async Task<bool> RemoveFlowers(Discord.User u, string reason, int amount, bool silent=false, string message="👎`Bot owner has taken {0}{1} from you.`")
        {
            if (amount <= 0)
                return false;
            var uid = (long)u.Id;
            var state = DbHandler.Instance.FindOne<DataModels.CurrencyState>(cs => cs.UserId == uid);

            if (state.Value < amount)
                return false;

            DbHandler.Instance.Connection.Insert(new DataModels.CurrencyTransaction
            {
                Reason = reason,
                UserId = (long)u.Id,
                Value = -amount,
            });

            if (silent)
                return true;

            await u.SendMessage(string.Format(message,amount,NadekoBot.Config.CurrencySign)).ConfigureAwait(false);
            return true;
        }
    }
}
