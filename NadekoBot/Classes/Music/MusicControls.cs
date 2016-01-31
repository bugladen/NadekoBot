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

        public bool Pause = false;
        public List<StreamRequest> SongQueue = new List<StreamRequest>();
        public StreamRequest CurrentSong = null;

        public bool IsPaused { get; internal set; } = false;
        public bool Stopped { get; private set; }

        public Channel VoiceChannel;

        public IAudioClient VoiceClient = null;

        private readonly object _voiceLock = new object();

        public MusicControls() {
            Task.Run(async () => {
                while (!Stopped) {
                    lock (_voiceLock) {
                        if (CurrentSong == null) {
                            if (SongQueue.Count > 0)
                                LoadNextSong().Wait();

                        } else if (CurrentSong.State == StreamState.Completed || NextSong) {
                            NextSong = false;
                            LoadNextSong().Wait();
                        }

                    }
                    await Task.Delay(1000);
                }
            });
        }

        public MusicControls(Channel voiceChannel) : this() {
            VoiceChannel = voiceChannel;
        }

        public async Task LoadNextSong() {
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

            try {
                if (VoiceChannel == null)
                    VoiceChannel = CurrentSong.Channel;
                if (VoiceClient == null)
                    VoiceClient = await NadekoBot.client.Audio().Join(VoiceChannel);
                await CurrentSong.Start();
            } catch (Exception ex) {
                Console.WriteLine($"Starting failed: {ex}");
                CurrentSong?.Stop();
            }
        }

        internal void Stop() {
            lock (_voiceLock) {
                Stopped = true;
                foreach (var kvp in SongQueue) {
                    if (kvp != null)
                        kvp.Stop();
                }
                SongQueue.Clear();
                CurrentSong?.Stop();
                CurrentSong = null;
                VoiceClient?.Disconnect();
                VoiceClient = null;
            }
        }

        internal bool TogglePause() => IsPaused = !IsPaused;
    }
}
