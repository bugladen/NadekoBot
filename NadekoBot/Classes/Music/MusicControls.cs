using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot.Classes.Music {
    public class MusicControls {
        public bool NextSong = false;
        public IAudioClient Voice;
        public Channel VoiceChannel;
        public bool Pause = false;
        public List<StreamRequest> SongQueue = new List<StreamRequest>();
        public StreamRequest CurrentSong;

        public bool IsPaused { get; internal set; } = false;
        public bool Stopped { get; private set; }

        public IAudioClient VoiceClient;

        private readonly object _voiceLock = new object();

        public MusicControls() {
            Task.Run(async () => {
                while (!Stopped) {
                    try {
                        lock (_voiceLock) {
                            if (CurrentSong == null) {
                                if (SongQueue.Count > 0)
                                    LoadNextSong();

                            } else if (CurrentSong.State == StreamState.Completed || NextSong) {
                                NextSong = false;
                                LoadNextSong();
                            }
                        }
                    } catch (Exception e) {
                        Console.WriteLine("Bug in music task run. " + e);
                    }
                    await Task.Delay(500);
                }
            });
        }

        public MusicControls(Channel voiceChannel) : this() {
            VoiceChannel = voiceChannel;
        }

        public void LoadNextSong() {
            lock (_voiceLock) {
                CurrentSong?.Stop();
                CurrentSong = null;
                if (SongQueue.Count != 0) {
                    CurrentSong = SongQueue[0];
                    SongQueue.RemoveAt(0);
                } else {
                    VoiceClient?.Disconnect();
                    VoiceClient = null;
                    return;
                }
            }

            try {
                CurrentSong?.Start();
            } catch (Exception ex) {
                Console.WriteLine($"Starting failed: {ex}");
                CurrentSong?.Stop();
            }
        }

        internal void Stop() {
            lock (_voiceLock) {
                Stopped = true;
                foreach (var kvp in SongQueue) {
                    if(kvp != null)
                        kvp.Cancel();
                }
                SongQueue.Clear();
                CurrentSong?.Stop();
                CurrentSong = null;
                VoiceClient?.Disconnect();
                VoiceClient = null;
            }
        }

        internal async Task<StreamRequest> CreateStreamRequest(CommandEventArgs e, string query, Channel voiceChannel) {
            if (VoiceChannel == null)
                throw new ArgumentNullException("Please join a voicechannel.");
            StreamRequest sr = null;
            if (VoiceClient == null) {
                VoiceChannel = voiceChannel;
                VoiceClient = await NadekoBot.client.Audio().Join(VoiceChannel);
            }
            sr = new StreamRequest(e, query, this);

            lock (_voiceLock) {
                SongQueue.Add(sr);
            }
            return sr;
        }

        internal bool TogglePause() => IsPaused = !IsPaused;
    }
}
