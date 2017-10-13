using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services
{
    public interface IDataCache
    {
        ConnectionMultiplexer Redis { get; }
        Task<(bool Success, byte[] Data)> TryGetImageDataAsync(string key);
        Task<(bool Success, string Data)> TryGetAnimeDataAsync(string key);
        Task SetImageDataAsync(string key, byte[] data);
        Task SetAnimeDataAsync(string link, string data);
    }
}
