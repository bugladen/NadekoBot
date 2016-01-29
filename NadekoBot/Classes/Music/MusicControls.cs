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

        public bool IsPaused { get; internal set; }
        public IAudioClient VoiceClient;

        private readonly object _voiceLock = new object();

        public MusicControls() {
            Task.Run(async () => {
                while (true) {
                    try {
                        if (CurrentSong == null) {
                            if (SongQueue.Count > 0)
                                LoadNextSong();

                        } else if (CurrentSong.State == StreamState.Completed) {
                            LoadNextSong();
                        }
                    } catch (Exception e) {
                        Console.WriteLine("Bug in music task run. " + e);
                    }
                    await Task.Delay(200);
                }
            });
        }

        public MusicControls(Channel voiceChannel) : this() {
            VoiceChannel = voiceChannel;
        }

        public void LoadNextSong() {
            Console.WriteLine("Loading next song.");
            lock (_voiceLock) {
                if (SongQueue.Count == 0) {
                    CurrentSong = null;
                    return;
                }
                CurrentSong = SongQueue[0];
                SongQueue.RemoveAt(0);
            }
            CurrentSong.Start();

            Console.WriteLine("Starting next song.");
        }

        internal void RemoveAllSongs() {
            lock (_voiceLock) {
                foreach (var kvp in SongQueue) {
                    if(kvp != null)
                        kvp.Cancel();
                }
                SongQueue.Clear();
                VoiceClient.Disconnect();
                VoiceClient = null;
            }
        }

        internal StreamRequest CreateStreamRequest(CommandEventArgs e, string query, Channel voiceChannel) {
            lock (_voiceLock) {
                if (VoiceClient == null)
                    VoiceClient = NadekoBot.client.Audio().Join(VoiceChannel).Result;
                return new StreamRequest(e, query, VoiceClient);
            }
        }
    }
}
