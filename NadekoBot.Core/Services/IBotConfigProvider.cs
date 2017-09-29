using NadekoBot.Common;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services
{
    public interface IBotConfigProvider
    {
        BotConfig BotConfig { get; }
        void Reload();
        bool Edit(BotConfigEditType type, string newValue);
    }
}
