using System.Collections.Immutable;

namespace NadekoBot.Core.Services
{
    public interface IImageCache
    {
        byte[] Heads { get; }
        byte[] Tails { get; }

        byte[][] Currency { get; }
        byte[][] Dice { get; }

        byte[] SlotBackground { get; }
        byte[][] SlotEmojis { get; }
        byte[][] SlotNumbers { get; }

        byte[] WifeMatrix { get; }
        byte[] RategirlDot { get; }

        byte[] XpCard { get; }

        byte[] Rip { get; }
        byte[] FlowerCircle { get; }

        void Reload();
    }
}
