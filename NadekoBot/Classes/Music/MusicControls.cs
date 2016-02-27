using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using MusicModule = NadekoBot.Modules.Music;
using System.Collections;
using NadekoBot.Extensions;
using System.Threading;

namespace NadekoBot.Classes.Music {

    public enum MusicType {
        Radio,
        Normal,
        Local
    }
    public class Song {
        public StreamState State { get; internal set; }

        private Song() { }

        internal Task Play(CancellationToken cancelToken) {
            throw new NotImplementedException();
        }
    }
    public class MusicPlayer {
        private IAudioClient _client { get; set; }

        private List<Song> _playlist = new List<Song>();
        public IReadOnlyCollection<Song> Playlist => _playlist;
        private object playlistLock = new object();

        public Song CurrentSong { get; set; } = default(Song);
        private CancellationTokenSource SongCancelSource { get; set; }
        private CancellationToken cancelToken { get; set; }

        public bool Paused { get; set; }

        public float Volume { get; private set; }

        public MusicPlayer(IAudioClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            _client = client;
            SongCancelSource = new CancellationTokenSource();
            cancelToken = SongCancelSource.Token;
            Task.Run(async () => {
                while (_client?.State == ConnectionState.Connected) {
                    CurrentSong = GetNextSong();
                    if (CurrentSong != null) {
                        try {
                            await CurrentSong.Play(cancelToken);
                        }
                        catch (OperationCanceledException) {
                            Console.WriteLine("Song canceled");
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Exception in PlaySong: {ex}");
                        }
                        SongCancelSource = new CancellationTokenSource();
                        cancelToken = SongCancelSource.Token;
                    }
                    else {
                        await Task.Delay(1000);
                    }
                }
                await Stop();
            });
        }

        public void Next() {
            if(!SongCancelSource.IsCancellationRequested)
                SongCancelSource.Cancel();
        }

        public async Task Stop() {

            lock (_playlist) {
                _playlist.Clear();
            }
            try {
                if (!SongCancelSource.IsCancellationRequested)
                    SongCancelSource.Cancel();
            }
            catch {
                Console.WriteLine("This shouldn't happen");
            }
            await _client?.Disconnect();
        }

        public void Shuffle() {
            lock (_playlist) {
                _playlist.Shuffle();
            }
        }

        public void SetVolume(float volume) {
            if (volume < 0)
                volume = 0;
            if (volume > 150)
                volume = 150;

            Volume = volume / 100.0f;
        }

        private Song GetNextSong() {
            lock (playlistLock) {
                if (_playlist.Count == 0)
                    return null;
                var toReturn = _playlist[0];
                _playlist.RemoveAt(0);
                return toReturn;
            }
        }

        public void AddSong(Song s) {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            lock (playlistLock) {
                _playlist.Add(s);
            }
        }

        public void RemoveSong(Song s) {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            lock (playlistLock) {
                _playlist.Remove(s);
            }
        }

        public void RemoveSongAt(int index) {
            lock (playlistLock) {
                if (index < 0 || index >= _playlist.Count)
                    throw new ArgumentException("Invalid index");
                _playlist.RemoveAt(index);
            }
        }

        /*
        private CommandEventArgs _e;
        public bool NextSong { get; set; } = false;
        public IAudioClient Voice { get; set; }

        public bool Pause { get; set; } = false;
        public List<StreamRequest> SongQueue { get; set; } = new List<StreamRequest>();
        public StreamRequest CurrentSong { get; set; } = null;
        public float Volume { get; set; } = .5f;

        public bool IsPaused { get; internal set; } = false;
        public bool Stopped { get; private set; }

        public Channel VoiceChannel { get; set; } = null;

        public IAudioClient VoiceClient { get; set; } = null;

        private readonly object _voiceLock = new object();

        public MusicPlayer() {
            Task.Run(async () => {
                while (true) {
                    if (!Stopped) {
                        if (CurrentSong == null) {
                            if (SongQueue.Count > 0)
                                await LoadNextSong();

                        }
                        else if (CurrentSong.State == StreamState.Completed || NextSong) {
                            NextSong = false;
                            await LoadNextSong();
                        }
                    }
                    else if (VoiceClient == null)
                        break;
                    await Task.Delay(500);
                }
            });
        }

        internal void AddSong(StreamRequest streamRequest) {
            lock (_voiceLock) {
                Stopped = false;
                this.SongQueue.Add(streamRequest);
            }
        }

        public MusicPlayer(Channel voiceChannel, CommandEventArgs e, float? vol) : this() {
            if (voiceChannel == null)
                throw new ArgumentNullException(nameof(voiceChannel));
            if (vol != null)
                Volume = (float)vol;
            VoiceChannel = voiceChannel;
            _e = e;
        }

        public async Task LoadNextSong() {
            CurrentSong?.Stop();
            CurrentSong = null;
            if (SongQueue.Count != 0) {
                lock (_voiceLock) {
                    CurrentSong = SongQueue[0];
                    SongQueue.RemoveAt(0);
                }
            }
            else {
                Stop();
                return;
            }

            try {
                if (VoiceClient == null) {
                    Console.WriteLine($"Joining voice channel [{DateTime.Now.Second}]");
                    //todo add a new event, to tell people nadeko is trying to join
                    VoiceClient = await Task.Run(async () => await VoiceChannel.JoinAudio());
                    Console.WriteLine($"Joined voicechannel [{DateTime.Now.Second}]");
                }
                await Task.Factory.StartNew(async () => await CurrentSong?.Start(), TaskCreationOptions.LongRunning).Unwrap();
            }
            catch (Exception ex) {
                Console.WriteLine($"Starting failed: {ex}");
                CurrentSong?.Stop();
            }
        }

        internal void Stop(bool leave = false) {
            Stopped = true;
            SongQueue.Clear();
            try {
                CurrentSong?.Stop();
            }
            catch { }
            CurrentSong = null;
            if (leave) {
                VoiceClient?.Disconnect();
                VoiceClient = null;

                MusicPlayer throwAwayValue;
                MusicModule.musicPlayers.TryRemove(_e.Server, out throwAwayValue);
            }
        }

        public int SetVolume(int value) {
            if (value < 0)
                value = 0;
            if (value > 150)
                value = 150;
            this.Volume = value / 100f;
            return value;
        }

        internal bool TogglePause() => IsPaused = !IsPaused;
        */
    }
}
