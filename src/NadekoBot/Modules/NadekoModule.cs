using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;

namespace NadekoBot.Modules
{
    public abstract class NadekoTopLevelModule : ModuleBase
    {
        protected readonly Logger _log;
        protected CultureInfo _cultureInfo;
        public readonly string Prefix;
        public readonly string ModuleTypeName;
        public readonly string LowerModuleTypeName;

        protected NadekoTopLevelModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            ModuleTypeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            LowerModuleTypeName = ModuleTypeName.ToLowerInvariant();

            if (!NadekoBot.ModulePrefixes.TryGetValue(ModuleTypeName, out Prefix))
                Prefix = "?err?";
            _log = LogManager.GetCurrentClassLogger();
        }

        protected override void BeforeExecute()
        {
            _cultureInfo = NadekoBot.Localization.GetCultureInfo(Context.Guild?.Id);

            _log.Info("Culture info is {0}", _cultureInfo);
        }

        //public Task<IUserMessage> ReplyConfirmLocalized(string titleKey, string textKey, string url = null, string footer = null)
        //{
        //    var title = NadekoBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
        //    var text = NadekoBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendConfirmAsync(title, text, url, footer);
        //}

        //public Task<IUserMessage> ReplyConfirmLocalized(string textKey)
        //{
        //    var text = NadekoBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + textKey);
        //}

        //public Task<IUserMessage> ReplyErrorLocalized(string titleKey, string textKey, string url = null, string footer = null)
        //{
        //    var title = NadekoBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
        //    var text = NadekoBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendErrorAsync(title, text, url, footer);
        //}

        /// <summary>
        /// Used as failsafe in case response key doesn't exist in the selected or default language.
        /// </summary>
        private static readonly CultureInfo _usCultureInfo = new CultureInfo("en-US");

        public static string GetTextStatic(string key, CultureInfo cultureInfo, string lowerModuleTypeName)
        {
            var text = NadekoBot.ResponsesResourceManager.GetString(lowerModuleTypeName + "_" + key, cultureInfo);

            if (string.IsNullOrWhiteSpace(text))
            {
                LogManager.GetCurrentClassLogger().Warn(lowerModuleTypeName + "_" + key + " key is missing from " + cultureInfo + " response strings. PLEASE REPORT THIS.");
                text = NadekoBot.ResponsesResourceManager.GetString(lowerModuleTypeName + "_" + key, _usCultureInfo) ?? $"Error: dkey {lowerModuleTypeName + "_" + key} not found!";
                if (string.IsNullOrWhiteSpace(text))
                    return "I can't tell you if the command is executed, because there was an error printing out the response. Key '" +
                        lowerModuleTypeName + "_" + key + "' " + "is missing from resources. Please report this.";
            }
            return text;
        }

        public static string GetTextStatic(string key, CultureInfo cultureInfo, string lowerModuleTypeName,
            params object[] replacements)
        {
            try
            {
                return string.Format(GetTextStatic(key, cultureInfo, lowerModuleTypeName), replacements);
            }
            catch (FormatException)
            {
                return "I can't tell you if the command is executed, because there was an error printing out the response. Key '" +
                       lowerModuleTypeName + "_" + key + "' " + "is not properly formatted. Please report this.";
            }
        }

        protected string GetText(string key) =>
            GetTextStatic(key, _cultureInfo, LowerModuleTypeName);

        protected string GetText(string key, params object[] replacements) =>
            GetTextStatic(key, _cultureInfo, LowerModuleTypeName, replacements);

        public Task<IUserMessage> ErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(text);
        }

        public Task<IUserMessage> ReplyErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(Context.User.Mention + " " + text);
        }

        public Task<IUserMessage> ConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(text);
        }

        public Task<IUserMessage> ReplyConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + text);
        }
    }

    public abstract class NadekoSubmodule : NadekoTopLevelModule
    {
        protected NadekoSubmodule() : base(false)
        {
        }
    }
}