using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot
{
    class ImageHandler
    {
        /// <summary>
        /// Merges Images into 1 Image and returns a bitmap.
        /// </summary>
        /// <param name="images">The Images you want to merge.</param>
        /// <returns>Merged bitmap</returns>
        public static Bitmap MergeImages(Image[] images)
        {
            int width = images.Sum(i => i.Width);
            int height = images[0].Height;
            Bitmap bitmap = new Bitmap(width, height);
            var r = new Random();
            int offsetx = 0;
            foreach (var img in images)
            {
                Bitmap bm = new Bitmap(img);
                for (int w = 0; w < img.Width; w++)
                {
                    for (int h = 0; h < img.Height; h++)
                    {
                        bitmap.SetPixel(w + offsetx, h, bm.GetPixel(w, h));
                    }
                }
                offsetx += img.Width;
            }
            return bitmap;
        }

        public static Stream ImageToStream(Image img,ImageFormat format) {
            MemoryStream stream = new MemoryStream();
            img.Save(stream, format);
            stream.Position = 0;
            return stream;
        }
    }
}
