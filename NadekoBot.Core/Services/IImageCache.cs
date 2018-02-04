using NadekoBot.Core.Common;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services
{
    public interface IImageCache
    {
        ImageUrls ImageUrls { get; }

        byte[][] Heads { get; }
        byte[][] Tails { get; }
        
        byte[][] Dice { get; }

        byte[] SlotBackground { get; }
        byte[][] SlotEmojis { get; }
        byte[][] SlotNumbers { get; }

        byte[] WifeMatrix { get; }
        byte[] RategirlDot { get; }

        byte[] XpCard { get; }

        byte[] Rip { get; }
        byte[] FlowerCircle { get; }

        Task Reload();
    }
}
