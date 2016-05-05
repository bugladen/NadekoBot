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
                DbHandler.Instance.InsertData(new DataModels.CurrencyTransaction
                {
                    Reason = reason,
                    UserId = (long)u.Id,
                    Value = amount,
                });
            }).ConfigureAwait(false);

            if (silent)
                return;

            var flows = "";
             //Maximum displayed will be ~40
            int i;
            for (i = 0; i < 40 && i < amount; i++)
            {
                flows += NadekoBot.Config.CurrencySign;
            }
            if (i < amount)
            {
                flows += $" and {amount - i} more {NadekoBot.Config.CurrencySign}!";
            }

            await u.SendMessage("👑Congratulations!👑\nYou received: " + flows).ConfigureAwait(false);
        }

        public static bool RemoveFlowers(Discord.User u, string reason, int amount)
        {
            if (amount <= 0)
                return false;
            var uid = (long)u.Id;
            var state = DbHandler.Instance.FindOne<DataModels.CurrencyState>(cs => cs.UserId == uid);

            if (state.Value < amount)
                return false;

            DbHandler.Instance.InsertData(new DataModels.CurrencyTransaction
            {
                Reason = reason,
                UserId = (long)u.Id,
                Value = -amount,
            });
            return true;
        }
    }
}
