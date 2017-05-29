using NadekoBot.DataStructures.ModuleBehaviors;
using NadekoBot.Services.Database.Models;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System;

namespace NadekoBot.Services.Help
{
    public class HelpService : ILateExecutor
    {
        private readonly BotConfig _bc;

        public HelpService(BotConfig bc)
        {
            _bc = bc;
        }

        public async Task LateExecute(DiscordShardedClient client, IGuild guild, IUserMessage msg)
        {
            try
            {
                if(guild == null)
                    await msg.Channel.SendMessageAsync(_bc.DMHelpString).ConfigureAwait(false);
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
}
