using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Music
{
    public class MusicQueue : IDisposable
    {
        private LinkedList<SongInfo> Songs { get; } = new LinkedList<SongInfo>();
        private int _currentIndex = 0;
        private int CurrentIndex
        {
            get
            {
                return _currentIndex;
            }
            set
            {
                lock (locker)
                {
                    if (Songs.Count == 0)
                        _currentIndex = 0;
                    else
                        _currentIndex = value %= Songs.Count;
                }
            }
        }
        public (int Index, SongInfo Song) Current
        {
            get
            {
                var cur = CurrentIndex;
                return (cur, Songs.ElementAtOrDefault(cur));
            }
        }

        private readonly object locker = new object();
        private TaskCompletionSource<bool> nextSource { get; } = new TaskCompletionSource<bool>();
        public int Count
        {
            get
            {
                lock (locker)
                {
                    return Songs.Count;
                }
            }
        }

        public void Add(SongInfo song)
        {
            lock (locker)
            {
                Songs.AddLast(song);
            }
        }

        public void Next()
        {
            CurrentIndex++;
        }

        public void Dispose()
        {
            Clear();
        }

        public SongInfo RemoveAt(int index)
        {
            lock (locker)
            {
                if (index < 0 || index >= Songs.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var current = Songs.First;
                for (int i = 0; i < Songs.Count; i++)
                {
                    if (i == index)
                    {
                        Songs.Remove(current);
                        if (CurrentIndex != 0)
                        {
                            if (CurrentIndex >= index)
                            {
                                --CurrentIndex;
                            }
                        }
                        break;
                    }
                }
                return current.Value;
            }
        }

        public void Clear()
        {
            lock (locker)
            {
                Songs.Clear();
                CurrentIndex = 0;
            }
        }

        public (int, SongInfo[]) ToArray()
        {
            lock (locker)
            {
                return (CurrentIndex, Songs.ToArray());
            }
        }

        public void ResetCurrent()
        {
            CurrentIndex = 0;
        }
    }
}
