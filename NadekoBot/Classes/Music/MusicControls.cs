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
                        if ((CurrentSong == null && SongQueue.Count>0) ||
                            CurrentSong?.State == StreamState.Completed) {
                            LoadNextSong();
                        }
                    } catch (Exception e) {
                        Console.WriteLine("Bug in music task run. " + e);
                    }
                    await Task.Delay(200);
                }
            });
        }

        public void LoadNextSong() {
            Console.WriteLine("Loading next song.");
            if (SongQueue.Count == 0) {
                CurrentSong = null;
                return;
            }
            CurrentSong = SongQueue[0];
            SongQueue.RemoveAt(0);
            CurrentSong.Start();
            Console.WriteLine("Starting next song.");
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
