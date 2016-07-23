using NadekoBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using System.Drawing;
using System.Drawing.Drawing2D;
using NadekoBot.Properties;
using System.IO;
using System.Drawing.Imaging;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Conversations.Commands
{
    class RipCommand : DiscordCommand
    {
        public RipCommand(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("rip")
                    .Description("Shows a grave image of someone with a start year | @NadekoBot rip @Someone 2000")
                    .Parameter("user", ParameterType.Required)
                    .Parameter("year", ParameterType.Optional)
                    .Do(async e =>
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("user")))
                            return;
                        var usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        var text = "";
                        Stream file;
                        if (usr == null)
                        {
                            text = e.GetArg("user");
                            file = RipName(text, string.IsNullOrWhiteSpace(e.GetArg("year"))
                                    ? null
                                    : e.GetArg("year"));
                        }
                        else
                        {
                            var avatar = await GetAvatar(usr.AvatarUrl);
                            text = usr.Name;
                            file = RipUser(text, avatar, string.IsNullOrWhiteSpace(e.GetArg("year"))
                                    ? null
                                    : e.GetArg("year"));
                        }
                        await e.Channel.SendFile("ripzor_m8.png",
                                            file);
                    });
        }


        /// <summary>
        /// Create a RIP image of the given name and avatar, with an optional year
        /// </summary>
        /// <param name="name"></param>
        /// <param name="avatar"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public Stream RipUser(string name, Image avatar, string year = null)
        {
            var bm = Resources.rip;
            int width = 300;
            var fontSize = width / name.Length  -2;
            if (fontSize > 20) fontSize = 20;
            var g = Graphics.FromImage(bm);
            Font nameFont = new Font("Comic Sans MS", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            SizeF nameSize = g.MeasureString(name, nameFont);
            g.DrawString(name, new Font("Comic Sans MS", fontSize, FontStyle.Bold), Brushes.Black, (bm.Width /2 - 8) - (nameSize.Width /2), 243 - nameSize.Height);
            g.DrawString((year ?? "?") + " - " + DateTime.Now.Year, new Font("Consolas", 12, FontStyle.Bold), Brushes.Black, 80, 240);

            g.DrawImage(avatar, 80, 135);
            g.DrawImage((Image)Resources.rose_overlay, 0, 0);
            g.Flush();
            g.Dispose();

            return bm.ToStream(ImageFormat.Png);
        }

        public Stream RipName(string name, string year = null)
        {
            var bm = Resources.rip;
            int width = 190;
            var offset = name.Length * 5;
            var fontSize = width / name.Length;
            if (fontSize > 20) fontSize = 20;
            var g = Graphics.FromImage(bm);
            Font nameFont = new Font("Comic Sans MS", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            SizeF nameSize = g.MeasureString(name, nameFont);
            g.DrawString(name, nameFont, Brushes.Black, (bm.Width / 2) - (nameSize.Width / 2), 200);
            g.DrawString((year ?? "?") + " - " + DateTime.Now.Year, new Font("Consolas", 12, FontStyle.Bold), Brushes.Black, 80, 235);
            g.Flush();
            g.Dispose();

            return bm.ToStream(ImageFormat.Png);
        }

        public static async Task<Image> GetAvatar(string url)
        {
            var stream = await SearchHelper.GetResponseStreamAsync(url);
            Bitmap bmp = new Bitmap(100, 100);
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(0, 0, bmp.Width, bmp.Height);
                using (Graphics gr = Graphics.FromImage(bmp))
                {
                    gr.SetClip(gp);
                    gr.DrawImage(Image.FromStream(stream), Point.Empty);

                }
            }
            return bmp;

        }
    }
}
