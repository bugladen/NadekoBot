using NadekoBot.DataStructures;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface IImagesService
    {
        ImmutableArray<byte> Heads { get; }
        ImmutableArray<byte> Tails { get; }

        ImmutableArray<KeyValuePair<string, ImmutableArray<byte>>> Currency { get; }
        ImmutableArray<KeyValuePair<string, ImmutableArray<byte>>> Dice { get; }

        ImmutableArray<byte> SlotBackground { get; }
        ImmutableArray<ImmutableArray<byte>> SlotEmojis { get; }
        ImmutableArray<ImmutableArray<byte>> SlotNumbers { get; }

        ImmutableArray<byte> WifeMatrix { get; }
        ImmutableArray<byte> RategirlDot { get; }

        Task<TimeSpan> Reload();
    }
}
