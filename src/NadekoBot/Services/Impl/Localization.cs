using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System;
using NadekoBot.Services.Database;

namespace NadekoBot.Services
{
    public class Localization
    {
        public ConcurrentDictionary<ulong, CultureInfo> GuildCultureInfos { get; }
        public CultureInfo DefaultCultureInfo { get; private set; } = CultureInfo.CurrentCulture;

        private Localization() { }
        public Localization(IDictionary<ulong, string> cultureInfoNames)
        {
            GuildCultureInfos = new ConcurrentDictionary<ulong, CultureInfo>(cultureInfoNames.ToDictionary(x => x.Key, x =>
              {
                  CultureInfo cultureInfo = null;
                  try
                  {
                      cultureInfo = new CultureInfo(x.Value);
                  }
                  catch
                  {
                  }
                  return cultureInfo;
              }).Where(x => x.Value != null));
        }

        public void SetGuildCulture(IGuild guild, CultureInfo ci) =>
            SetGuildCulture(guild.Id, ci);

        public void SetGuildCulture(ulong guildId, CultureInfo ci)
        {
            if (ci == DefaultCultureInfo)
            {
                RemoveGuildCulture(guildId);
                return;
            }

            using (var uow = DbHandler.UnitOfWork())
            {
                var gc = uow.GuildConfigs.For(guildId, set => set);
                gc.Locale = ci.Name;
                uow.Complete();
            }
        }

        public void RemoveGuildCulture(IGuild guild) => 
            RemoveGuildCulture(guild.Id);

        public void RemoveGuildCulture(ulong guildId) {

            CultureInfo throwaway;
            if (GuildCultureInfos.TryRemove(guildId, out throwaway))
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var gc = uow.GuildConfigs.For(guildId, set => set);
                    gc.Locale = null;
                    uow.Complete();
                }
            }
        }

        public void SetDefaultCulture(CultureInfo ci)
        {
            DefaultCultureInfo = ci;
        }

        public void ResetDefaultCulture()
        {
            DefaultCultureInfo = CultureInfo.CurrentCulture;
        }

        public CultureInfo GetCultureInfo(IGuild guild) =>
            GetCultureInfo(guild.Id);

        public CultureInfo GetCultureInfo(ulong guildId)
        {
            CultureInfo info = null;
            GuildCultureInfos.TryGetValue(guildId, out info);
            return info ?? DefaultCultureInfo;
        }

        public static string LoadCommandString(string key)
        {
            string toReturn = Resources.CommandStrings.ResourceManager.GetString(key);
            return string.IsNullOrWhiteSpace(toReturn) ? key : toReturn;
        }
    }
}
