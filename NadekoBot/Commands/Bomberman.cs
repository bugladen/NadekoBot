using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules;
using System;

namespace NadekoBot.Commands
{
    class Bomberman : DiscordCommand
    {
        public Bomberman(DiscordModule module) : base(module)
        {
            NadekoBot.Client.MessageReceived += async (s, e) =>
            {
                if (e.Channel.Id != bombGame.ChannelId) return;

                var text = e.Message.Text;
                await e.Message.Delete();
                HandleBombermanCommand(e.User, text);
            };
        }

        private void HandleBombermanCommand(User user, string text)
        {
            throw new NotImplementedException();
        }

        //only one bomberman game can run at any one time
        public static BombermanGame bombGame = null;
        private readonly object locker = new object();
        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand($"{Module.Prefix}bmb")
                .Description("Creates a bomberman game for this channel or join existing one." +
                             " If you are 4th player - Game will start. After game starts " +
                             " everything written in the channel will be autodeleted and treated as a bomberman command." +
                             " only one bomberman game can run at any one time per bot. Game will run at 1FPS." +
                             " You must have manage messages permissions in order to create the game.")
                .Do(e =>
                {
                    lock (locker)
                    {
                        if (bombGame == null || bombGame.Ended)
                        {
                            if (!e.User.ServerPermissions.ManageMessages ||
                                !e.Server.GetUser(NadekoBot.Client.CurrentUser.Id).ServerPermissions.ManageMessages)
                            {
                                e.Channel.SendMessage("Both you and Nadeko need manage messages permissions to start a new bomberman game.").Wait();
                            }

                        }
                    }
                });
        }
    }
}
