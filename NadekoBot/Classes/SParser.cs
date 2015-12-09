using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes
{
    /// <summary>
    /// Shit parser. I used this to convert shit-format to json format
    /// </summary>
    class SParser
    {
        public static void DoShitParse() {

            string[] lines = File.ReadAllLines("questions.txt");
            JArray qs = new JArray();
            foreach (var line in lines)
            {
                if (!line.Contains(";")) continue;
                JObject j = new JObject();
                if (line.Contains(":"))
                {
                    j["Category"] = line.Substring(0, line.LastIndexOf(":"));
                    j["Question"] = line.Substring(line.LastIndexOf(":") + 1, line.LastIndexOf(";") - line.LastIndexOf(":") - 1);

                }
                else {
                    j["Question"] = line.Substring(0, line.LastIndexOf(";"));
                }
                j["Answer"] = line.Substring(line.LastIndexOf(";") + 1, line.Length - line.LastIndexOf(";") - 1).Trim();
                qs.Add(j);
            }
            File.WriteAllText("questions2.txt", qs.ToString());
        }
    }
}
