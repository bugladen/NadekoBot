using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Modules.Utility;
using NadekoBot.Services;
using NadekoBot.Services.Impl;
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
        public BotConfiguration Config { get; private set; }
        public Localization Localizer { get; private set; }

        public async Task RunAsync(string[] args)
        {
            //create client
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AudioMode = Discord.Audio.AudioMode.Incoming,
                LargeThreshold = 200,
                LogLevel = LogSeverity.Warning,
                MessageCacheSize = 10,
            });

            //initialize Services
            Commands = new CommandService();
            Config = new BotConfiguration();
            Localizer = new Localization();
            
            //setup DI
            var depMap = new DependencyMap();
            depMap.Add<ILocalization>(Localizer);
            depMap.Add<IBotConfiguration>(Config);
            depMap.Add<IDiscordClient>(Client);
            depMap.Add<CommandService>(Commands);

            //connect
            await Client.LoginAsync(TokenType.Bot, "MTE5Nzc3MDIxMzE5NTc3NjEw.CpGoCA.yQBJbLWurrjSk7IlGpGzBm-tPTg");
            await Client.ConnectAsync();
            
            //load commands
            await Commands.LoadAssembly(Assembly.GetEntryAssembly(), depMap);
            Client.MessageReceived += Client_MessageReceived;

            Console.WriteLine(Commands.Commands.Count());

            await Task.Delay(-1);
        }

        private async Task Client_MessageReceived(IMessage arg)
        {
                var t = await Commands.Execute(arg, 0);
                if(!t.IsSuccess)
                    Console.WriteLine(t.ErrorReason);
        }
    }
}
