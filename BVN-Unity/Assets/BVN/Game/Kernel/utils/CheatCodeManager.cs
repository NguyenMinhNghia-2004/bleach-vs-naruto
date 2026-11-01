using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 作弊码管理器：管理和检测玩家输入的作弊码序列（单例，共享单一 KEY_DOWN 监听器）。
    /// 按键之间用 '|' 分隔，按键名为 KyoKeyCode 中的常量名，如 "W|S|A|K"。
    /// register 返回注销函数（Action）。
    /// </summary>
    public class CheatCodeManager
    {
        /// <summary>单例实例</summary>
        private static CheatCodeManager _instance;

        /// <summary>全局启用/禁用标志</summary>
        private static bool _enabled = true;

        /// <summary>是否已添加键盘事件监听器</summary>
        private bool _hasListener = false;

        /// <summary>已注册的作弊码字典：id → CheatCodeEntry</summary>
        private Dictionary<object, object> _registeredCodes = new Dictionary<object, object>();

        /// <summary>获取单例实例</summary>
        public static CheatCodeManager I
        {
            get
            {
                _instance ??= new CheatCodeManager();
                return _instance;
            }
        }

        /// <summary>全局启用/禁用属性</summary>
        public virtual bool enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// 注册一个作弊码。
        /// </summary>
        /// <param name="code">作弊码字符串，格式为 "按键1|按键2|按键3..."</param>
        /// <param name="success">成功触发时调用的回调函数</param>
        /// <param name="isRunOnce">是否只触发一次</param>
        /// <returns>注销函数；注册失败返回 null</returns>
        public virtual Delegate register(string code, Delegate success, bool isRunOnce = false)
        {
            if (STAGE == null)
            {
                return null;
            }

            // 解析作弊码字符串
            var cheatCode = parseCode(code);
            if (cheatCode == null)
            {
                return null;
            }

            // 生成唯一的作弊码 ID
            string id = UUID.create();
            var entry = new CheatCodeEntry(new ASObject
            {
                ["code"] = code,
                ["success"] = success,
                ["isRunOnce"] = isRunOnce,
                ["keys"] = cheatCode,
                ["currentIndex"] = 0,
                ["hasSucceeded"] = false
            });
            _registeredCodes[id] = entry;

            if (!_hasListener)
            {
                STAGE.addEventListener<KeyboardEvent>(KeyboardEvent.KEY_DOWN, onKeyDown);
                _hasListener = true;
            }

            // 返回注销函数
            return (Action)(() => unregister(id));
        }

        /// <summary>注销指定的作弊码；无作弊码时移除全局键盘监听。</summary>
        public virtual void unregister(string id)
        {
            if (id != null && _registeredCodes.ContainsKey(id) && _registeredCodes[id] != null)
            {
                _registeredCodes.Remove(id);

                if (isEmpty())
                {
                    STAGE.removeEventListener<KeyboardEvent>(KeyboardEvent.KEY_DOWN, onKeyDown);
                    _hasListener = false;
                }
            }
        }

        /// <summary>注销所有已注册的作弊码</summary>
        public virtual void unregisterAll()
        {
            foreach (var id in new List<object>(_registeredCodes.Keys))
            {
                unregister(Str(id));
            }
        }

        /// <summary>获取所有已注册的作弊码列表</summary>
        public virtual List<object> getRegisteredCodes()
        {
            var result = new List<object>();
            foreach (var kv in _registeredCodes)
            {
                var entry = kv.Value as CheatCodeEntry;
                result.Add(entry.code);
            }

            return result;
        }

        /// <summary>全局共享的键盘事件监听器。</summary>
        private void onKeyDown(KeyboardEvent e)
        {
            if (!_enabled)
            {
                return;
            }

            // 遍历所有已注册的作弊码（快照：回调中可能注销）
            foreach (var kv in new List<KeyValuePair<object, object>>(_registeredCodes))
            {
                var entry = kv.Value as CheatCodeEntry;
                if (entry == null) continue;

                // 检查是否已触发过
                if (entry.isRunOnce && entry.hasSucceeded)
                {
                    continue;
                }

                // 检查按键是否匹配当前期待的按键
                if (e.keyCode == entry.keys[entry.currentIndex].code)
                {
                    entry.currentIndex++;

                    // 检查是否匹配完成
                    if (entry.currentIndex >= entry.keys.Count)
                    {
                        if (entry.isRunOnce)
                        {
                            entry.hasSucceeded = true;
                        }

                        // 重置进度
                        entry.currentIndex = 0;

                        // 触发成功回调
                        if (entry.success != null)
                        {
                            Call(entry.success);
                        }
                    }
                }
                else
                {
                    // 不匹配，重置进度
                    entry.currentIndex = 0;
                }
            }
        }

        private static string SEPARATOR = "|";

        /// <summary>解析作弊码字符串为按键序列；任何按键名无效则返回 null。</summary>
        private List<KyoKeyVO> parseCode(string code)
        {
            if (code == null)
            {
                return null;
            }

            // 分隔后的按键名数组
            List<object> parts = code.IndexOf(SEPARATOR, StringComparison.Ordinal) == -1
                ? new List<object> { code }
                : split(code, SEPARATOR);

            var result = new List<KyoKeyVO>();
            foreach (var p in parts)
            {
                var name = Str(p);
                // KyoKeyCode[name]: static member lookup by name
                var key = string.IsNullOrEmpty(name) ? null : Get(typeof(KyoKeyCode), name) as KyoKeyVO;
                if (key == null)
                {
                    return null;
                }

                result.Add(key);
            }

            return result;
        }

        /// <summary>检查是否还有已注册的作弊码</summary>
        private bool isEmpty()
        {
            return _registeredCodes.Count == 0;
        }
    }

    /// <summary>作弊码条目类（CheatCodeManager.as 文件内私有类）。</summary>
    public class CheatCodeEntry
    {
        /// <summary>原始作弊码字符串</summary>
        public string code;

        /// <summary>成功触发作弊码时调用的回调函数</summary>
        public Delegate success;

        /// <summary>是否只允许触发一次</summary>
        public bool isRunOnce;

        /// <summary>解析后的按键序列</summary>
        public List<KyoKeyVO> keys;

        /// <summary>当前按键匹配进度（从 0 开始）</summary>
        public int currentIndex;

        /// <summary>是否已成功触发过</summary>
        public bool hasSucceeded;

        public CheatCodeEntry(object @params)
        {
            this.code = Str(Get(@params, "code"));
            this.success = Get(@params, "success") as Delegate;
            this.isRunOnce = Bool(Get(@params, "isRunOnce"));
            this.keys = Get(@params, "keys") as List<KyoKeyVO>;
            this.currentIndex = Int(Get(@params, "currentIndex"));
            this.hasSucceeded = Bool(Get(@params, "hasSucceeded"));
        }
    }
}
