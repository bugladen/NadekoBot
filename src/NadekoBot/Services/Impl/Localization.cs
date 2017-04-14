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
using NLog;

namespace NadekoBot.Services
{
    public class Localization
    {
        private readonly Logger _log;

        public ConcurrentDictionary<ulong, CultureInfo> GuildCultureInfos { get; }
        public CultureInfo DefaultCultureInfo { get; private set; } = CultureInfo.CurrentCulture;

        private Localization() { }
        public Localization(string defaultCulture, IDictionary<ulong, string> cultureInfoNames)
        {
            _log = LogManager.GetCurrentClassLogger();
            if (string.IsNullOrWhiteSpace(defaultCulture))
                DefaultCultureInfo = new CultureInfo("en-US");
            else
            {
                try
                {
                    DefaultCultureInfo = new CultureInfo(defaultCulture);
                }
                catch
                {
                    _log.Warn("Unable to load default bot's locale/language. Using en-US.");
                    DefaultCultureInfo = new CultureInfo("en-US");
                }
            }
            GuildCultureInfos = new ConcurrentDictionary<ulong, CultureInfo>(cultureInfoNames.ToDictionary(x => x.Key, x =>
              {
                  CultureInfo cultureInfo = null;
                  try
                  {
                      if (x.Value == null)
                          return null;
                      cultureInfo = new CultureInfo(x.Value);
                  }
                  catch { }
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

            GuildCultureInfos.AddOrUpdate(guildId, ci, (id, old) => ci);
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
            using (var uow = DbHandler.UnitOfWork())
            {
                var bc = uow.BotConfig.GetOrCreate();
                bc.Locale = ci.Name;
                uow.Complete();
            }
            DefaultCultureInfo = ci;
        }

        public void ResetDefaultCulture() =>
            SetDefaultCulture(CultureInfo.CurrentCulture);

        public CultureInfo GetCultureInfo(IGuild guild) =>
            GetCultureInfo(guild?.Id);

        public CultureInfo GetCultureInfo(ulong? guildId)
        {
            if (guildId == null)
                return DefaultCultureInfo;
            CultureInfo info = null;
            GuildCultureInfos.TryGetValue(guildId.Value, out info);
            return info ?? DefaultCultureInfo;
        }

        public static string LoadCommandString(string key)
        {
            string toReturn = Resources.CommandStrings.ResourceManager.GetString(key);
            return string.IsNullOrWhiteSpace(toReturn) ? key : toReturn;
        }
    }
}
