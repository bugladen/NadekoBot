using NadekoBot.DataModels;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Classes
{
    internal static class IncidentsHandler
    {
        public static Task Add(ulong serverId, ulong channelId, string text)
        {
            var def = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"INCIDENT: {text}");
            Console.ForegroundColor = def;
            var incident = new Incident
            {
                ChannelId = (long)channelId,
                ServerId = (long)serverId,
                Text = text,
                Read = false
            };

            return DbHandler.Instance.Connection.InsertAsync(incident);
        }
    }
}
