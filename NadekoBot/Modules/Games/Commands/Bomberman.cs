using Discord;
using Discord.Commands;
using NadekoBot.Commands;
using System.Linq;
using static NadekoBot.Modules.Games.Commands.Bomberman;

namespace NadekoBot.Modules.Games.Commands
{
    class Bomberman : DiscordCommand
    {
        public Field[,] board = new Field[15, 15];

        public BombermanPlayer[] players = new BombermanPlayer[4];

        public Channel gameChannel;


        public Bomberman(DiscordModule module) : base(module)
        {
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    board[i, j] = new Field();
                }
            }
            NadekoBot.Client.MessageReceived += (s, e) =>
            {
                if (e.Channel != gameChannel)
                    return;

                if (e.Message.Text == "a")
                    players.Where(p => p.User == e.User).FirstOrDefault()?.MoveLeft();
            };
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            //cgb.CreateCommand(Module.Prefix + "bomb")
            //    .Description("Bomberman start")
            //    .Do(e =>
            //    {
            //        if (gameChannel != null)
            //            return;
            //        gameChannel = e.Channel;
            //        players[0] = new BombermanPlayer
            //        {
            //            User = e.User,
            //        };
            //    });
        }

        public class BombermanPlayer
        {
            public User User;
            public string Icon = "👳";

            internal void MoveLeft()
            {

            }
        }
    }

    internal class Field
    {
        public BombermanPlayer player = null;

        public Field()
        {
        }

        public override string ToString() => player?.Icon ?? "⬜";
    }
}
