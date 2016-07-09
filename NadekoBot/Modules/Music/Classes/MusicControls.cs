using Discord;
using Discord.Audio;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
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
        Buffering, //not using it atm
        Playing,
        Completed
    }

    public class MusicPlayer
    {
        public static int MaximumPlaylistSize => 50;

        private IAudioClient audioClient { get; set; }

        private readonly List<Song> playlist = new List<Song>();
        public IReadOnlyCollection<Song> Playlist => playlist;
        private readonly object playlistLock = new object();

        public Song CurrentSong { get; set; } = default(Song);
        private CancellationTokenSource SongCancelSource { get; set; }
        private CancellationToken cancelToken { get; set; }

        public bool Paused { get; set; }

        public float Volume { get; private set; }

        public event EventHandler<Song> OnCompleted = delegate { };
        public event EventHandler<Song> OnStarted = delegate { };

        public Channel PlaybackVoiceChannel { get; private set; }

        private bool Destroyed { get; set; } = false;
        public bool RepeatSong { get; private set; } = false;
        public bool RepeatPlaylist { get; private set; } = false;
        public bool Autoplay { get; set; } = false;
        public uint MaxQueueSize { get; set; } = 0;

        public MusicPlayer(Channel startingVoiceChannel, float? defaultVolume)
        {
            if (startingVoiceChannel == null)
                throw new ArgumentNullException(nameof(startingVoiceChannel));
            if (startingVoiceChannel.Type != ChannelType.Voice)
                throw new ArgumentException("Channel must be of type voice");
            Volume = defaultVolume ?? 1.0f;

            PlaybackVoiceChannel = startingVoiceChannel;
            SongCancelSource = new CancellationTokenSource();
            cancelToken = SongCancelSource.Token;

            Task.Run(async () =>
            {
                while (!Destroyed)
                {
                    try
                    {
                        if (audioClient?.State != ConnectionState.Connected)
                            audioClient = await PlaybackVoiceChannel.JoinAudio().ConfigureAwait(false);
                    }
                    catch
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        continue;
                    }
                    CurrentSong = GetNextSong();
                    var curSong = CurrentSong;
                    if (curSong != null)
                    {
                        try
                        {
                            OnStarted(this, curSong);
                            await curSong.Play(audioClient, cancelToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine("Song canceled");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception in PlaySong: {ex}");
                        }
                        OnCompleted(this, curSong);
                        curSong = CurrentSong; //to check if its null now
                        if (curSong != null)
                            if (RepeatSong)
                                playlist.Insert(0, curSong);
                            else if (RepeatPlaylist)
                                playlist.Insert(playlist.Count, curSong);
                        SongCancelSource = new CancellationTokenSource();
                        cancelToken = SongCancelSource.Token;
                    }
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            });
        }

        public void Next()
        {
            lock (playlistLock)
            {
                if (!SongCancelSource.IsCancellationRequested)
                {
                    Paused = false;
                    SongCancelSource.Cancel();
                }
            }
        }

        public void Stop()
        {
            lock (playlistLock)
            {
                playlist.Clear();
                CurrentSong = null;
                RepeatPlaylist = false;
                RepeatSong = false;
                if (!SongCancelSource.IsCancellationRequested)
                    SongCancelSource.Cancel();
            }
        }

        public void TogglePause() => Paused = !Paused;

        public void Shuffle()
        {
            lock (playlistLock)
            {
                playlist.Shuffle();
            }
        }

        public int SetVolume(int volume)
        {
            if (volume < 0)
                volume = 0;
            if (volume > 100)
                volume = 100;

            Volume = volume / 100.0f;
            return volume;
        }

        private Song GetNextSong()
        {
            lock (playlistLock)
            {
                if (playlist.Count == 0)
                    return null;
                var toReturn = playlist[0];
                playlist.RemoveAt(0);
                return toReturn;
            }
        }

        public void AddSong(Song s, string username)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            ThrowIfQueueFull();
            lock (playlistLock)
            {
                s.MusicPlayer = this;
                s.QueuerName = username.TrimTo(10);
                playlist.Add(s);
            }
        }

        public void AddSong(Song s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            lock (playlistLock)
            {
                playlist.Insert(index, s);
            }
        }

        public void RemoveSong(Song s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            lock (playlistLock)
            {
                playlist.Remove(s);
            }
        }

        public void RemoveSongAt(int index)
        {
            lock (playlistLock)
            {
                if (index < 0 || index >= playlist.Count)
                    throw new ArgumentException("Invalid index");
                playlist.RemoveAt(index);
            }
        }

        internal Task MoveToVoiceChannel(Channel voiceChannel)
        {
            if (audioClient?.State != ConnectionState.Connected)
                throw new InvalidOperationException("Can't move while bot is not connected to voice channel.");
            PlaybackVoiceChannel = voiceChannel;
            return PlaybackVoiceChannel.JoinAudio();
        }

        internal void ClearQueue()
        {
            lock (playlistLock)
            {
                playlist.Clear();
            }
        }

        public void Destroy()
        {
            lock (playlistLock)
            {
                playlist.Clear();
                Destroyed = true;
                CurrentSong = null;
                if (!SongCancelSource.IsCancellationRequested)
                    SongCancelSource.Cancel();
                audioClient.Disconnect();
            }
        }

        internal bool ToggleRepeatSong() => this.RepeatSong = !this.RepeatSong;

        internal bool ToggleRepeatPlaylist() => this.RepeatPlaylist = !this.RepeatPlaylist;

        internal bool ToggleAutoplay() => this.Autoplay = !this.Autoplay;

        internal void ThrowIfQueueFull()
        {
            if (MaxQueueSize == 0)
                return;
            if (playlist.Count >= MaxQueueSize)
                throw new PlaylistFullException();
        }
    }
}
