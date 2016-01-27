using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NadekoBot.Classes.Music {
    public class MusicControls {
        public bool NextSong = false;
        public IAudioClient Voice;
        public Channel VoiceChannel;
        public bool Pause = false;
        public List<StreamRequest> SongQueue = new List<StreamRequest>();
        public StreamRequest CurrentSong;

        public bool IsPaused { get; internal set; }

        public MusicControls() {
            Task.Run(async () => {
                while (true) {
                    try {
                        if (CurrentSong == null || CurrentSong.State == StreamState.Completed) {
                            LoadNextSong();
                        }
                    } catch (Exception e) {
                        Console.WriteLine("Bug in music task run. " + e);
                    }
                    await Task.Delay(200);
                }
            });
        }

        private void LoadNextSong() {
            if (SongQueue.Count == 0) {
                if (CurrentSong != null)
                    CurrentSong.Cancel();
                CurrentSong = null;
                return;
            }
            CurrentSong = SongQueue[0];
            SongQueue.RemoveAt(0);
            CurrentSong.Start();
            Console.WriteLine("starting");
        }

        internal void RemoveAllSongs() {
            lock (SongQueue) {
                foreach (var kvp in SongQueue) {
                    if(kvp != null)
                        kvp.Cancel();
                }
                SongQueue.Clear();
            }
        }
    }
}
