using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace NadekoBot.Classes
{
    internal class SpecificConfigurations
    {
        public static SpecificConfigurations Default { get; } = new SpecificConfigurations();
        public static bool Instantiated { get; private set; }

        private const string filePath = "data/ServerSpecificConfigs.json";

        static SpecificConfigurations() { }

        private SpecificConfigurations()
        {

            if (File.Exists(filePath))
            {
                try
                {
                    configs = JsonConvert
                        .DeserializeObject<ConcurrentDictionary<ulong, ServerSpecificConfig>>(
                            File.ReadAllText(filePath));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Deserialization failing: {ex}");
                }
            }
            if (configs == null)
                configs = new ConcurrentDictionary<ulong, ServerSpecificConfig>();
            Instantiated = true;
        }

        private readonly ConcurrentDictionary<ulong, ServerSpecificConfig> configs;

        public IEnumerable<ServerSpecificConfig> AllConfigs => configs.Values;

        public ServerSpecificConfig Of(ulong id) =>
            configs.GetOrAdd(id, _ => new ServerSpecificConfig());

        private readonly object saveLock = new object();

        public void Save()
        {
            lock (saveLock)
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(configs, Formatting.Indented));
            }
        }
    }

    internal class ServerSpecificConfig : INotifyPropertyChanged
    {
        [JsonProperty("VoicePlusTextEnabled")]
        private bool voicePlusTextEnabled;
        [JsonIgnore]
        public bool VoicePlusTextEnabled {
            get { return voicePlusTextEnabled; }
            set {
                voicePlusTextEnabled = value;
                OnPropertyChanged();
            }
        }
        [JsonProperty("SendPrivateMessageOnMention")]
        private bool sendPrivateMessageOnMention;
        [JsonIgnore]
        public bool SendPrivateMessageOnMention {
            get { return sendPrivateMessageOnMention; }
            set {
                sendPrivateMessageOnMention = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        private ObservableCollection<ulong> listOfSelfAssignableRoles;
        public ObservableCollection<ulong> ListOfSelfAssignableRoles {
            get { return listOfSelfAssignableRoles; }
            set {
                listOfSelfAssignableRoles = value;
                if (value != null)
                    listOfSelfAssignableRoles.CollectionChanged += (s, e) =>
                    {
                        if (!SpecificConfigurations.Instantiated) return;
                        OnPropertyChanged();
                    };
            }
        }

        [JsonIgnore]
        private ulong autoAssignedRole = 0;
        public ulong AutoAssignedRole {
            get { return autoAssignedRole; }
            set {
                autoAssignedRole = value;
                if (!SpecificConfigurations.Instantiated) return;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        private ObservableCollection<StreamNotificationConfig> observingStreams;
        public ObservableCollection<StreamNotificationConfig> ObservingStreams {
            get { return observingStreams; }
            set {
                observingStreams = value;
                if (value != null)
                    observingStreams.CollectionChanged += (s, e) =>
                    {
                        if (!SpecificConfigurations.Instantiated) return;
                        OnPropertyChanged();
                    };
            }
        }

        public ServerSpecificConfig()
        {
            ListOfSelfAssignableRoles = new ObservableCollection<ulong>();
            ObservingStreams = new ObservableCollection<StreamNotificationConfig>();
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { SpecificConfigurations.Default.Save(); };

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StreamNotificationConfig : IEquatable<StreamNotificationConfig>
    {
        public string Username { get; set; }
        public StreamType Type { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public bool LastStatus { get; set; }

        public enum StreamType
        {
            Twitch,
            Beam,
            Hitbox,
            YoutubeGaming
        }

        public bool Equals(StreamNotificationConfig other) =>
            this.Username.ToLower().Trim() == other.Username.ToLower().Trim() &&
            this.Type == other.Type &&
            this.ServerId == other.ServerId;

        public override int GetHashCode()
        {
            return (int)((int)ServerId + Username.Length + (int)Type);
        }
    }
}
