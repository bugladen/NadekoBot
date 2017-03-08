using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.DataStructures
{
    public class PermissionsCollection<T> : IList<T> where T : IIndexed
    {
        public List<T> Source { get; }
        private readonly object _locker = new object();

        public PermissionsCollection(IEnumerable<T> source)
        {
            lock (_locker)
            {
                Source = source.OrderBy(x => x.Index).ToList();
                for (var i = 0; i < Source.Count; i++)
                {
                    if(Source[i].Index != i)
                        Source[i].Index = i;
                }
            }
        }

        public static implicit operator List<T>(PermissionsCollection<T> x) => 
            x.Source;

        public IEnumerator<T> GetEnumerator() =>
            Source.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            Source.GetEnumerator();

        public void Add(T item)
        {
            lock (_locker)
            {
                item.Index = Source.Count;
                Source.Add(item);
            }
        }

        public void Clear()
        {
            lock (_locker)
            {
                var first = Source[0];
                Source.Clear();
                Source[0] = first;
            }
        }

        public bool Contains(T item)
        {
            lock (_locker)
            {
                return Source.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_locker)
            {
                Source.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            bool removed;
            lock (_locker)
            {
                if(Source.IndexOf(item) == 0)
                    throw new ArgumentException("You can't remove first permsission (allow all)");
                if (removed = Source.Remove(item))
                {
                    for (int i = 0; i < Source.Count; i++)
                    {
                        // hm, no idea how ef works, so I don't want to set if it's not changed, 
                        // maybe it will try to update db? 
                        // But most likely it just compares old to new values, meh.
                        if (Source[i].Index != i)
                            Source[i].Index = i;
                    }
                }
            }
            return removed;
        }

        public int Count => Source.Count;
        public bool IsReadOnly => false;
        public int IndexOf(T item) => item.Index;

        public void Insert(int index, T item)
        {
            lock (_locker)
            {
                if(index == 0) // can't insert on first place. Last item is always allow all.
                    throw new IndexOutOfRangeException(nameof(index));
                Source.Insert(index, item);
                for (int i = index; i < Source.Count; i++)
                {
                    Source[i].Index = i;
                }
            }
        }

        public void RemoveAt(int index)
        {
            lock (_locker)
            {
                if(index == 0) // you can't remove first permission (allow all)
                    throw new IndexOutOfRangeException(nameof(index)); 

                Source.RemoveAt(index);
                for (int i = index; i < Source.Count; i++)
                {
                    Source[i].Index = i;
                }
            }
        }

        public T this[int index] {
            get { return Source[index]; }
            set {
                lock (_locker)
                {
                    if(index == 0) // can't set first element. It's always allow all
                        throw new IndexOutOfRangeException(nameof(index));
                    value.Index = index;
                    Source[index] = value;
                }
            }
        }
    }

}
