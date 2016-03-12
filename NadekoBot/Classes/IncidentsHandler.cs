using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes {
    class IncidentsHandler {
        public static void Add(ulong serverId, string text) {
            Directory.CreateDirectory("data/incidents");
            File.AppendAllText($"data/incidents/{serverId}.txt", text + "\n--------------------------");
            Console.WriteLine($"INCIDENT: {text}");
        }
    }
}
