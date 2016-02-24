using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes.Permissions {
    static class SimpleCheckers {
        public static Func<Command, User, Channel, bool> OwnerOnly() =>
            (com, user, ch) => user.Id == NadekoBot.creds.OwnerID;
    }
}
