using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using ImageSharp;
using NadekoBot.Common;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NLog;
using SixLabors.Primitives;

namespace NadekoBot.Modules.Games.Common
{
    public class GirlRating
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public double Crazy { get; }
        public double Hot { get; }
        public int Roll { get; }
        public string Advice { get; }
        public AsyncLazy<string> Url { get; }

        public GirlRating(IImagesService _images, double crazy, double hot, int roll, string advice)
        {
            Crazy = crazy;
            Hot = hot;
            Roll = roll;
            Advice = advice; // convenient to have it here, even though atm there are only few different ones.

            Url = new AsyncLazy<string>(async () =>
            {
                try
                {
                    using (var ms = new MemoryStream(_images.WifeMatrix.ToArray(), false))
                    using (var img = Image.Load(ms))
                    {
                        const int minx = 35;
                        const int miny = 385;
                        const int length = 345;

                        var pointx = (int)(minx + length * (Hot / 10));
                        var pointy = (int)(miny - length * ((Crazy - 4) / 6));

                        using (var pointMs = new MemoryStream(_images.RategirlDot.ToArray(), false))
                        using (var pointImg = Image.Load(pointMs))
                        {
                            img.DrawImage(pointImg, 100, default(Size), new Point(pointx - 10, pointy - 10));
                        }

                        string url;
                        using (var http = new HttpClient())
                        using (var imgStream = new MemoryStream())
                        {
                            img.SaveAsPng(imgStream);
                            var byteContent = new ByteArrayContent(imgStream.ToArray());
                            http.AddFakeHeaders();

                            var reponse = await http.PutAsync("https://transfer.sh/img.png", byteContent);
                            url = await reponse.Content.ReadAsStringAsync();
                        }
                        return url;
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    return null;
                }
            });
        }
    }
}
