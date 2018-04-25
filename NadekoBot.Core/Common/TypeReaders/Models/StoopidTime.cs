using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NadekoBot.Core.Common.TypeReaders.Models
{
    public class StoopidTime
    {
        public string Input { get; set; }
        public TimeSpan Time { get; set; }

        private static readonly Regex _regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d{1,2})w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,4})h)?(?:(?<minutes>\d{1,5})m)?$",
                                RegexOptions.Compiled | RegexOptions.Multiline);

        private StoopidTime() { }

        public static StoopidTime FromInput(string input)
        {
            var m = _regex.Match(input);

            if (m.Length == 0)
            {
                throw new ArgumentException("Invalid string input format.");
            }

            string output = "";
            var namesAndValues = new Dictionary<string, int>();

            foreach (var groupName in _regex.GetGroupNames())
            {
                if (groupName == "0") continue;
                int.TryParse(m.Groups[groupName].Value, out var value);

                if (string.IsNullOrEmpty(m.Groups[groupName].Value))
                {
                    namesAndValues[groupName] = 0;
                    continue;
                }
                if (value < 1 ||
                    (groupName == "months" && value > 2) ||
                    (groupName == "weeks" && value > 10) ||
                    (groupName == "days" && value >= 70) ||
                    (groupName == "hours" && value > 2000) ||
                    (groupName == "minutes" && value > 12000))
                {
                    throw new ArgumentException($"Invalid {groupName} value.");
                }
                namesAndValues[groupName] = value;
                output += m.Groups[groupName].Value + " " + groupName + " ";
            }
            var ts = new TimeSpan(30 * namesAndValues["months"] +
                                                    7 * namesAndValues["weeks"] +
                                                    namesAndValues["days"],
                                                    namesAndValues["hours"],
                                                    namesAndValues["minutes"],
                                                    0);

            return new StoopidTime()
            {
                Input = input,
                Time = ts,
            };
        }
    }
}
