using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Music
{
    public class SongResolver
    {
        //        public async Task<Song> ResolveSong(string query, MusicType musicType = MusicType.Normal)
        //        {
        //            if (string.IsNullOrWhiteSpace(query))
        //                throw new ArgumentNullException(nameof(query));

        //            if (musicType != MusicType.Local && IsRadioLink(query))
        //            {
        //                musicType = MusicType.Radio;
        //                query = await HandleStreamContainers(query).ConfigureAwait(false) ?? query;
        //            }

        //            try
        //            {
        //                switch (musicType)
        //                {
        //                    case MusicType.Local:
        //                        return new Song(new SongInfo
        //                        {
        //                            Uri = "\"" + Path.GetFullPath(query) + "\"",
        //                            Title = Path.GetFileNameWithoutExtension(query),
        //                            Provider = "Local File",
        //                            ProviderType = musicType,
        //                            Query = query,
        //                        });
        //                    case MusicType.Radio:
        //                        return new Song(new SongInfo
        //                        {
        //                            Uri = query,
        //                            Title = $"{query}",
        //                            Provider = "Radio Stream",
        //                            ProviderType = musicType,
        //                            Query = query
        //                        })
        //                        { TotalTime = TimeSpan.MaxValue };
        //                }
        //                if (_sc.IsSoundCloudLink(query))
        //                {
        //                    var svideo = await _sc.ResolveVideoAsync(query).ConfigureAwait(false);
        //                    return new Song(new SongInfo
        //                    {
        //                        Title = svideo.FullName,
        //                        Provider = "SoundCloud",
        //                        Uri = await svideo.StreamLink(),
        //                        ProviderType = musicType,
        //                        Query = svideo.TrackLink,
        //                        AlbumArt = svideo.artwork_url,
        //                    })
        //                    { TotalTime = TimeSpan.FromMilliseconds(svideo.Duration) };
        //                }

        //                if (musicType == MusicType.Soundcloud)
        //                {
        //                    var svideo = await _sc.GetVideoByQueryAsync(query).ConfigureAwait(false);
        //                    return new Song(new SongInfo
        //                    {
        //                        Title = svideo.FullName,
        //                        Provider = "SoundCloud",
        //                        Uri = await svideo.StreamLink(),
        //                        ProviderType = MusicType.Soundcloud,
        //                        Query = svideo.TrackLink,
        //                        AlbumArt = svideo.artwork_url,
        //                    })
        //                    { TotalTime = TimeSpan.FromMilliseconds(svideo.Duration) };
        //                }

        //                var link = (await _google.GetVideoLinksByKeywordAsync(query).ConfigureAwait(false)).FirstOrDefault();
        //                if (string.IsNullOrWhiteSpace(link))
        //                    throw new OperationCanceledException("Not a valid youtube query.");
        //                var allVideos = await Task.Run(async () => { try { return await YouTube.Default.GetAllVideosAsync(link).ConfigureAwait(false); } catch { return Enumerable.Empty<YouTubeVideo>(); } }).ConfigureAwait(false);
        //                var videos = allVideos.Where(v => v.AdaptiveKind == AdaptiveKind.Audio);
        //                var video = videos
        //                    .Where(v => v.AudioBitrate < 256)
        //                    .OrderByDescending(v => v.AudioBitrate)
        //                    .FirstOrDefault();

        //                if (video == null) // do something with this error
        //                    throw new Exception("Could not load any video elements based on the query.");
        //                var m = Regex.Match(query, @"\?t=(?<t>\d*)");
        //                int gotoTime = 0;
        //                if (m.Captures.Count > 0)
        //                    int.TryParse(m.Groups["t"].ToString(), out gotoTime);
        //                var song = new Song(new SongInfo
        //                {
        //                    Title = video.Title.Substring(0, video.Title.Length - 10), // removing trailing "- You Tube"
        //                    Provider = "YouTube",
        //                    Uri = await video.GetUriAsync().ConfigureAwait(false),
        //                    Query = link,
        //                    ProviderType = musicType,
        //                });
        //                song.SkipTo = gotoTime;
        //                return song;
        //            }
        //            catch (Exception ex)
        //            {
        //                _log.Warn($"Failed resolving the link.{ex.Message}");
        //                _log.Warn(ex);
        //                return null;
        //            }
        //        }
    }
}
