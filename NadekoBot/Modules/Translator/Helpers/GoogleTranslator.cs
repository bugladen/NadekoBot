// Copyright (c) 2015 Ravi Bhavnani
// License: Code Project Open License
// http://www.codeproject.com/info/cpol10.aspx

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace NadekoBot.Modules.Translator.Helpers
{
    /// <summary>
    /// Translates text using Google's online language tools.
    /// </summary>
    public class GoogleTranslator
    {
        #region Properties

        /// <summary>
        /// Gets the supported languages.
        /// </summary>
        public static IEnumerable<string> Languages {
            get {
                GoogleTranslator.EnsureInitialized();
                return GoogleTranslator._languageModeMap.Keys.OrderBy(p => p);
            }
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Translates the specified source text.
        /// </summary>
        /// <param name="sourceText">The source text.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>The translation.</returns>
        public async Task<string> Translate
            (string sourceText,
             string sourceLanguage,
             string targetLanguage)
        {
            // Initialize
            DateTime tmStart = DateTime.Now;
            string text = string.Empty;


            // Download translation
            string url = string.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}",
                                        GoogleTranslator.LanguageEnumToIdentifier(sourceLanguage),
                                        GoogleTranslator.LanguageEnumToIdentifier(targetLanguage),
                                        HttpUtility.UrlEncode(sourceText));
            using (HttpClient http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
                text = await http.GetStringAsync(url).ConfigureAwait(false);
            }

            return (string.Concat(JArray.Parse(text)[0].Select(x => x[0])));
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Converts a language to its identifier.
        /// </summary>
        /// <param name="language">The language."</param>
        /// <returns>The identifier or <see cref="string.Empty"/> if none.</returns>
        private static string LanguageEnumToIdentifier
            (string language)
        {
            string mode = string.Empty;
            GoogleTranslator.EnsureInitialized();
            GoogleTranslator._languageModeMap.TryGetValue(language, out mode);
            return mode;
        }

        /// <summary>
        /// Ensures the translator has been initialized.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (GoogleTranslator._languageModeMap == null)
            {
                GoogleTranslator._languageModeMap = new Dictionary<string, string>() {
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
                    { "zh-CN", "zh-CN"},
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
        }

        #endregion

        #region Fields

        /// <summary>
        /// The language to translation mode map.
        /// </summary>
        public static Dictionary<string, string> _languageModeMap;

        #endregion
    }
}
