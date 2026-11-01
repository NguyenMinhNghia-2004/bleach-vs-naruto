using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 角色/地图包 meta 展示字段的多语言解析。
    /// 标准形态：language.{locale}.name / says（locale 如 zh-CN）。回退：当前 LANGUAGE → zh-CN → 首个可用 locale 块。
    /// </summary>
    public class PackLangUtil
    {
        /// <summary>从 meta.language 取出当前 locale 内容块（兼容旧形态 { name, says }）。</summary>
        public static object pickLocalePack(object languageRoot)
        {
            if (languageRoot == null)
            {
                return null;
            }
            // 旧形态：language 下直接 name/says
            if (Get(languageRoot, "name") != null || Get(languageRoot, "says") != null)
            {
                return languageRoot;
            }

            string lang = LANGUAGE;
            if (!string.IsNullOrEmpty(lang) && Get(languageRoot, lang) != null)
            {
                return Get(languageRoot, lang);
            }
            if (Get(languageRoot, LanguageType.CHINESE_SIMPLIFIED) != null)
            {
                return Get(languageRoot, LanguageType.CHINESE_SIMPLIFIED);
            }

            foreach (var k in Keys(languageRoot))
            {
                var block = Get(languageRoot, Str(k));
                if (block != null && Bool(block))
                {
                    return block;
                }
            }

            return null;
        }

        /// <summary>解析展示用字符串（如 name）。</summary>
        /// <param name="raw">字符串或 locale→字符串映射；可为 null。</param>
        /// <param name="fallback">皆无时的回退。</param>
        public static string resolveString(object raw, string fallback = null)
        {
            if (raw == null || raw is Undefined)
            {
                return fallback;
            }
            if (raw is string s)
            {
                return !string.IsNullOrEmpty(s) ? s : fallback;
            }
            if (raw is List<object>)
            {
                return fallback;
            }

            string text = pickLocaleString(raw);

            return !string.IsNullOrEmpty(text) ? text : fallback;
        }

        /// <summary>解析台词数组（如 says）。</summary>
        public static List<object> resolveStringArray(object raw)
        {
            if (raw == null || raw is Undefined)
            {
                return null;
            }
            if (raw is List<object> l)
            {
                return l;
            }

            return pickLocaleArray(raw);
        }

        /// <summary>从 locale map 取字符串</summary>
        private static string pickLocaleString(object map)
        {
            if (map == null)
            {
                return null;
            }

            var v = valueForLocale(map);
            if (v is string vs && vs.Length > 0)
            {
                return vs;
            }

            foreach (var k in Keys(map))
            {
                v = Get(map, Str(k));
                if (v is string s2 && s2.Length > 0)
                {
                    return s2;
                }
            }

            return null;
        }

        /// <summary>从 locale map 取字符串数组</summary>
        private static List<object> pickLocaleArray(object map)
        {
            if (map == null)
            {
                return null;
            }

            var v = valueForLocale(map);
            if (v is List<object> l)
            {
                return l;
            }

            foreach (var k in Keys(map))
            {
                v = Get(map, Str(k));
                if (v is List<object> l2)
                {
                    return l2;
                }
            }

            return null;
        }

        /// <summary>当前语言或 zh-CN 对应值（未校验类型）</summary>
        private static object valueForLocale(object map)
        {
            string lang = LANGUAGE;
            if (!string.IsNullOrEmpty(lang) && Get(map, lang) != null)
            {
                return Get(map, lang);
            }

            return Get(map, LanguageType.CHINESE_SIMPLIFIED);
        }
    }
}
