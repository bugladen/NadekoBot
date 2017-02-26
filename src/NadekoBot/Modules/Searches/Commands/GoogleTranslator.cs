using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public class GoogleTranslator
    {
        private static GoogleTranslator _instance;
        public static GoogleTranslator Instance = _instance ?? (_instance = new GoogleTranslator());

        public IEnumerable<string> Languages => _languageDictionary.Keys.OrderBy(x => x);
        private readonly Dictionary<string, string> _languageDictionary;

        static GoogleTranslator() { }
        private GoogleTranslator() {
            _languageDictionary = new Dictionary<string, string>() {
                    { "afrikaans", "af"},
                    { "albanian", "sq"},
                    { "arabic", "ar"},
                    { "armenian", "hy"},
                    { "azerbaijani", "az"},
                    { "basque", "eu"},
                    { "belarusian", "be"},
                    { "bengali", "bn"},
                    { "bulgarian", "bg"},
                    { "catalan", "ca"},
                    { "chinese-traditional", "zh-TW"},
                    { "chinese-simplified", "zh-CN"},
                    { "chinese", "zh-CN"},
                    { "croatian", "hr"},
                    { "czech", "cs"},
                    { "danish", "da"},
                    { "dutch", "nl"},
                    { "english", "en"},
                    { "esperanto", "eo"},
                    { "estonian", "et"},
                    { "filipino", "tl"},
                    { "finnish", "fi"},
                    { "french", "fr"},
                    { "galician", "gl"},
                    { "german", "de"},
                    { "georgian", "ka"},
                    { "greek", "el"},
                    { "haitian Creole", "ht"},
                    { "hebrew", "iw"},
                    { "hindi", "hi"},
                    { "hungarian", "hu"},
                    { "icelandic", "is"},
                    { "indonesian", "id"},
                    { "irish", "ga"},
                    { "italian", "it"},
                    { "japanese", "ja"},
                    { "korean", "ko"},
                    { "lao", "lo"},
                    { "latin", "la"},
                    { "latvian", "lv"},
                    { "lithuanian", "lt"},
                    { "macedonian", "mk"},
                    { "malay", "ms"},
                    { "maltese", "mt"},
                    { "norwegian", "no"},
                    { "persian", "fa"},
                    { "polish", "pl"},
                    { "portuguese", "pt"},
                    { "romanian", "ro"},
                    { "russian", "ru"},
                    { "serbian", "sr"},
                    { "slovak", "sk"},
                    { "slovenian", "sl"},
                    { "spanish", "es"},
                    { "swahili", "sw"},
                    { "swedish", "sv"},
                    { "tamil", "ta"},
                    { "telugu", "te"},
                    { "thai", "th"},
                    { "turkish", "tr"},
                    { "ukrainian", "uk"},
                    { "urdu", "ur"},
                    { "vietnamese", "vi"},
                    { "welsh", "cy"},
                    { "yiddish", "yi"},

                    { "af", "af"},
                    { "sq", "sq"},
                    { "ar", "ar"},
                    { "hy", "hy"},
                    { "az", "az"},
                    { "eu", "eu"},
                    { "be", "be"},
                    { "bn", "bn"},
                    { "bg", "bg"},
                    { "ca", "ca"},
                    { "zh-tw", "zh-TW"},
                    { "zh-cn", "zh-CN"},
                    { "hr", "hr"},
                    { "cs", "cs"},
                    { "da", "da"},
                    { "nl", "nl"},
                    { "en", "en"},
                    { "eo", "eo"},
                    { "et", "et"},
                    { "tl", "tl"},
                    { "fi", "fi"},
                    { "fr", "fr"},
                    { "gl", "gl"},
                    { "de", "de"},
                    { "ka", "ka"},
                    { "el", "el"},
                    { "ht", "ht"},
                    { "iw", "iw"},
                    { "hi", "hi"},
                    { "hu", "hu"},
                    { "is", "is"},
                    { "id", "id"},
                    { "ga", "ga"},
                    { "it", "it"},
                    { "ja", "ja"},
                    { "ko", "ko"},
                    { "lo", "lo"},
                    { "la", "la"},
                    { "lv", "lv"},
                    { "lt", "lt"},
                    { "mk", "mk"},
                    { "ms", "ms"},
                    { "mt", "mt"},
                    { "no", "no"},
                    { "fa", "fa"},
                    { "pl", "pl"},
                    { "pt", "pt"},
                    { "ro", "ro"},
                    { "ru", "ru"},
                    { "sr", "sr"},
                    { "sk", "sk"},
                    { "sl", "sl"},
                    { "es", "es"},
                    { "sw", "sw"},
                    { "sv", "sv"},
                    { "ta", "ta"},
                    { "te", "te"},
                    { "th", "th"},
                    { "tr", "tr"},
                    { "uk", "uk"},
                    { "ur", "ur"},
                    { "vi", "vi"},
                    { "cy", "cy"},
                    { "yi", "yi"},
                };
        }

        public async Task<string> Translate(string sourceText, string sourceLanguage, string targetLanguage)
        {
            string text;

            if(!_languageDictionary.ContainsKey(sourceLanguage) || 
               !_languageDictionary.ContainsKey(targetLanguage))
                throw new ArgumentException();
            

            var url = string.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}",
                                        ConvertToLanguageCode(sourceLanguage),
                                        ConvertToLanguageCode(targetLanguage),
                                       WebUtility.UrlEncode(sourceText));
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
                text = await http.GetStringAsync(url).ConfigureAwait(false);
            }

            return (string.Concat(JArray.Parse(text)[0].Select(x => x[0])));
        }

        private string ConvertToLanguageCode(string language)
        {
            string mode;
            _languageDictionary.TryGetValue(language, out mode);
            return mode;
        }
    }
}
