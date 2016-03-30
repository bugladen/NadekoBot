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
                DbHandler.Instance.InsertData(new _DataModels.CurrencyTransaction
                {
                    Reason = reason,
                    UserId = (long)u.Id,
                    Value = amount,
                });
            });

            if (silent)
                return;

            var flows = "";
            for (var i = 0; i < amount; i++)
            {
                flows += NadekoBot.Config.CurrencySign;
            }
            await u.SendMessage("👑Congratulations!👑\nYou received: " + flows);
        }

        public static bool RemoveFlowers(Discord.User u, string reason, int amount)
        {
            if (amount <= 0)
                return false;
            var uid = (long)u.Id;
            var state = DbHandler.Instance.FindOne<_DataModels.CurrencyState>(cs => cs.UserId == uid);

            if (state.Value < amount)
                return false;

            DbHandler.Instance.InsertData(new _DataModels.CurrencyTransaction
            {
                Reason = reason,
                UserId = (long)u.Id,
                Value = -amount,
            });
            return true;
        }
    }
}
