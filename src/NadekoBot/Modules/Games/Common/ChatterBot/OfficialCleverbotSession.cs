using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Common.ChatterBot
{
    public class OfficialCleverbotSession : IChatterBotSession
    {
        private readonly string _apiKey;
        private string _cs = null;

        private string queryString => $"https://www.cleverbot.com/getreply?key={_apiKey}" +
            "&wrapper=nadekobot" +
            "&input={0}" +
            "&cs={1}";

        public OfficialCleverbotSession(string apiKey)
        {
            this._apiKey = apiKey;
        }

        public async Task<string> Think(string input)
        {
            using (var http = new HttpClient())
            {
                var dataString = await http.GetStringAsync(string.Format(queryString, input, _cs ?? "")).ConfigureAwait(false);
                var data = JsonConvert.DeserializeObject<CleverbotResponse>(dataString);
                _cs = data?.Cs;
                return data?.Output;
            }
        }
    }
}
