using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Language codes (BCP 47 style) and support checks.</summary>
    public class LanguageType
    {
        /// <summary>Simplified Chinese</summary>
        public const string CHINESE_SIMPLIFIED = "zh-CN";
        /// <summary>Traditional Chinese</summary>
        public const string CHINESE_TRADITIONAL = "zh-TW";
        /// <summary>English</summary>
        public const string ENGLISH = "en";
        /// <summary>Japanese</summary>
        public const string JAPANESE = "ja";
        /// <summary>Korean</summary>
        public const string KOREAN = "ko";
        /// <summary>Vietnamese</summary>
        public const string VIETNAMESE = "vi";

        /// <summary>All supported language codes.</summary>
        private static readonly List<string> _allLanguages = new List<string>
        {
            CHINESE_SIMPLIFIED, CHINESE_TRADITIONAL,
            ENGLISH,
            JAPANESE,
            KOREAN,
            VIETNAMESE
        };

        public static bool isSupported(string language)
        {
            return _allLanguages.IndexOf(language) != -1;
        }

        public static bool isSimplifiedChinese(string language)
        {
            return language == CHINESE_SIMPLIFIED;
        }
    }
}
