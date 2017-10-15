using NadekoBot.Modules.Xp.Services;
using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Xp.Extensions
{
    public static class Extensions
    {
        public static (int Level, int LevelXp, int LevelRequiredXp) GetLevelData(this UserXpStats stats)
        {
            var baseXp = XpService.XP_REQUIRED_LVL_1;
            
            var required = baseXp;
            var totalXp = 0;
            var lvl = 1;
            while (true)
            {
                required = (int)(baseXp + baseXp / 4.0 * (lvl - 1));

                if (required + totalXp > stats.Xp)
                    break;

                totalXp += required;
                lvl++;
            }

            return (lvl - 1, stats.Xp - totalXp, required);
        }
    }
}
