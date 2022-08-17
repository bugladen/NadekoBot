using System;
using System.Runtime.CompilerServices;
using Discord.Commands;
using NadekoBot.Core.Services.Impl;
using Newtonsoft.Json;

namespace NadekoBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UsageAttribute : RemarksAttribute
    {
        public UsageAttribute([CallerMemberName] string memberName="") : base(UsageAttribute.GetUsage(memberName))
        {

        }

        public static string GetUsage(string memberName)
        {
            var usage = Localization.LoadCommand(memberName.ToLowerInvariant()).Usage;
            return JsonConvert.SerializeObject(usage);
        }
    }
}
