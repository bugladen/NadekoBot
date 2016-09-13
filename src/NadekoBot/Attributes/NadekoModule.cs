using Discord.Commands;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Attributes
{
    [System.AttributeUsage(AttributeTargets.Class)]
    sealed class NadekoModuleAttribute : ModuleAttribute
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

        public NadekoModuleAttribute(string moduleName, string defaultPrefix) : base(GetModulePrefix(moduleName) ?? defaultPrefix)
        {
            AppendSpace = false;
        }

        private static string GetModulePrefix(string moduleName)
        {
            string prefix;
            if (ModulePrefixes.TryGetValue(moduleName, out prefix))
            {
                Console.WriteLine("Cache hit");
                return prefix;
            }

            Console.WriteLine("Cache not hit for " + moduleName);
            return null;
        }
    }
}

