using NadekoBot.Common;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services
{
    public interface IBotConfigProvider : INService
    {
        BotConfig BotConfig { get; }
        void Reload();
        bool Edit(BotConfigEditType type, string newValue);
    }
}
