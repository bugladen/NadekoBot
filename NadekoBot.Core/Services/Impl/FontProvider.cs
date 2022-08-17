using SixLabors.Fonts;
using System.IO;

namespace NadekoBot.Core.Services.Impl
{
    public class FontProvider : INService
    {
        private readonly FontCollection _fonts;

        public FontProvider()
        {
            _fonts = new FontCollection();
            if (Directory.Exists("data/fonts"))
                foreach (var file in Directory.GetFiles("data/fonts"))
                {
                    _fonts.Install(file);
                }

            NotoSans = _fonts.Find("Noto Sans");
            RankFontFamily = _fonts.Find("Uni Sans Thin CAPS");
        }

        public FontFamily RankFontFamily { get; }
        public FontFamily NotoSans { get; }
    }
}
