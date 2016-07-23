using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
                            File.ReadAllText(filePath), new JsonSerializerSettings()
                            {
                                Error = (s, e) =>
                                {
                                    if (e.ErrorContext.Member.ToString() == "GenerateCurrencyChannels")
                                    {
                                        e.ErrorContext.Handled = true;
                                    }
                                }
                            });
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

        private readonly SemaphoreSlim saveLock = new SemaphoreSlim(1, 1);

        public async Task Save()
        {
            await saveLock.WaitAsync();
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(configs, Formatting.Indented));
            }
            finally
            {
                saveLock.Release();
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
                if (!SpecificConfigurations.Instantiated) return;
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
                if (!SpecificConfigurations.Instantiated) return;
                OnPropertyChanged();
            }
        }

        [JsonProperty("LogChannel")]
        private ulong? logServerChannel = null;
        [JsonIgnore]
        public ulong? LogServerChannel {
            get { return logServerChannel; }
            set {
                logServerChannel = value;
                if (!SpecificConfigurations.Instantiated) return;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        private ObservableCollection<ulong> logserverIgnoreChannels;
        public ObservableCollection<ulong> LogserverIgnoreChannels {
            get { return logserverIgnoreChannels; }
            set {
                logserverIgnoreChannels = value;
                if (value != null)
                    logserverIgnoreChannels.CollectionChanged += (s, e) =>
                    {
                        if (!SpecificConfigurations.Instantiated) return;
                        OnPropertyChanged();
                    };
            }
        }

        [JsonProperty("LogPresenceChannel")]
        private ulong? logPresenceChannel = null;
        [JsonIgnore]
        public ulong? LogPresenceChannel {
            get { return logPresenceChannel; }
            set {
                logPresenceChannel = value;
                if (!SpecificConfigurations.Instantiated) return;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        private ObservableConcurrentDictionary<ulong, ulong> voiceChannelLog;
        public ObservableConcurrentDictionary<ulong, ulong> VoiceChannelLog {
            get { return voiceChannelLog; }
            set {
                voiceChannelLog = value;
                if (value != null)
                    voiceChannelLog.CollectionChanged += (s, e) =>
                    {
                        if (!SpecificConfigurations.Instantiated) return;
                        OnPropertyChanged();
                    };
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
        private ObservableConcurrentDictionary<ulong, int> generateCurrencyChannels;
        public ObservableConcurrentDictionary<ulong, int> GenerateCurrencyChannels {
            get { return generateCurrencyChannels; }
            set {
                generateCurrencyChannels = value;
                if (value != null)
                    generateCurrencyChannels.CollectionChanged += (s, e) =>
                    {
                        if (!SpecificConfigurations.Instantiated) return;
                        OnPropertyChanged();
                    };
            }
        }

        [JsonIgnore]
        private bool autoDeleteMessagesOnCommand = false;
        public bool AutoDeleteMessagesOnCommand {
            get { return autoDeleteMessagesOnCommand; }
            set {
                autoDeleteMessagesOnCommand = value;
                if (!SpecificConfigurations.Instantiated) return;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        private bool exclusiveSelfAssignedRoles = false;
        public bool ExclusiveSelfAssignedRoles
        {
            get { return exclusiveSelfAssignedRoles; }
            set
            {
                exclusiveSelfAssignedRoles = value;
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

        [JsonIgnore]
        private float defaultMusicVolume = 1f;
        public float DefaultMusicVolume {
            get { return defaultMusicVolume; }
            set {
                defaultMusicVolume = value;
                if (!SpecificConfigurations.Instantiated) return;
                OnPropertyChanged();
            }
        }

        public ServerSpecificConfig()
        {
            ListOfSelfAssignableRoles = new ObservableCollection<ulong>();
            ObservingStreams = new ObservableCollection<StreamNotificationConfig>();
            GenerateCurrencyChannels = new ObservableConcurrentDictionary<ulong, int>();
            VoiceChannelLog = new ObservableConcurrentDictionary<ulong, ulong>();
            LogserverIgnoreChannels = new ObservableCollection<ulong>();
        }

        public event PropertyChangedEventHandler PropertyChanged = async delegate { await SpecificConfigurations.Default.Save().ConfigureAwait(false); };

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
            return (int)ServerId + Username.Length + (int)Type;
        }
    }
}
