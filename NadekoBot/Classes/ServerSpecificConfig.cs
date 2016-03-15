using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace NadekoBot.Classes {
    internal class SpecificConfigurations {
        public static SpecificConfigurations Default { get; } = new SpecificConfigurations();
        public static bool Instantiated { get; set; } = false;

        private const string filePath = "data/ServerSpecificConfigs.json";

        static SpecificConfigurations() { }

        private SpecificConfigurations() {

            if (File.Exists(filePath)) {
                try {
                    configs = JsonConvert
                        .DeserializeObject<ConcurrentDictionary<ulong, ServerSpecificConfig>>(
                            File.ReadAllText(filePath));
                } catch (Exception ex) {
                    Console.WriteLine($"Deserialization failing: {ex}");
                }
            }
            if (configs == null)
                configs = new ConcurrentDictionary<ulong, ServerSpecificConfig>();
            Instantiated = true;
        }

        private readonly ConcurrentDictionary<ulong, ServerSpecificConfig> configs;

        public ServerSpecificConfig Of(ulong id) =>
            configs.GetOrAdd(id, _ => new ServerSpecificConfig());

        private readonly object saveLock = new object();

        public void Save() {
            lock (saveLock) {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(configs, Formatting.Indented));
            }
        }
    }

    internal class ServerSpecificConfig : INotifyPropertyChanged {
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
                    listOfSelfAssignableRoles.CollectionChanged += (s, e) => {
                        if (!SpecificConfigurations.Instantiated) return;
                        OnPropertyChanged();
                    };
            }
        }

        public ServerSpecificConfig() {
            ListOfSelfAssignableRoles = new ObservableCollection<ulong>();
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { SpecificConfigurations.Default.Save(); };

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            Console.WriteLine("property changed");
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
