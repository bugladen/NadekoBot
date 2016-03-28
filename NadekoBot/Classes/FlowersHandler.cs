using System.Threading.Tasks;

namespace NadekoBot.Classes
{
    internal static class FlowersHandler
    {
        public static async Task AddFlowersAsync(Discord.User u, string reason, int amount)
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
            var flows = "";
            for (var i = 0; i < amount; i++)
            {
                flows += NadekoBot.Config.CurrencySign;
            }
            await u.SendMessage("👑Congratulations!👑\nYou received: " + flows);
        }

        public static async Task RemoveFlowersAsync(Discord.User u, string reason, int amount)
        {
            if (amount <= 0)
                return;
            await Task.Run(() =>
            {
                DbHandler.Instance.InsertData(new _DataModels.CurrencyTransaction
                {
                    Reason = reason,
                    UserId = (long)u.Id,
                    Value = -amount,
                });
            });
        }
    }
}
