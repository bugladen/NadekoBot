using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using NLog;
using System.Diagnostics;
using Newtonsoft.Json;
using System;

namespace NadekoBot.Services
{
    public class NadekoStrings
    {
        public const string stringsPath = @"_strings/";

        private readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> responseStrings;
        private readonly Logger _log;
        /// <summary>
        /// Used as failsafe in case response key doesn't exist in the selected or default language.
        /// </summary>
        private readonly CultureInfo _usCultureInfo = new CultureInfo("en-US");

        public NadekoStrings()
        {
            _log = LogManager.GetCurrentClassLogger();
            var sw = Stopwatch.StartNew();
            var allLangsDict = new Dictionary<string, ImmutableDictionary<string, string>>(); // lang:(name:value)
            foreach (var file in Directory.GetFiles(stringsPath))
            {
                var langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));

                allLangsDict.Add(GetLocaleName(file).ToLowerInvariant(), langDict.ToImmutableDictionary());
            }

            responseStrings = allLangsDict.ToImmutableDictionary();
            sw.Stop();

            _log.Info("Loaded {0} languages ({1}) in {2:F2}s",
                responseStrings.Count,
                string.Join(",", responseStrings.Keys),
                sw.Elapsed.TotalSeconds);
        }

        private string GetLocaleName(string fileName)
        {
            var dotIndex = fileName.IndexOf('.') + 1;
            var secondDotINdex = fileName.LastIndexOf('.');
            return fileName.Substring(dotIndex, secondDotINdex - dotIndex);
        }

        private string GetString(string text, CultureInfo cultureInfo)
        {
            if (!responseStrings.TryGetValue(cultureInfo.Name.ToLowerInvariant(), out ImmutableDictionary<string, string> strings))
                return null;

            strings.TryGetValue(text, out string val);
            return val;
        }

        public string GetText(string key, CultureInfo cultureInfo, string lowerModuleTypeName)
        {
            var text = GetString(lowerModuleTypeName + "_" + key, cultureInfo);

            if (string.IsNullOrWhiteSpace(text))
            {
                LogManager.GetCurrentClassLogger().Warn(lowerModuleTypeName + "_" + key + " key is missing from " + cultureInfo + " response strings. PLEASE REPORT THIS.");
                text = GetString(lowerModuleTypeName + "_" + key, _usCultureInfo) ?? $"Error: dkey {lowerModuleTypeName + "_" + key} not found!";
                if (string.IsNullOrWhiteSpace(text))
                    return "I can't tell you if the command is executed, because there was an error printing out the response. Key '" +
                        lowerModuleTypeName + "_" + key + "' " + "is missing from resources. Please report this.";
            }
            return text;
        }

        public string GetText(string key, CultureInfo cultureInfo, string lowerModuleTypeName,
            params object[] replacements)
        {
            try
            {
                return string.Format(GetText(key, cultureInfo, lowerModuleTypeName), replacements);
            }
            catch (FormatException)
            {
                return "I can't tell you if the command is executed, because there was an error printing out the response. Key '" +
                       lowerModuleTypeName + "_" + key + "' " + "is not properly formatted. Please report this.";
            }
        }
    }
}
