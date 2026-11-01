using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 多语言工具集。所选语言缺键时静默回退到简体中文（zh-CN）；中文亦无定义时由 GetLangText 返回 [N/A]。
    /// </summary>
    public class MultiLangUtils
    {
        // 分隔符
        private const string SEPARATOR = ".";

        // 单例
        private static MultiLangUtils _instance;

        /// <summary>单例</summary>
        public static MultiLangUtils I
        {
            get
            {
                _instance ??= new MultiLangUtils();
                return _instance;
            }
        }

        // 当前语言对象
        private object _languageObj;
        // 简体中文兜底对象
        private object _fallbackObj;
        // 多语言缓存池（已解析最终文案）
        private Dictionary<string, string> _cacheObj = new Dictionary<string, string>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="language">所选语言</param>
        /// <param name="back">成功回调（zh-CN 就绪即可；其它语言包缺失时仍成功并走中文兜底）</param>
        /// <param name="fail">失败回调（仅 zh-CN 加载失败）</param>
        public virtual void initialize(string language, Delegate back, Delegate fail)
        {
            string fallbackUrl = langUrl(LanguageType.CHINESE_SIMPLIFIED);

            Action<object> onLangOk = data =>
            {
                _languageObj = data;
                if (back != null)
                {
                    Call(back);
                }
            };

            Action onLangFail = () =>
            {
                // 所选语言包缺失：静默仅用 zh-CN
                _languageObj = null;
                if (back != null)
                {
                    Call(back);
                }
            };

            Action<object> onFallbackOk = data =>
            {
                _fallbackObj = data;
                _cacheObj = new Dictionary<string, string>();

                if (LanguageType.isSimplifiedChinese(language))
                {
                    _languageObj = data;
                    if (back != null)
                    {
                        Call(back);
                    }
                    return;
                }

                AssetManager.I.loadJSON(langUrl(language), onLangOk, onLangFail);
            };

            Action onFallbackFail = () =>
            {
                _fallbackObj = null;
                _languageObj = null;
                _cacheObj = new Dictionary<string, string>();

                if (fail != null)
                {
                    Call(fail);
                }
            };

            AssetManager.I.loadJSON(fallbackUrl, onFallbackOk, onFallbackFail);
        }

        /// <summary>得到当前语言文本；缺键时回退 zh-CN；皆无则 null</summary>
        /// <param name="tree">文本的树形路径</param>
        public virtual string getLangText(string tree)
        {
            if (_languageObj == null && _fallbackObj == null)
            {
                return null;
            }
            if (tree == null)
            {
                return null;
            }

            if (_cacheObj.TryGetValue(tree, out var cached))
            {
                return cached;
            }

            string text = lookupText(_languageObj, tree);
            if (string.IsNullOrEmpty(text))
            {
                text = lookupText(_fallbackObj, tree);
            }
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            _cacheObj[tree] = text;
            return text;
        }

        /// <summary>语言包 URL</summary>
        private static string langUrl(string language)
        {
            return "config/language/" + language + ".json";
        }

        /// <summary>
        /// 按点分路径取字符串叶节点（AS3: ClassUtils.continuousAccess(obj, tree.split('.'))）。
        /// </summary>
        private string lookupText(object obj, string tree)
        {
            if (obj == null)
            {
                return null;
            }

            object cur = obj;
            foreach (var seg in split(tree, SEPARATOR))
            {
                if (cur == null)
                {
                    return null;
                }
                cur = Get(cur, Str(seg));
            }

            if (cur == null || cur is IDictionary<string, object> || cur is List<object>)
            {
                return null;
            }
            var text = cur as string ?? Str(cur);
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
