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
    public abstract class NadekoModule : ModuleBase
    {
        protected readonly Logger _log;
        protected CultureInfo _cultureInfo { get; private set; }
        public readonly string _prefix;
        public readonly string ModuleTypeName;
        public readonly string LowerModuleTypeName;

        public NadekoModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            ModuleTypeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            LowerModuleTypeName = ModuleTypeName.ToLowerInvariant();

            if (!NadekoBot.ModulePrefixes.TryGetValue(ModuleTypeName, out _prefix))
                _prefix = "?err?";
            _log = LogManager.GetCurrentClassLogger();
        }

        protected override void BeforeExecute()
        {
            _cultureInfo = (Context.Guild == null
                ? NadekoBot.Localization.DefaultCultureInfo
                : NadekoBot.Localization.GetCultureInfo(Context.Guild));

            _log.Warn("Culture info is {0}", _cultureInfo);
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
        private readonly CultureInfo usCultureInfo = new CultureInfo("en-US");
        protected string GetText(string key)
        {
            var text = NadekoBot.ResponsesResourceManager.GetString(LowerModuleTypeName + "_" + key, _cultureInfo);

            if (string.IsNullOrWhiteSpace(text))
            {
                _log.Warn(LowerModuleTypeName + "_" + key + " key is missing from " + _cultureInfo + " response strings. PLEASE REPORT THIS.");
                return NadekoBot.ResponsesResourceManager.GetString(LowerModuleTypeName + "_" + key, usCultureInfo);
            }
            return text;
        }

        protected string GetText(string key, params object[] replacements)
        {
            return string.Format(GetText(key), replacements);
        }

        public Task<IUserMessage> ErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey);
            return Context.Channel.SendErrorAsync(string.Format(text, replacements));
        }

        public Task<IUserMessage> ReplyErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey);
            return Context.Channel.SendErrorAsync(Context.User.Mention + " " + string.Format(text, replacements));
        }

        public Task<IUserMessage> ConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey);
            return Context.Channel.SendConfirmAsync(string.Format(text, replacements));
        }

        public Task<IUserMessage> ReplyConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey);
            return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + string.Format(text, replacements));
        }
    }

    public abstract class NadekoSubmodule : NadekoModule
    {
        public NadekoSubmodule() : base(false)
        {
        }
    }
}