using System;
using System.IO;
using System.Net.Http;
using NadekoBot.Common;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NLog;
using SixLabors.Primitives;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;

namespace NadekoBot.Modules.Games.Common
{
    public class GirlRating
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly IImageCache _images;

        public double Crazy { get; }
        public double Hot { get; }
        public int Roll { get; }
        public string Advice { get; }

        private readonly IHttpClientFactory _httpFactory;

        public AsyncLazy<string> Url { get; }

        public GirlRating(IImageCache images, IHttpClientFactory factory, double crazy, double hot, int roll, string advice)
        {
            _images = images;
            Crazy = crazy;
            Hot = hot;
            Roll = roll;
            Advice = advice; // convenient to have it here, even though atm there are only few different ones.
            _httpFactory = factory;

            Url = new AsyncLazy<string>(async () =>
            {
                try
                {
                    using (var img = Image.Load(_images.RategirlMatrix))
                    {
                        const int minx = 35;
                        const int miny = 385;
                        const int length = 345;

                        var pointx = (int)(minx + length * (Hot / 10));
                        var pointy = (int)(miny - length * ((Crazy - 4) / 6));

                        using (var pointImg = Image.Load(_images.RategirlDot))
                        {
                            img.Mutate(x => x.DrawImage(GraphicsOptions.Default, pointImg, new Point(pointx - 10, pointy - 10)));
                        }

                        string url;
                        using (var http = _httpFactory.CreateClient())
                        using (var imgStream = new MemoryStream())
                        {
                            img.SaveAsPng(imgStream);
                            using (var byteContent = new ByteArrayContent(imgStream.ToArray()))
                            {
                                http.AddFakeHeaders();

                                using (var reponse = await http.PutAsync("https://transfer.sh/img.png", byteContent).ConfigureAwait(false))
                                {
                                    url = await reponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                                }
                            }
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
