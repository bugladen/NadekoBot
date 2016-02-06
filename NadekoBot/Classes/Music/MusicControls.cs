using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using MusicModule = NadekoBot.Modules.Music;

namespace NadekoBot.Classes.Music {
    public class MusicControls {
        private CommandEventArgs _e;
        public bool NextSong = false;
        public IAudioClient Voice;

        public bool Pause = false;
        public List<StreamRequest> SongQueue = new List<StreamRequest>();
        public StreamRequest CurrentSong = null;
        public float Volume { get; set; } = .5f;

        public bool IsPaused { get; internal set; } = false;
        public bool Stopped { get; private set; }

        public Channel VoiceChannel = null;

        public IAudioClient VoiceClient = null;

        private readonly object _voiceLock = new object();

        public MusicControls() {
            Task.Run(async () => {
                while (!Stopped) {
                    if (CurrentSong == null) {
                        if (SongQueue.Count > 0)
                            await LoadNextSong();

                    } else if (CurrentSong.State == StreamState.Completed || NextSong) {
                        NextSong = false;
                        await LoadNextSong();
                    }
                    await Task.Delay(500);
                }
            });
        }

        public MusicControls(Channel voiceChannel, CommandEventArgs e) : this() {
            if (voiceChannel == null)
                throw new ArgumentNullException(nameof(voiceChannel));
            VoiceChannel = voiceChannel;
            _e = e;
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
                if (VoiceClient == null) {
                    Console.WriteLine($"Joining voice channel [{DateTime.Now.Second}]");
                    //todo add a new event, to tell people nadeko is trying to join
                    VoiceClient = await NadekoBot.client.Audio().Join(VoiceChannel);
                    Console.WriteLine($"Joined voicechannel [{DateTime.Now.Second}]");
                }
                await Task.Factory.StartNew(async () => await CurrentSong?.Start(), TaskCreationOptions.LongRunning).Unwrap();
            } catch (Exception ex) {
                Console.WriteLine($"Starting failed: {ex}");
                CurrentSong?.Stop();
                CurrentSong = null;
            }
        }

        internal void Stop() {
            Stopped = true;
            SongQueue.Clear();
            CurrentSong?.Stop();
            CurrentSong = null;
            VoiceClient?.Disconnect();
            VoiceClient = null;

            MusicControls throwAwayValue;
            MusicModule.musicPlayers.TryRemove(_e.Server, out throwAwayValue);
        }

        public int SetVolume(int value) {
            if (value < 0)
                value = 0;
            if (value > 150)
                value = 150;
            this.Volume = value/100f;
            return value;
        }

        internal bool TogglePause() => IsPaused = !IsPaused;
    }
}
