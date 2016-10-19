using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface IGoogleApiService
    {
        Task<IEnumerable<string>> GetVideosByKeywordsAsync(string keywords, int count = 1);
        Task<IEnumerable<string>> GetPlaylistIdsByKeywordsAsync(string keywords, int count = 1);
        Task<IEnumerable<string>> GetRelatedVideosAsync(string url, int count = 1);
        Task<IEnumerable<string>> GetPlaylistTracksAsync(string playlistId, int count = 50);
        Task<IReadOnlyDictionary<string, TimeSpan>> GetVideoDurationsAsync(IEnumerable<string> videoIds);

        Task<string> ShortenUrl(string url);
    }
}
