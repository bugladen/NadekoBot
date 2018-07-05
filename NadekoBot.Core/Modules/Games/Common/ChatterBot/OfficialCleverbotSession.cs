using NadekoBot.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Common.ChatterBot
{
    public class OfficialCleverbotSession : IChatterBotSession
    {
        private readonly string _apiKey;
        private string _cs = null;

        private string QueryString => $"https://www.cleverbot.com/getreply?key={_apiKey}" +
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
                var dataString = await http.GetStringAsync(string.Format(QueryString, input, _cs ?? "")).ConfigureAwait(false);
                var data = JsonConvert.DeserializeObject<CleverbotResponse>(dataString);
                _cs = data?.Cs;
                return data?.Output;
            }
        }
    }

    public class CleverbotIOSession : IChatterBotSession
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _key;
        private readonly string _user;

        private AsyncLazy<string> _nick;

        private readonly string _createEndpoint = $"https://cleverbot.io/1.0/create";
        private readonly string _askEndpoint = $"https://cleverbot.io/1.0/ask";

        public CleverbotIOSession(string user, string key)
        {
            this._key = key;
            this._user = user;

            _nick = new AsyncLazy<string>((Func<Task<string>>)GetNick);
        }

        private async Task<string> GetNick()
        {
            var msg = new FormUrlEncodedContent(new[]
           {
                new KeyValuePair<string, string>("user", _user),
                new KeyValuePair<string, string>("key", _key),
            });
            var data = await _http.PostAsync(_createEndpoint, msg).ConfigureAwait(false);
            var str = await data.Content.ReadAsStringAsync().ConfigureAwait(false);
            var obj = JsonConvert.DeserializeObject<CleverbotIOCreateResponse>(str);
            if (obj.Status != "success")
                throw new OperationCanceledException(obj.Status);

            return obj.Nick;
        }

        public async Task<string> Think(string input)
        {
            var msg = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("user", _user),
                new KeyValuePair<string, string>("key", _key),
                new KeyValuePair<string, string>("nick", await _nick),
                new KeyValuePair<string, string>("text", input),
            });
            var data = await _http.PostAsync(_askEndpoint, msg).ConfigureAwait(false);
            var str = await data.Content.ReadAsStringAsync().ConfigureAwait(false);
            var obj = JsonConvert.DeserializeObject<CleverbotIOAskResponse>(str);
            if (obj.Status != "success")
                throw new OperationCanceledException(obj.Status);

            return obj.Response;
        }
    }
}
