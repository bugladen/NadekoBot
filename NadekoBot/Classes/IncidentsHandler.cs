using System;
using System.IO;

namespace NadekoBot.Classes {
    internal static class IncidentsHandler {
        public static void Add(ulong serverId, string text) {
            Directory.CreateDirectory("data/incidents");
            File.AppendAllText($"data/incidents/{serverId}.txt", text + "\n--------------------------\n");
            var def = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"INCIDENT: {text}");
            Console.ForegroundColor = def;
        }
    }
}
