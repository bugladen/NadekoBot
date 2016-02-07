using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes._DataModels {
    class Announcement : IDataModel {
        public long ServerId { get; set; } = 0;
        public bool Greet { get; set; } = false;
        public bool GreetPM { get; set; } = false;
        public long GreetChannelId { get; set; } = 0;
        public string GreetText { get; set; } = "Welcome %user%!";
        public bool Bye { get; set; } = false;
        public bool ByePM { get; set; } = false;
        public long ByeChannelId { get; set; } = 0;
        public string ByeText { get; set; } = "%user% has left the server.";
        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}
