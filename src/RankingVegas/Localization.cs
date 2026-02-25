using System;
using System.Drawing;
using System.Globalization;

namespace RankingVegas
{
    public enum StatusKind
    {
        Active,
        Idle,
        Rendering,
        Error
    }

    public enum SupportedLanguage
    {
        English,
        Chinese,
        Japanese
    }

    internal static class Localization
    {
        private static SupportedLanguage? _language;

        public static SupportedLanguage Language
        {
            get
            {
                if (!_language.HasValue)
                {
                    _language = DetectLanguage();
                }
                return _language.Value;
            }
            set
            {
                _language = value;
            }
        }

        private static SupportedLanguage DetectLanguage()
        {
            string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (string.Equals(language, "zh", StringComparison.InvariantCultureIgnoreCase))
            {
                return SupportedLanguage.Chinese;
            }
            if (string.Equals(language, "ja", StringComparison.InvariantCultureIgnoreCase))
            {
                return SupportedLanguage.Japanese;
            }
            return SupportedLanguage.English;
        }

        public static bool IsChinese
        {
            get { return Language == SupportedLanguage.Chinese; }
        }

        public static bool IsJapanese
        {
            get { return Language == SupportedLanguage.Japanese; }
        }


        public static string Text(string zh, string en, string ja)
        {
            switch (Language)
            {
                case SupportedLanguage.Chinese:
                    return zh;
                case SupportedLanguage.Japanese:
                    return ja ?? en;
                default:
                    return en;
            }
        }

        public static string Format(string zh, string en, params object[] args)
        {
            return Format(zh, en, null, args);
        }

        public static string Format(string zh, string en, string ja, params object[] args)
        {
            return string.Format(Text(zh, en, ja), args);
        }

        public static string GetLanguageDisplayName(SupportedLanguage lang)
        {
            switch (lang)
            {
                case SupportedLanguage.Chinese:
                    return "简体中文";
                case SupportedLanguage.Japanese:
                    return "日本語";
                default:
                    return "English";
            }
        }

        /// <summary>
        /// Returns the primary UI font family name for the current language.
        /// </summary>
        public static string FontFamily
        {
            get
            {
                switch (Language)
                {
                    case SupportedLanguage.Chinese:
                        return "Microsoft Yahei UI";
                    case SupportedLanguage.Japanese:
                        return "Yu Gothic UI";
                    default:
                        return "Segoe UI";
                }
            }
        }

        /// <summary>
        /// Font family used for emoji rendering.
        /// </summary>
        public const string EmojiFontFamily = "Segoe UI Emoji";

        /// <summary>
        /// Returns the CSS font-family string for the WebView, including emoji fallback.
        /// </summary>
        public static string CssFontFamily
        {
            get
            {
                switch (Language)
                {
                    case SupportedLanguage.Chinese:
                        return "'Microsoft YaHei UI', 'Segoe UI Emoji', sans-serif";
                    case SupportedLanguage.Japanese:
                        return "'Yu Gothic UI', 'Meiryo UI', 'Segoe UI Emoji', sans-serif";
                    default:
                        return "'Segoe UI', 'Segoe UI Emoji', sans-serif";
                }
            }
        }

        /// <summary>
        /// Returns the localized timer label text depending on whether rendering is active.
        /// </summary>
        public static string GetTimerLabel(bool isRendering)
        {
            if (isRendering)
            {
                return Text("本次渲染时长", "Render Duration", "レンダリング時間");
            }
            return Text("累计时长", "Total Duration", "累積時間");
        }
    }
}
