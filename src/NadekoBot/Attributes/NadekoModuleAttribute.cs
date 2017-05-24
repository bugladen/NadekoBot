using Discord.Commands;
using System;

namespace NadekoBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class NadekoModuleAttribute : GroupAttribute
    {
        public NadekoModuleAttribute(string moduleName) : base(moduleName)
        {
        }
    }
}

