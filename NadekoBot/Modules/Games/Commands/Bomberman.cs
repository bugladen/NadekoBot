using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using System.Text;
using System.Timers;
using static NadekoBot.Modules.Games.Commands.Bomberman;

namespace NadekoBot.Modules.Games.Commands
{
    class Bomberman : DiscordCommand
    {
        public Field[,] board = new Field[15, 15];

        public BombermanPlayer[] players = new BombermanPlayer[4];

        public Channel gameChannel = null;

        public Message godMsg = null;

        public int curI = 5;
        public int curJ = 5;


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
                if (e.Channel != gameChannel ||
                    e.User.Id == NadekoBot.Client.CurrentUser.Id)
                    return;

                if (e.Message.Text == "w")
                {
                    board[curI - 1, curJ] = board[curI--, curJ];
                    board[curI + 1, curJ].player = null;
                }
                else if (e.Message.Text == "s")
                {
                    board[curI + 1, curJ] = board[curI++, curJ];
                    board[curI - 1, curJ].player = null;
                }
                else if (e.Message.Text == "a")
                {
                    board[curI, curJ - 1] = board[curI, curJ--];
                    board[curI, curJ + 1].player = null;
                }
                else if (e.Message.Text == "d")
                {
                    board[curI, curJ + 1] = board[curI, curJ++];
                    board[curI, curJ - 1].player = null;
                }

                e.Message.Delete();
            };

            var t = new Timer();
            t.Elapsed += async (s, e) =>
            {
                if (gameChannel == null)
                    return;

                var boardStr = new StringBuilder();

                for (int i = 0; i < 15; i++)
                {
                    for (int j = 0; j < 15; j++)
                    {
                        boardStr.Append(board[i, j].ToString());
                    }
                    boardStr.AppendLine();
                }
                if (godMsg.Id != 0)
                    await godMsg.Edit(boardStr.ToString()).ConfigureAwait(false);

            };
            t.Interval = 1000;
            t.Start();

        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            //cgb.CreateCommand(Module.Prefix + "bomb")
            //    .Description("Bomberman start")
            //    .Do(async e =>
            //    {
            //        if (gameChannel != null)
            //            return;
            //        godMsg = await e.Channel.SendMessage("GAME START IN 1 SECOND....").ConfigureAwait(false);
            //        gameChannel = e.Channel;
            //        players[0] = new BombermanPlayer
            //        {
            //            User = e.User,
            //        };

            //        board[5, 5].player = players[0];
            //    });
        }

        public class BombermanPlayer
        {
            public User User = null;
            public string Icon = "👳";

            internal void MoveLeft()
            {

            }
        }
    }

    internal struct Field
    {
        public BombermanPlayer player;

        public override string ToString() => player?.Icon ?? "⬜";
    }
}
