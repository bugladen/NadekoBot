// ReSharper disable InconsistentNaming
namespace NadekoBot.Classes.JSONModels
{
    public class Credentials
    {
        public string Username;
        public string Password;
        public string BotId;
        public string GoogleAPIKey;
        public ulong[] OwnerIds;
        public string TrelloAppKey;
        public bool? ForwardMessages;
        public string SoundCloudClientID;
        public string MashapeKey;
        public string LOLAPIKey;
        public bool DontJoinServers = false;
    }
}