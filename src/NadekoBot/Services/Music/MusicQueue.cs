using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Music
{
    public class MusicQueue : IDisposable
    {
        private LinkedList<SongInfo> Songs { get; set; } = new LinkedList<SongInfo>();
        private int _currentIndex = 0;
        public int CurrentIndex
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

        private uint _maxQueueSize;
        public uint MaxQueueSize
        {
            get => _maxQueueSize;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                lock (locker)
                {
                    _maxQueueSize = value;
                }
            }
        }

        public void Add(SongInfo song)
        {
            song.ThrowIfNull(nameof(song));
            lock (locker)
            {
                if(MaxQueueSize != 0 && Songs.Count >= MaxQueueSize)
                    throw new QueueFullException();
                Songs.AddLast(song);
            }
        }

        public void Next(int skipCount = 1)
        {
            lock(locker)
                CurrentIndex += skipCount;
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

        public (int CurrentIndex, SongInfo[] Songs) ToArray()
        {
            lock (locker)
            {
                return (CurrentIndex, Songs.ToArray());
            }
        }

        public void ResetCurrent()
        {
            lock (locker)
            {
                CurrentIndex = 0;
            }
        }

        public void Random()
        {
            lock (locker)
            {
                CurrentIndex = new NadekoRandom().Next(Songs.Count);
            }
        }

        public SongInfo MoveSong(int n1, int n2)
        {
            lock (locker)
            {
                var playlist = Songs.ToList();
                if (n1 > playlist.Count || n2 > playlist.Count)
                    return null;
                var s = playlist[n1 - 1];
                playlist.Insert(n2 - 1, s);
                var nn1 = n2 < n1 ? n1 : n1 - 1;
                playlist.RemoveAt(nn1);
                Songs = new LinkedList<SongInfo>(playlist);
                return s;
            }
        }

        public void RemoveSong(SongInfo song)
        {
            lock (locker)
            {
                Songs.Remove(song);
            }
        }
    }
}
