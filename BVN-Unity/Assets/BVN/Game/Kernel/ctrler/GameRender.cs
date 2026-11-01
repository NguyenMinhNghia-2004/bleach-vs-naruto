using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏渲染（Stage ENTER_FRAME 驱动）</summary>
    public class GameRender
    {
        // 是否渲染
        public static bool isRender = true;

        // 先于主渲染之前渲染
        private static List<Delegate> _beforeFuncs;
        // 主渲染 (owner → funcs)；插入顺序即 AS3 Dictionary 的遍历顺序
        private static Dictionary<object, List<Delegate>> _funcs = new Dictionary<object, List<Delegate>>();
        // 后于主渲染之后渲染
        private static List<Delegate> _afterFuncs;

        private static readonly List<List<Delegate>> _funcsSnapshot = new List<List<Delegate>>();

        /// <summary>初始化</summary>
        public static void initialize(Stage stage)
        {
            stage.addEventListener(Event.ENTER_FRAME, render);
        }

        public static void addBefore(Delegate func)
        {
            if (func == null) return;

            _beforeFuncs ??= new List<Delegate>();

            int index = _beforeFuncs.IndexOf(func);
            if (index == -1) _beforeFuncs.Add(func);
        }

        public static void removeBefore(Delegate func)
        {
            if (func == null || _beforeFuncs == null) return;

            int index = _beforeFuncs.IndexOf(func);
            if (index != -1) _beforeFuncs.RemoveAt(index);
        }

        public static void add(Delegate func, object owner = null)
        {
            if (owner == null || !Bool(owner)) owner = "anyone";

            if (_funcs.TryGetValue(owner, out var list) && list.IndexOf(func) != -1) return;

            if (list == null)
            {
                list = new List<Delegate>();
                _funcs[owner] = list;
            }
            list.Add(func);
        }

        public static void remove(Delegate func, object owner = null)
        {
            if (owner == null || !Bool(owner)) owner = "anyone";

            if (!_funcs.TryGetValue(owner, out var list) || list == null) return;

            int index = list.IndexOf(func);
            if (index != -1) list.RemoveAt(index);
        }

        public static void addAfter(Delegate func)
        {
            if (func == null) return;

            _afterFuncs ??= new List<Delegate>();

            int index = _afterFuncs.IndexOf(func);
            if (index == -1) _afterFuncs.Add(func);
        }

        public static void removeAfter(Delegate func)
        {
            if (func == null || _afterFuncs == null) return;

            int index = _afterFuncs.IndexOf(func);
            if (index != -1) _afterFuncs.RemoveAt(index);
        }

        private static void runList(List<Delegate> funcs)
        {
            int length = funcs.Count;
            for (int i = 0; i < length; i++)
            {
                if (i > funcs.Count - 1) break;

                var func = funcs[i];
                if (func != null) Call(func);
            }
        }

        private static void render(Event e)
        {
            if (!isRender) return;

            if (_beforeFuncs != null) runList(_beforeFuncs);

            // snapshot: owners may be added while iterating (AS3 for-each tolerates this)
            _funcsSnapshot.Clear();
            _funcsSnapshot.AddRange(_funcs.Values);
            for (int k = 0; k < _funcsSnapshot.Count; k++) runList(_funcsSnapshot[k]);
            _funcsSnapshot.Clear();

            if (_afterFuncs != null) runList(_afterFuncs);
        }
    }
}
