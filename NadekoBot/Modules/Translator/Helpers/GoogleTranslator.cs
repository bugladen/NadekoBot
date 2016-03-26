// Copyright (c) 2015 Ravi Bhavnani
// License: Code Project Open License
// http://www.codeproject.com/info/cpol10.aspx

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        public static IEnumerable<string> Languages
        {
            get
            {
                GoogleTranslator.EnsureInitialized();
                return GoogleTranslator._languageModeMap.Keys.OrderBy(p => p);
            }
        }

        /// <summary>
        /// Gets the time taken to perform the translation.
        /// </summary>
        public TimeSpan TranslationTime
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the url used to speak the translation.
        /// </summary>
        /// <value>The url used to speak the translation.</value>
        public string TranslationSpeechUrl
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the error.
        /// </summary>
        public Exception Error
        {
            get;
            private set;
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
        public string Translate
            (string sourceText,
             string sourceLanguage,
             string targetLanguage)
        {
            // Initialize
            this.Error = null;
            this.TranslationSpeechUrl = null;
            this.TranslationTime = TimeSpan.Zero;
            DateTime tmStart = DateTime.Now;
            string translation = string.Empty;

            try
            {
                // Download translation
                string url = string.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}",
                                            GoogleTranslator.LanguageEnumToIdentifier(sourceLanguage),
                                            GoogleTranslator.LanguageEnumToIdentifier(targetLanguage),
                                            HttpUtility.UrlEncode(sourceText));
                string outputFile = Path.GetTempFileName();
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
                    wc.DownloadFile(url, outputFile);
                }

                // Get translated text
                if (File.Exists(outputFile))
                {

                    // Get phrase collection
                    string text = File.ReadAllText(outputFile);
                    int index = text.IndexOf(string.Format(",,\"{0}\"", GoogleTranslator.LanguageEnumToIdentifier(sourceLanguage)));
                    if (index == -1)
                    {
                        // Translation of single word
                        int startQuote = text.IndexOf('\"');
                        if (startQuote != -1)
                        {
                            int endQuote = text.IndexOf('\"', startQuote + 1);
                            if (endQuote != -1)
                            {
                                translation = text.Substring(startQuote + 1, endQuote - startQuote - 1);
                            }
                        }
                    }
                    else {
                        // Translation of phrase
                        text = text.Substring(0, index);
                        text = text.Replace("],[", ",");
                        text = text.Replace("]", string.Empty);
                        text = text.Replace("[", string.Empty);
                        text = text.Replace("\",\"", "\"");

                        // Get translated phrases
                        string[] phrases = text.Split(new[] { '\"' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; (i < phrases.Count()); i += 2)
                        {
                            string translatedPhrase = phrases[i];
                            if (translatedPhrase.StartsWith(",,"))
                            {
                                i--;
                                continue;
                            }
                            translation += translatedPhrase + "  ";
                        }
                    }

                    // Fix up translation
                    translation = translation.Trim();
                    translation = translation.Replace(" ?", "?");
                    translation = translation.Replace(" !", "!");
                    translation = translation.Replace(" ,", ",");
                    translation = translation.Replace(" .", ".");
                    translation = translation.Replace(" ;", ";");

                    // And translation speech URL
                    this.TranslationSpeechUrl = string.Format("https://translate.googleapis.com/translate_tts?ie=UTF-8&q={0}&tl={1}&total=1&idx=0&textlen={2}&client=gtx",
                                                               HttpUtility.UrlEncode(translation), GoogleTranslator.LanguageEnumToIdentifier(targetLanguage), translation.Length);
                }
            }
            catch (Exception ex)
            {
                this.Error = ex;
            }

            // Return result
            this.TranslationTime = DateTime.Now - tmStart;
            return translation;
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
                GoogleTranslator._languageModeMap = new Dictionary<string, string>();
                GoogleTranslator._languageModeMap.Add("Afrikaans", "af");
                GoogleTranslator._languageModeMap.Add("Albanian", "sq");
                GoogleTranslator._languageModeMap.Add("Arabic", "ar");
                GoogleTranslator._languageModeMap.Add("Armenian", "hy");
                GoogleTranslator._languageModeMap.Add("Azerbaijani", "az");
                GoogleTranslator._languageModeMap.Add("Basque", "eu");
                GoogleTranslator._languageModeMap.Add("Belarusian", "be");
                GoogleTranslator._languageModeMap.Add("Bengali", "bn");
                GoogleTranslator._languageModeMap.Add("Bulgarian", "bg");
                GoogleTranslator._languageModeMap.Add("Catalan", "ca");
                GoogleTranslator._languageModeMap.Add("Chinese", "zh-CN");
                GoogleTranslator._languageModeMap.Add("Croatian", "hr");
                GoogleTranslator._languageModeMap.Add("Czech", "cs");
                GoogleTranslator._languageModeMap.Add("Danish", "da");
                GoogleTranslator._languageModeMap.Add("Dutch", "nl");
                GoogleTranslator._languageModeMap.Add("English", "en");
                GoogleTranslator._languageModeMap.Add("Esperanto", "eo");
                GoogleTranslator._languageModeMap.Add("Estonian", "et");
                GoogleTranslator._languageModeMap.Add("Filipino", "tl");
                GoogleTranslator._languageModeMap.Add("Finnish", "fi");
                GoogleTranslator._languageModeMap.Add("French", "fr");
                GoogleTranslator._languageModeMap.Add("Galician", "gl");
                GoogleTranslator._languageModeMap.Add("German", "de");
                GoogleTranslator._languageModeMap.Add("Georgian", "ka");
                GoogleTranslator._languageModeMap.Add("Greek", "el");
                GoogleTranslator._languageModeMap.Add("Haitian Creole", "ht");
                GoogleTranslator._languageModeMap.Add("Hebrew", "iw");
                GoogleTranslator._languageModeMap.Add("Hindi", "hi");
                GoogleTranslator._languageModeMap.Add("Hungarian", "hu");
                GoogleTranslator._languageModeMap.Add("Icelandic", "is");
                GoogleTranslator._languageModeMap.Add("Indonesian", "id");
                GoogleTranslator._languageModeMap.Add("Irish", "ga");
                GoogleTranslator._languageModeMap.Add("Italian", "it");
                GoogleTranslator._languageModeMap.Add("Japanese", "ja");
                GoogleTranslator._languageModeMap.Add("Korean", "ko");
                GoogleTranslator._languageModeMap.Add("Lao", "lo");
                GoogleTranslator._languageModeMap.Add("Latin", "la");
                GoogleTranslator._languageModeMap.Add("Latvian", "lv");
                GoogleTranslator._languageModeMap.Add("Lithuanian", "lt");
                GoogleTranslator._languageModeMap.Add("Macedonian", "mk");
                GoogleTranslator._languageModeMap.Add("Malay", "ms");
                GoogleTranslator._languageModeMap.Add("Maltese", "mt");
                GoogleTranslator._languageModeMap.Add("Norwegian", "no");
                GoogleTranslator._languageModeMap.Add("Persian", "fa");
                GoogleTranslator._languageModeMap.Add("Polish", "pl");
                GoogleTranslator._languageModeMap.Add("Portuguese", "pt");
                GoogleTranslator._languageModeMap.Add("Romanian", "ro");
                GoogleTranslator._languageModeMap.Add("Russian", "ru");
                GoogleTranslator._languageModeMap.Add("Serbian", "sr");
                GoogleTranslator._languageModeMap.Add("Slovak", "sk");
                GoogleTranslator._languageModeMap.Add("Slovenian", "sl");
                GoogleTranslator._languageModeMap.Add("Spanish", "es");
                GoogleTranslator._languageModeMap.Add("Swahili", "sw");
                GoogleTranslator._languageModeMap.Add("Swedish", "sv");
                GoogleTranslator._languageModeMap.Add("Tamil", "ta");
                GoogleTranslator._languageModeMap.Add("Telugu", "te");
                GoogleTranslator._languageModeMap.Add("Thai", "th");
                GoogleTranslator._languageModeMap.Add("Turkish", "tr");
                GoogleTranslator._languageModeMap.Add("Ukrainian", "uk");
                GoogleTranslator._languageModeMap.Add("Urdu", "ur");
                GoogleTranslator._languageModeMap.Add("Vietnamese", "vi");
                GoogleTranslator._languageModeMap.Add("Welsh", "cy");
                GoogleTranslator._languageModeMap.Add("Yiddish", "yi");

                GoogleTranslator._languageModeMap.Add("af", "af");
                GoogleTranslator._languageModeMap.Add("sq", "sq");
                GoogleTranslator._languageModeMap.Add("ar", "ar");
                GoogleTranslator._languageModeMap.Add("hy", "hy");
                GoogleTranslator._languageModeMap.Add("az", "az");
                GoogleTranslator._languageModeMap.Add("eu", "eu");
                GoogleTranslator._languageModeMap.Add("be", "be");
                GoogleTranslator._languageModeMap.Add("bn", "bn");
                GoogleTranslator._languageModeMap.Add("bg", "bg");
                GoogleTranslator._languageModeMap.Add("ca", "ca");
                GoogleTranslator._languageModeMap.Add("zh-CN", "zh-CN");
                GoogleTranslator._languageModeMap.Add("hr", "hr");
                GoogleTranslator._languageModeMap.Add("cs", "cs");
                GoogleTranslator._languageModeMap.Add("da", "da");
                GoogleTranslator._languageModeMap.Add("nl", "nl");
                GoogleTranslator._languageModeMap.Add("en", "en");
                GoogleTranslator._languageModeMap.Add("eo", "eo");
                GoogleTranslator._languageModeMap.Add("et", "et");
                GoogleTranslator._languageModeMap.Add("tl", "tl");
                GoogleTranslator._languageModeMap.Add("fi", "fi");
                GoogleTranslator._languageModeMap.Add("fr", "fr");
                GoogleTranslator._languageModeMap.Add("gl", "gl");
                GoogleTranslator._languageModeMap.Add("de", "de");
                GoogleTranslator._languageModeMap.Add("ka", "ka");
                GoogleTranslator._languageModeMap.Add("el", "el");
                GoogleTranslator._languageModeMap.Add("ht", "ht");
                GoogleTranslator._languageModeMap.Add("iw", "iw");
                GoogleTranslator._languageModeMap.Add("hi", "hi");
                GoogleTranslator._languageModeMap.Add("hu", "hu");
                GoogleTranslator._languageModeMap.Add("is", "is");
                GoogleTranslator._languageModeMap.Add("id", "id");
                GoogleTranslator._languageModeMap.Add("ga", "ga");
                GoogleTranslator._languageModeMap.Add("it", "it");
                GoogleTranslator._languageModeMap.Add("ja", "ja");
                GoogleTranslator._languageModeMap.Add("ko", "ko");
                GoogleTranslator._languageModeMap.Add("lo", "lo");
                GoogleTranslator._languageModeMap.Add("la", "la");
                GoogleTranslator._languageModeMap.Add("lv", "lv");
                GoogleTranslator._languageModeMap.Add("lt", "lt");
                GoogleTranslator._languageModeMap.Add("mk", "mk");
                GoogleTranslator._languageModeMap.Add("ms", "ms");
                GoogleTranslator._languageModeMap.Add("mt", "mt");
                GoogleTranslator._languageModeMap.Add("no", "no");
                GoogleTranslator._languageModeMap.Add("fa", "fa");
                GoogleTranslator._languageModeMap.Add("pl", "pl");
                GoogleTranslator._languageModeMap.Add("pt", "pt");
                GoogleTranslator._languageModeMap.Add("ro", "ro");
                GoogleTranslator._languageModeMap.Add("ru", "ru");
                GoogleTranslator._languageModeMap.Add("sr", "sr");
                GoogleTranslator._languageModeMap.Add("sk", "sk");
                GoogleTranslator._languageModeMap.Add("sl", "sl");
                GoogleTranslator._languageModeMap.Add("es", "es");
                GoogleTranslator._languageModeMap.Add("sw", "sw");
                GoogleTranslator._languageModeMap.Add("sv", "sv");
                GoogleTranslator._languageModeMap.Add("ta", "ta");
                GoogleTranslator._languageModeMap.Add("te", "te");
                GoogleTranslator._languageModeMap.Add("th", "th");
                GoogleTranslator._languageModeMap.Add("tr", "tr");
                GoogleTranslator._languageModeMap.Add("uk", "uk");
                GoogleTranslator._languageModeMap.Add("ur", "ur");
                GoogleTranslator._languageModeMap.Add("vi", "vi");
                GoogleTranslator._languageModeMap.Add("cy", "cy");
                GoogleTranslator._languageModeMap.Add("yi", "yi");
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
