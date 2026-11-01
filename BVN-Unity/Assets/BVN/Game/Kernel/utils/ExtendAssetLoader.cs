using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>IAssetLoader forwarding to an external (dynamic) loader object.</summary>
    public class ExtendAssetLoader : IAssetLoader
    {
        public ExtendAssetLoader(object extend)
        {
            if (extend == null)
            {
                ThrowError(typeof(Exception), "extend is null !");
                return;
            }
            _extend = extend;
        }

        private object _extend;

        /// <summary>AS3 `_extend.func(args)`: typed IAssetLoader when possible, else dynamic member call.</summary>
        private object callExtend(string func, params object[] args)
        {
            var fn = Get(_extend, func);
            return Call(fn, args);
        }

        public virtual void loadXML(string url, Delegate back, Delegate fail = null)
        {
            if (_extend is IAssetLoader l) { l.loadXML(url, back, fail); return; }
            callExtend("loadXML", url, back, fail);
        }

        public virtual void loadJSON(string url, Delegate back, Delegate fail = null)
        {
            if (_extend is IAssetLoader l) { l.loadJSON(url, back, fail); return; }
            callExtend("loadJSON", url, back, fail);
        }

        public virtual void loadSwf(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            if (_extend is IAssetLoader l) { l.loadSwf(url, back, fail, progress); return; }
            callExtend("loadSwf", url, back, fail, progress);
        }

        public virtual void loadSound(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            if (_extend is IAssetLoader l) { l.loadSound(url, back, fail, progress); return; }
            callExtend("loadSound", url, back, fail, progress);
        }

        public virtual void loadBitmap(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            if (_extend is IAssetLoader l) { l.loadBitmap(url, back, fail, progress); return; }
            callExtend("loadBitmap", url, back, fail, progress);
        }

        public virtual void dispose(string url)
        {
            if (_extend is IAssetLoader l) { l.dispose(url); return; }
            callExtend("dispose", url);
        }

        public virtual bool needPreLoad()
        {
            if (_extend is IAssetLoader l) return l.needPreLoad();
            return Bool(callExtend("needPreLoad"));
        }

        public virtual void loadPreLoad(Delegate back, Delegate fail = null, Delegate progress = null)
        {
            if (_extend is IAssetLoader l) { l.loadPreLoad(back, fail, progress); return; }
            callExtend("loadPreLoad", back, fail, progress);
        }
    }
}
