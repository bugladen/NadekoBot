using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Modules.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class NadekoBot
    {
        public static CommandService Commands { get; private set; }
        public static DiscordSocketClient Client { get; private set; }

        public async Task RunAsync(string[] args)
        {
            Client = new DiscordSocketClient(new Discord.DiscordSocketConfig
            {
                AudioMode = Discord.Audio.AudioMode.Incoming,
                LargeThreshold = 200,
                LogLevel = Discord.LogSeverity.Warning,
                MessageCacheSize = 10,
            });

            Commands = new CommandService();

            //Client.MessageReceived += Client_MessageReceived;

            //await Commands.Load(new UtilityModule());
            await Commands.LoadAssembly(Assembly.GetEntryAssembly());

            await Client.LoginAsync(Discord.TokenType.Bot, "MTE5Nzc3MDIxMzE5NTc3NjEw.CmxGHA.nk1KyvR6y05nntj-J0W_Zvu-2kk");
            await Client.ConnectAsync();

            Console.WriteLine(Commands.Commands.Count());

            await Task.Delay(-1);
        }
    }
}
