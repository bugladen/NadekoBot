using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes {
    static class FlowersHandler {
        public static async Task AddFlowersAsync(Discord.User u, string reason, int amount) {
            if (amount <= 0)
                return;
            await Task.Run(() => {
                DBHandler.Instance.InsertData(new _DataModels.CurrencyTransaction {
                    Reason = reason,
                    UserId = (long)u.Id,
                    Value = amount,
                });
            });
            string flows = "";
            for (int i = 0; i < amount; i++) {
                flows += "🌸";
            }
            await u.SendMessage("👑Congratulations!👑\nYou got: "+flows);
        }
    }
}
