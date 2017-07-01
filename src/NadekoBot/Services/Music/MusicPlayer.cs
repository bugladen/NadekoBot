using Discord;
using Discord.Audio;
using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using System.Diagnostics;

namespace NadekoBot.Services.Music
{
    public enum StreamState
    {
        Resolving,
        Queued,
        Playing,
        Completed
    }
    public class MusicPlayer
    {
        private readonly Task _player;
        private readonly IVoiceChannel VoiceChannel;
        private readonly Logger _log;

        private MusicQueue Queue { get; } = new MusicQueue();

        public bool Exited { get; set; } = false;
        public bool Stopped { get; private set; } = false;
        public float Volume { get; private set; } = 1.0f;
        public string PrettyVolume => $"🔉 {(int)(Volume * 100)}%";
        private TaskCompletionSource<bool> pauseTaskSource { get; set; } = null;

        private CancellationTokenSource SongCancelSource { get; set; }
        public ITextChannel OutputTextChannel { get; set; }
        public (int Index, SongInfo Current) Current
        {
            get
            {
                if (Stopped)
                    return (0, null);
                return Queue.Current;
            }
        }

        public bool RepeatCurrentSong { get; private set; }

        private IAudioClient _audioClient;
        private readonly object locker = new object();

        #region events
        public event Action<MusicPlayer, SongInfo> OnStarted;
        public event Action<MusicPlayer, SongInfo> OnCompleted;
        public event Action<MusicPlayer, bool> OnPauseChanged;
        #endregion

        public MusicPlayer(MusicService musicService, IVoiceChannel vch, ITextChannel output, float volume)
        {
            _log = LogManager.GetCurrentClassLogger();
            this.Volume = volume;
            this.VoiceChannel = vch;
            this.SongCancelSource = new CancellationTokenSource();
            this.OutputTextChannel = output;

            _player = Task.Run(async () =>
             {
                 while (!Exited)
                 {
                     CancellationToken cancelToken;
                     (int Index, SongInfo Song) data;
                     lock (locker)
                     {
                         data = Queue.Current;
                         cancelToken = SongCancelSource.Token;
                     }
                     try
                     {
                         _log.Info("Checking for songs");
                         if (data.Song == null)
                             continue;

                         _log.Info("Connecting");


                         _log.Info("Starting");
                         var p = Process.Start(new ProcessStartInfo
                         {
                             FileName = "ffmpeg",
                             Arguments = $"-i {data.Song.Uri} -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel quiet",
                             UseShellExecute = false,
                             RedirectStandardOutput = true,
                             RedirectStandardError = false,
                             CreateNoWindow = true,
                         });
                         var ac = await GetAudioClient();
                         if (ac == null)
                         {
                             await Task.Delay(900);
                             // just wait some time, maybe bot doesn't even have perms to join that voice channel, 
                             // i don't want to spam connection attempts
                             continue;
                         }
                         var pcm = ac.CreatePCMStream(AudioApplication.Music);

                         OnStarted?.Invoke(this, data.Song);

                         byte[] buffer = new byte[3840];
                         int bytesRead = 0;
                         try
                         {
                             while ((bytesRead = await p.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length, cancelToken).ConfigureAwait(false)) > 0)
                             {
                                 var vol = Volume;
                                 if (vol != 1)
                                     AdjustVolume(buffer, vol);
                                 await pcm.WriteAsync(buffer, 0, bytesRead, cancelToken);

                                 await (pauseTaskSource?.Task ?? Task.CompletedTask);
                             }
                         }
                         catch (OperationCanceledException)
                         {
                             _log.Info("Song Canceled");
                         }
                         catch (Exception ex)
                         {
                             _log.Warn(ex);
                         }
                         finally
                         {
                             //flush is known to get stuck from time to time, just cancel it if it takes more than 1 second
                             var flushCancel = new CancellationTokenSource();
                             var flushToken = flushCancel.Token;
                             var flushDelay = Task.Delay(1000, flushToken);
                             await Task.WhenAny(flushDelay, pcm.FlushAsync(flushToken));
                             flushCancel.Cancel();

                             OnCompleted?.Invoke(this, data.Song);
                         }
                     }
                     finally
                     {
                         _log.Info("Next song");
                         do
                         {
                             await Task.Delay(100);
                         }
                         while (Stopped && !Exited);
                         if(!RepeatCurrentSong)
                            Queue.Next();
                     }
                 }
             }, SongCancelSource.Token);
        }

        private async Task<IAudioClient> GetAudioClient(bool reconnect = false)
        {
            if (_audioClient == null ||
                _audioClient.ConnectionState != ConnectionState.Connected ||
                reconnect)
                try
                {
                    _audioClient = await VoiceChannel.ConnectAsync();
                }
                catch
                {
                    return null;
                }
            return _audioClient;
        }

        public (bool Success, int Index) Enqueue(SongInfo song)
        {
            _log.Info("Adding song");
            Queue.Add(song);
            return (true, Queue.Count);
        }

        public void Next()
        {
            lock (locker)
            {
                Stopped = false;
                Unpause();
                CancelCurrentSong();
            }
        }

        public void Stop(bool clearQueue = false)
        {
            lock (locker)
            {
                Stopped = true;
                Queue.ResetCurrent();
                if (clearQueue)
                    Queue.Clear();
                Unpause();
                CancelCurrentSong();
            }
        }

        private void Unpause()
        {
            if (pauseTaskSource != null)
            {
                pauseTaskSource.TrySetResult(true);
                pauseTaskSource = null;
            }
        }

        public void TogglePause()
        {
            lock (locker)
            {
                if (pauseTaskSource == null)
                    pauseTaskSource = new TaskCompletionSource<bool>();
                else
                {
                    Unpause();
                }
            }
            OnPauseChanged?.Invoke(this, pauseTaskSource != null);
        }

        public void SetVolume(int volume)
        {
            if (volume < 0 || volume > 100)
                throw new ArgumentOutOfRangeException(nameof(volume));
            Volume = ((float)volume) / 100;
        }

        public SongInfo RemoveAt(int index)
        {
            lock (locker)
            {
                var cur = Queue.Current;
                if (cur.Index == index)
                    Next();
                return Queue.RemoveAt(index);
            }
        }

        private void CancelCurrentSong()
        {
            lock (locker)
            {
                var cs = SongCancelSource;
                SongCancelSource = new CancellationTokenSource();
                cs.Cancel();
            }
        }

        public void ClearQueue()
        {
            lock (locker)
            {
                Queue.Clear();
            }
        }

        public (int CurrentIndex, SongInfo[] Songs) QueueArray()
            => Queue.ToArray();

        //aidiakapi ftw
        public static unsafe byte[] AdjustVolume(byte[] audioSamples, float volume)
        {
            if (Math.Abs(volume - 1f) < 0.0001f) return audioSamples;

            // 16-bit precision for the multiplication
            var volumeFixed = (int)Math.Round(volume * 65536d);

            var count = audioSamples.Length / 2;

            fixed (byte* srcBytes = audioSamples)
            {
                var src = (short*)srcBytes;

                for (var i = count; i != 0; i--, src++)
                    *src = (short)(((*src) * volumeFixed) >> 16);
            }

            return audioSamples;
        }

        public bool ToggleRepeatSong()
        {
            lock (locker)
            {
                return RepeatCurrentSong = !RepeatCurrentSong;
            }
        }

        public async Task Destroy()
        {
            _log.Info("Destroying");
            lock (locker)
            {
                Stop();
                Exited = true;
                Unpause();

                OnCompleted = null;
                OnPauseChanged = null;
                OnStarted = null;
            }
            var ac = _audioClient;
            if (ac != null)
                await ac.StopAsync();
        }


        //private IAudioClient AudioClient { get; set; }

        ///// <summary>
        ///// Player will prioritize different queuer name
        ///// over the song position in the playlist
        ///// </summary>
        //public bool FairPlay { get; set; } = false;

        ///// <summary>
        ///// Song will stop playing after this amount of time. 
        ///// To prevent people queueing radio or looped songs 
        ///// while other people want to listen to other songs too.
        ///// </summary>
        //public uint MaxPlaytimeSeconds { get; set; } = 0;


        //// this should be written better
        //public TimeSpan TotalPlaytime => 
        //    _playlist.Any(s => s.TotalTime == TimeSpan.MaxValue) ? 
        //    TimeSpan.MaxValue : 
        //    new TimeSpan(_playlist.Sum(s => s.TotalTime.Ticks));

        ///// <summary>
        ///// Users who recently got their music wish
        ///// </summary>
        //private ConcurrentHashSet<string> RecentlyPlayedUsers { get; } = new ConcurrentHashSet<string>();

        //private readonly List<Song> _playlist = new List<Song>();
        //private readonly Logger _log;
        //private readonly IGoogleApiService _google;

        //public IReadOnlyCollection<Song> Playlist => _playlist;

        //public Song CurrentSong { get; private set; }
        //public CancellationTokenSource SongCancelSource { get; private set; }
        //private CancellationToken CancelToken { get; set; }

        //public bool Paused { get; set; }

        //public float Volume { get; private set; }

        //public event Action<MusicPlayer, Song> OnCompleted = delegate { };
        //public event Action<MusicPlayer, Song> OnStarted = delegate { };
        //public event Action<bool> OnPauseChanged = delegate { };

        //public IVoiceChannel PlaybackVoiceChannel { get; private set; }
        //public ITextChannel OutputTextChannel { get; set; }

        //private bool Destroyed { get; set; }
        //public bool RepeatSong { get; private set; }
        //public bool RepeatPlaylist { get; private set; }
        //public bool Autoplay { get; set; }
        //public uint MaxQueueSize { get; set; } = 0;

        //private ConcurrentQueue<Action> ActionQueue { get; } = new ConcurrentQueue<Action>();

        //public string PrettyVolume => $"🔉 {(int)(Volume * 100)}%";

        //public event Action<Song, int> SongRemoved = delegate { };

        //public MusicPlayer(IVoiceChannel startingVoiceChannel, ITextChannel outputChannel, float? defaultVolume, IGoogleApiService google)
        //{
        //    _log = LogManager.GetCurrentClassLogger();
        //    _google = google;

        //    OutputTextChannel = outputChannel;
        //    Volume = defaultVolume ?? 1.0f;

        //    PlaybackVoiceChannel = startingVoiceChannel ?? throw new ArgumentNullException(nameof(startingVoiceChannel));
        //    SongCancelSource = new CancellationTokenSource();
        //    CancelToken = SongCancelSource.Token;

        //    Task.Run(async () =>
        //    {
        //        try
        //        {
        //            while (!Destroyed)
        //            {
        //                try
        //                {
        //                    if (ActionQueue.TryDequeue(out Action action))
        //                    {
        //                        action();
        //                    }
        //                }
        //                finally
        //                {
        //                    await Task.Delay(100).ConfigureAwait(false);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _log.Warn("Action queue crashed");
        //            _log.Warn(ex);
        //        }
        //    }).ConfigureAwait(false);

        //    var t = new Thread(async () =>
        //    {
        //        while (!Destroyed)
        //        {
        //            try
        //            {
        //                CurrentSong = GetNextSong();

        //                if (CurrentSong == null)
        //                    continue;

        //                while (AudioClient?.ConnectionState == ConnectionState.Disconnecting || 
        //                    AudioClient?.ConnectionState == ConnectionState.Connecting)
        //                {
        //                    _log.Info("Waiting for Audio client");
        //                    await Task.Delay(200).ConfigureAwait(false);
        //                }

        //                if (AudioClient == null || AudioClient.ConnectionState == ConnectionState.Disconnected)
        //                    AudioClient = await PlaybackVoiceChannel.ConnectAsync().ConfigureAwait(false);

        //                var index = _playlist.IndexOf(CurrentSong);
        //                if (index != -1)
        //                    RemoveSongAt(index, true);

        //                OnStarted(this, CurrentSong);
        //                try
        //                {
        //                    await CurrentSong.Play(AudioClient, CancelToken);
        //                }
        //                catch (OperationCanceledException)
        //                {
        //                }
        //                finally
        //                {
        //                    OnCompleted(this, CurrentSong);
        //                }


        //                if (RepeatPlaylist & !RepeatSong)
        //                    AddSong(CurrentSong, CurrentSong.QueuerName);

        //                if (RepeatSong)
        //                    AddSong(CurrentSong, 0);

        //            }
        //            catch (Exception ex)
        //            {
        //                _log.Warn("Music thread almost crashed.");
        //                _log.Warn(ex);
        //                await Task.Delay(3000).ConfigureAwait(false);
        //            }
        //            finally
        //            {
        //                if (!CancelToken.IsCancellationRequested)
        //                {
        //                    SongCancelSource.Cancel();
        //                }
        //                SongCancelSource = new CancellationTokenSource();
        //                CancelToken = SongCancelSource.Token;
        //                CurrentSong = null;
        //                await Task.Delay(300).ConfigureAwait(false);
        //            }
        //        }
        //    });

        //    t.Start();
        //}

        //public void Next()
        //{
        //    ActionQueue.Enqueue(() =>
        //    {
        //        Paused = false;
        //        SongCancelSource.Cancel();
        //    });
        //}

        //public void Stop()
        //{
        //    ActionQueue.Enqueue(() =>
        //    {
        //        RepeatPlaylist = false;
        //        RepeatSong = false;
        //        Autoplay = false;
        //        _playlist.Clear();
        //        if (!SongCancelSource.IsCancellationRequested)
        //            SongCancelSource.Cancel();
        //    });
        //}

        //public void TogglePause() => OnPauseChanged(Paused = !Paused);

        //public int SetVolume(int volume)
        //{
        //    if (volume < 0)
        //        volume = 0;
        //    if (volume > 100)
        //        volume = 100;

        //    Volume = volume / 100.0f;
        //    return volume;
        //}

        //private Song GetNextSong()
        //{
        //    if (!FairPlay)
        //    {
        //        return _playlist.FirstOrDefault();
        //    }
        //    var song = _playlist.FirstOrDefault(c => !RecentlyPlayedUsers.Contains(c.QueuerName))
        //        ?? _playlist.FirstOrDefault();

        //    if (song == null)
        //        return null;

        //    if (RecentlyPlayedUsers.Contains(song.QueuerName))
        //    {
        //        RecentlyPlayedUsers.Clear();
        //    }

        //    RecentlyPlayedUsers.Add(song.QueuerName);
        //    return song;
        //}

        //public void Shuffle()
        //{
        //    ActionQueue.Enqueue(() =>
        //    {
        //        var oldPlaylist = _playlist.ToArray();
        //        _playlist.Clear();
        //        _playlist.AddRange(oldPlaylist.Shuffle());
        //    });
        //}

        //public void AddSong(Song s, string username)
        //{
        //    if (s == null)
        //        throw new ArgumentNullException(nameof(s));
        //    ThrowIfQueueFull();
        //    ActionQueue.Enqueue(() =>
        //    {
        //        s.MusicPlayer = this;
        //        s.QueuerName = username.TrimTo(10);
        //        _playlist.Add(s);
        //    });
        //}

        //public void AddSong(Song s, int index)
        //{
        //    if (s == null)
        //        throw new ArgumentNullException(nameof(s));
        //    ActionQueue.Enqueue(() =>
        //    {
        //        _playlist.Insert(index, s);
        //    });
        //}

        //public void RemoveSong(Song s)
        //{
        //    if (s == null)
        //        throw new ArgumentNullException(nameof(s));
        //    ActionQueue.Enqueue(() =>
        //    {
        //        _playlist.Remove(s);
        //    });
        //}

        //public void RemoveSongAt(int index, bool silent = false)
        //{
        //    ActionQueue.Enqueue(() =>
        //    {
        //        if (index < 0 || index >= _playlist.Count)
        //            return;
        //        var song = _playlist.ElementAtOrDefault(index);
        //        if (_playlist.Remove(song) && !silent)
        //        {
        //            SongRemoved(song, index);
        //        }

        //    });
        //}

        //public void ClearQueue()
        //{
        //    ActionQueue.Enqueue(() =>
        //    {
        //        _playlist.Clear();
        //    });
        //}

        //public async Task UpdateSongDurationsAsync()
        //{
        //    var curSong = CurrentSong;
        //    var toUpdate = _playlist.Where(s => s.SongInfo.ProviderType == MusicType.Normal &&
        //                                                    s.TotalTime == TimeSpan.Zero)
        //                                                    .ToArray();
        //    if (curSong != null)
        //    {
        //        Array.Resize(ref toUpdate, toUpdate.Length + 1);
        //        toUpdate[toUpdate.Length - 1] = curSong;
        //    }
        //    var ids = toUpdate.Select(s => s.SongInfo.Query.Substring(s.SongInfo.Query.LastIndexOf("?v=") + 3))
        //                      .Distinct();

        //    var durations = await _google.GetVideoDurationsAsync(ids);

        //    toUpdate.ForEach(s =>
        //    {
        //        foreach (var kvp in durations)
        //        {
        //            if (s.SongInfo.Query.EndsWith(kvp.Key))
        //            {
        //                s.TotalTime = kvp.Value;
        //                return;
        //            }
        //        }
        //    });
        //}

        //public void Destroy()
        //{
        //    ActionQueue.Enqueue(async () =>
        //    {
        //        RepeatPlaylist = false;
        //        RepeatSong = false;
        //        Autoplay = false;
        //        Destroyed = true;
        //        _playlist.Clear();

        //        try { await AudioClient.StopAsync(); } catch { }
        //        if (!SongCancelSource.IsCancellationRequested)
        //            SongCancelSource.Cancel();
        //    });
        //}

        ////public async Task MoveToVoiceChannel(IVoiceChannel voiceChannel)
        ////{
        ////    if (audioClient?.ConnectionState != ConnectionState.Connected)
        ////        throw new InvalidOperationException("Can't move while bot is not connected to voice channel.");
        ////    PlaybackVoiceChannel = voiceChannel;
        ////    audioClient = await voiceChannel.ConnectAsync().ConfigureAwait(false);
        ////}

        //public bool ToggleRepeatSong() => RepeatSong = !RepeatSong;

        //public bool ToggleRepeatPlaylist() => RepeatPlaylist = !RepeatPlaylist;

        //public bool ToggleAutoplay() => Autoplay = !Autoplay;

        //public void ThrowIfQueueFull()
        //{
        //    if (MaxQueueSize == 0)
        //        return;
        //    if (_playlist.Count >= MaxQueueSize)
        //        throw new PlaylistFullException();
        //}
    }
}
