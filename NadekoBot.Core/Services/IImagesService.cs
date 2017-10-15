using System.Collections.Immutable;

namespace NadekoBot.Core.Services
{
    public interface IImagesService : INService
    {
        ImmutableArray<byte> Heads { get; }
        ImmutableArray<byte> Tails { get; }

        ImmutableArray<(string, ImmutableArray<byte>)> Currency { get; }
        ImmutableArray<ImmutableArray<byte>> Dice { get; }

        ImmutableArray<byte> SlotBackground { get; }
        ImmutableArray<ImmutableArray<byte>> SlotEmojis { get; }
        ImmutableArray<ImmutableArray<byte>> SlotNumbers { get; }

        ImmutableArray<byte> WifeMatrix { get; }
        ImmutableArray<byte> RategirlDot { get; }

        ImmutableArray<byte> XpCard { get; }

        ImmutableArray<byte> Rip { get; }
        ImmutableArray<byte> FlowerCircle { get; }

        void Reload();
    }
}
