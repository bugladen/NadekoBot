using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using NLog;
using System.Diagnostics;
using Newtonsoft.Json;

namespace NadekoBot.Services
{
    public class NadekoStrings
    {
        public const string stringsPath = @"_strings/";

        private readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> responseStrings;
        private readonly Logger _log;

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

        public string GetString(string text, CultureInfo cultureInfo)
        {
            if (!responseStrings.TryGetValue(cultureInfo.Name.ToLowerInvariant(), out ImmutableDictionary<string, string> strings))
                return null;

            strings.TryGetValue(text, out string val);
            return val;
        }
    }
}
