using Discord.Commands;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Attributes
{
    [System.AttributeUsage(AttributeTargets.Class)]
    sealed class NadekoModuleAttribute : GroupAttribute
    {
        //modulename / prefix
        private static Dictionary<string, string> modulePrefixes = null;
        public static Dictionary<string, string> ModulePrefixes {
            get {
                if (modulePrefixes != null)
                    return modulePrefixes;

                using (var uow = DbHandler.UnitOfWork())
                {
                    return (modulePrefixes = uow.BotConfig
                                                .GetOrCreate()
                                                .ModulePrefixes
                                                .ToDictionary(p => p.ModuleName, p => p.Prefix));
                }
            }
        }

        public NadekoModuleAttribute(string moduleName, string defaultPrefix) : base(GetModulePrefix(moduleName, defaultPrefix), moduleName)
        {
            //AppendSpace = false;
        }

        private static string GetModulePrefix(string moduleName, string defaultPrefix)
        {
            string prefix = null;
            if (!ModulePrefixes.TryGetValue(moduleName, out prefix))
            {
                NadekoBot.ModulePrefixes.TryAdd(moduleName, defaultPrefix);
                NLog.LogManager.GetCurrentClassLogger().Warn("Prefix not found for {0}. Will use default one: {1}", moduleName, defaultPrefix);
            }
            

            return prefix ?? defaultPrefix;
        }
    }
}

