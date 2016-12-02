using Discord;
using Discord.Audio;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NadekoBot.Modules.Music.Classes
{

    public enum MusicType
    {
        Radio,
        Normal,
        Local,
        Soundcloud
    }

    public enum StreamState
    {
        Resolving,
        Queued,
        Playing,
        Completed
    }

    public class MusicPlayer
    {
        private IAudioClient audioClient { get; set; }

        private readonly List<Song> playlist = new List<Song>();
        public IReadOnlyCollection<Song> Playlist => playlist;

        public Song CurrentSong { get; private set; }
        public CancellationTokenSource SongCancelSource { get; private set; }
        private CancellationToken cancelToken { get; set; }

        public bool Paused { get; set; }

        public float Volume { get; private set; }

        public event EventHandler<Song> OnCompleted = delegate { };
        public event EventHandler<Song> OnStarted = delegate { };

        public IVoiceChannel PlaybackVoiceChannel { get; private set; }

        private bool Destroyed { get; set; } = false;
        public bool RepeatSong { get; private set; } = false;
        public bool RepeatPlaylist { get; private set; } = false;
        public bool Autoplay { get; set; } = false;
        public uint MaxQueueSize { get; set; } = 0;

        private ConcurrentQueue<Action> actionQueue { get; set; } = new ConcurrentQueue<Action>();

        public MusicPlayer(IVoiceChannel startingVoiceChannel, float? defaultVolume)
        {
            if (startingVoiceChannel == null)
                throw new ArgumentNullException(nameof(startingVoiceChannel));
            Volume = defaultVolume ?? 1.0f;

            PlaybackVoiceChannel = startingVoiceChannel;
            SongCancelSource = new CancellationTokenSource();
            cancelToken = SongCancelSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!Destroyed)
                    {
                        try
                        {
                            Action action;
                            if (actionQueue.TryDequeue(out action))
                            {
                                action();
                            }
                        }
                        finally
                        {
                            await Task.Delay(100).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Action queue crashed");
                    Console.WriteLine(ex);
                }
            }).ConfigureAwait(false);

            var t = new Thread(new ThreadStart(async () =>
            {
                while (!Destroyed)
                {
                    try
                    {
                        if (audioClient?.ConnectionState != ConnectionState.Connected)
                        {
                            if (audioClient != null)
                                try { await audioClient.DisconnectAsync().ConfigureAwait(false); } catch { }
                            audioClient = await PlaybackVoiceChannel.ConnectAsync().ConfigureAwait(false);
                            continue;
                        }

                        CurrentSong = GetNextSong();
                        RemoveSongAt(0);

                        if (CurrentSong == null)
                            continue;


                        OnStarted(this, CurrentSong);
                        await CurrentSong.Play(audioClient, cancelToken);

                        OnCompleted(this, CurrentSong);

                        if (RepeatPlaylist)
                            AddSong(CurrentSong, CurrentSong.QueuerName);

                        if (RepeatSong)
                            AddSong(CurrentSong, 0);

                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Music thread almost crashed.");
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        if (!cancelToken.IsCancellationRequested)
                        {
                            SongCancelSource.Cancel();
                        }
                        SongCancelSource = new CancellationTokenSource();
                        cancelToken = SongCancelSource.Token;
                        CurrentSong = null;
                        await Task.Delay(300).ConfigureAwait(false);
                    }
                }
            }));

            t.Start();
        }

        public void Next()
        {
            actionQueue.Enqueue(() =>
            {
                Paused = false;
                SongCancelSource.Cancel();
            });
        }

        public void Stop()
        {
            actionQueue.Enqueue(() =>
            {
                RepeatPlaylist = false;
                RepeatSong = false;
                playlist.Clear();
                if (!SongCancelSource.IsCancellationRequested)
                    SongCancelSource.Cancel();
            });
        }

        public void TogglePause() => Paused = !Paused;

        public int SetVolume(int volume)
        {
            if (volume < 0)
                volume = 0;
            if (volume > 100)
                volume = 100;

            Volume = volume / 100.0f;
            return volume;
        }

        private Song GetNextSong() =>
            playlist.FirstOrDefault();

        public void Shuffle()
        {
            actionQueue.Enqueue(() =>
            {
                var oldPlaylist = playlist.ToArray();
                playlist.Clear();
                playlist.AddRange(oldPlaylist.Shuffle());
            });
        }

        public void AddSong(Song s, string username)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            ThrowIfQueueFull();
            actionQueue.Enqueue(() =>
            {
                s.MusicPlayer = this;
                s.QueuerName = username.TrimTo(10);
                playlist.Add(s);
            });
        }

        public void AddSong(Song s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            actionQueue.Enqueue(() =>
            {
                playlist.Insert(index, s);
            });
        }

        public void RemoveSong(Song s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            actionQueue.Enqueue(() =>
            {
                playlist.Remove(s);
            });
        }

        public void RemoveSongAt(int index)
        {
            actionQueue.Enqueue(() =>
            {
                if (index < 0 || index >= playlist.Count)
                    return;
                playlist.RemoveAt(index);
            });
        }

        public void ClearQueue()
        {
            actionQueue.Enqueue(() =>
            {
                playlist.Clear();
            });
        }

        public async Task UpdateSongDurationsAsync()
        {
            var curSong = CurrentSong;
            var toUpdate = playlist.Where(s => s.SongInfo.ProviderType == MusicType.Normal &&
                                                          s.TotalLength == TimeSpan.Zero);
            if (curSong != null)
                toUpdate = toUpdate.Append(curSong);
            var ids = toUpdate.Select(s => s.SongInfo.Query.Substring(s.SongInfo.Query.LastIndexOf("?v=") + 3))
                              .Distinct();

            var durations = await NadekoBot.Google.GetVideoDurationsAsync(ids);

            toUpdate.ForEach(s =>
            {
                foreach (var kvp in durations)
                {
                    if (s.SongInfo.Query.EndsWith(kvp.Key))
                    {
                        s.TotalLength = kvp.Value;
                        return;
                    }
                }
            });
        }

        public void Destroy()
        {
            actionQueue.Enqueue(async () =>
            {
                RepeatPlaylist = false;
                RepeatSong = false;
                Destroyed = true;
                playlist.Clear();

                try { await audioClient.DisconnectAsync(); } catch { }
                if (!SongCancelSource.IsCancellationRequested)
                    SongCancelSource.Cancel();
            });
        }

        public Task MoveToVoiceChannel(IVoiceChannel voiceChannel)
        {
            if (audioClient?.ConnectionState != ConnectionState.Connected)
                throw new InvalidOperationException("Can't move while bot is not connected to voice channel.");
            PlaybackVoiceChannel = voiceChannel;
            return PlaybackVoiceChannel.ConnectAsync();
        }

        public bool ToggleRepeatSong() => this.RepeatSong = !this.RepeatSong;

        public bool ToggleRepeatPlaylist() => this.RepeatPlaylist = !this.RepeatPlaylist;

        public bool ToggleAutoplay() => this.Autoplay = !this.Autoplay;

        public void ThrowIfQueueFull()
        {
            if (MaxQueueSize == 0)
                return;
            if (playlist.Count >= MaxQueueSize)
                throw new PlaylistFullException();
        }
    }
}
