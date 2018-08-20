using NadekoBot.Modules.Searches.Common;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services
{
    public interface IDataCache
    {
        ConnectionMultiplexer Redis { get; }
        IImageCache LocalImages { get; }
        ILocalDataCache LocalData { get; }

        Task<(bool Success, byte[] Data)> TryGetImageDataAsync(Uri key);
        Task<(bool Success, string Data)> TryGetAnimeDataAsync(string key);
        Task<(bool Success, string Data)> TryGetNovelDataAsync(string key);
        Task SetImageDataAsync(Uri key, byte[] data);
        Task SetAnimeDataAsync(string link, string data);
        Task SetNovelDataAsync(string link, string data);
        TimeSpan? AddTimelyClaim(ulong id, int period);
        TimeSpan? TryAddRatelimit(ulong id, string name, int expireIn);
        void RemoveAllTimelyClaims();
        bool TryAddAffinityCooldown(ulong userId, out TimeSpan? time);
        bool TryAddDivorceCooldown(ulong userId, out TimeSpan? time);
        Task SetStreamDataAsync(string url, string data);
        bool TryGetStreamData(string url, out string dataStr);
        void SubscribeToStreamUpdates(Func<StreamResponse[], Task> onStreamsUpdated);
        Task<StreamResponse[]> GetAllStreamDataAsync();
        Task ClearAllStreamData();
        Task PublishStreamUpdates(List<StreamResponse> toPublish);
        bool TryGetEconomy(out string data);
        void SetEconomy(string data);

        Task<TOut> GetOrAddCachedDataAsync<TParam, TOut>(string key, Func<TParam, Task<TOut>> factory, TParam param, TimeSpan expiry);
    }
}
