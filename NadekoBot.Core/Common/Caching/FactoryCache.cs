using System;

namespace NadekoBot.Core.Common.Caching
{
    /// <summary>
    /// A caching object which loads its value with a factory method when it expires.
    /// </summary>
    /// <typeparam name="T">Type of the value which is cached.</typeparam>
    public class FactoryCache<T> : IFactoryCache
    {
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;
        private readonly object _locker = new object();
        private TimeSpan _expireAfter;
        private readonly Func<T> _factory;
        private T Value;

        /// <summary>
        /// Creates a new factory cache object.
        /// </summary>
        /// <param name="factory">Method which loads the value when it expires or if it's not loaded the first time.</param>
        /// <param name="expireAfter">Time after which the value will be reloaded.</param>
        /// <param name="loadImmediately">Should the value be loaded right away. If set to false, value will load when it's first retrieved.</param>
        public FactoryCache(Func<T> factory, TimeSpan expireAfter, 
            bool loadImmediately = false)
        {
            _expireAfter = expireAfter;
            _factory = factory;
            if (loadImmediately)
            {
                Value = _factory();
                LastUpdate = DateTime.UtcNow;
            }
        }

        public T GetValue()
        {
            lock (_locker)
            {
                if (DateTime.UtcNow - LastUpdate > _expireAfter)
                {
                    LastUpdate = DateTime.UtcNow;
                    return Value = _factory();
                }

                return Value;
            }
        }
    }
}
