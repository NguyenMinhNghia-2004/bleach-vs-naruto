using System;
using System.Collections.Generic;
using System.Reflection;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Embed SWF 资源池：扫描子类上的 Embed 字段，经 EmbedSwf 加载后按名取类。
    /// 子类需提供 UI Embed 字段（public 实例字段，值见 EmbedSwf 说明），并实现 getUiClass。
    /// </summary>
    public class EmbedSwfAssetUtil
    {
        private Dictionary<object, object> _swfPool;
        private Delegate _initBack;
        private bool _inited;
        private bool _initing;

        /// <summary>子类返回主 UI Embed 类（用于 getItemClass）。</summary>
        public virtual object getUiClass()
        {
            return null;
        }

        /// <summary>初始化：加载本实例上全部 Embed 变量。</summary>
        public virtual void initialize(Delegate back = null)
        {
            if (_initing)
            {
                throw new Exception(GetLang("debug.error.data.embed_swf_asset_util.init_in_progress"));
            }

            if (_inited)
            {
                if (back != null)
                {
                    Call(back);
                }
                return;
            }

            _inited = true;
            _initing = true;

            _swfPool = new Dictionary<object, object>();

            _initBack = back;

            // describeType(this).variable → public instance fields
            foreach (var j in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var cls = j.GetValue(this);
                if (cls == null || _swfPool.ContainsKey(cls))
                {
                    continue;
                }

                var swf = new EmbedSwf(cls);
                swf.ready = (Action<EmbedSwf>)swfReadyBack;
                _swfPool[cls] = swf;
            }

            if (_swfPool.Count == 0)
            {
                finish();
            }
        }

        /// <summary>创建显示对象。</summary>
        public virtual object createDisplayObject(string itemName)
        {
            var cls = getItemClass(itemName);
            if (cls != null)
            {
                return New(cls);
            }
            return null;
        }

        /// <summary>创建 BitmapData。</summary>
        public virtual BitmapData createBitmapData(string itemName, int width, int height)
        {
            var cls = getItemClass(itemName);
            if (cls == null)
            {
                return null;
            }
            return EmbedSwf.CreateBitmapData(cls, width, height);
        }

        /// <summary>取得链接类。</summary>
        public virtual object getItemClass(string itemName)
        {
            if (_swfPool == null)
            {
                throw new Exception(GetLang("debug.error.data.embed_swf_asset_util.not_initialized"));
            }

            var ui = getUiClass();
            EmbedSwf swf = null;
            if (ui != null && _swfPool.TryGetValue(ui, out var s)) swf = s as EmbedSwf;
            if (swf == null)
            {
                throw new Exception("swf is undefined!");
            }
            return swf.getClass(itemName);
        }

        private void swfReadyBack(EmbedSwf target)
        {
            foreach (var o in _swfPool.Values)
            {
                if (!(o as EmbedSwf).isReady)
                {
                    return;
                }
            }
            finish();
        }

        private void finish()
        {
            _initing = false;

            if (_initBack != null)
            {
                var b = _initBack;
                _initBack = null;
                Call(b);
            }
        }
    }
}
