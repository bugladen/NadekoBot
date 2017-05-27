using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Administration
{
    public class SelfService
    {
        public volatile bool ForwardDMs;
        public volatile bool ForwardDMsToAllOwners;
        
        private readonly NadekoBot _bot;
        private readonly CommandHandler _cmdHandler;
        private readonly DbHandler _db;

        public SelfService(NadekoBot bot, CommandHandler cmdHandler, DbHandler db,
            BotConfig bc)
        {
            _bot = bot;
            _cmdHandler = cmdHandler;
            _db = db;

            using (var uow = _db.UnitOfWork)
            {
                var config = uow.BotConfig.GetOrCreate();
                ForwardDMs = config.ForwardMessages;
                ForwardDMsToAllOwners = config.ForwardToAllOwners;
            }

            var _ = Task.Run(async () =>
            {
                while (!bot.Ready)
                    await Task.Delay(1000);

                foreach (var cmd in bc.StartupCommands)
                {
                    await cmdHandler.ExecuteExternal(cmd.GuildId, cmd.ChannelId, cmd.CommandText);
                    await Task.Delay(400).ConfigureAwait(false);
                }
            });
        }
    }
}
