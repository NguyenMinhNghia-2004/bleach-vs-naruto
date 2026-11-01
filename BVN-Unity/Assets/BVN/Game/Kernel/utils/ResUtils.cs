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
    /// UI 资源（ISwfLib 中的 Embed SWF）池。Embed "classes" are resolved by EmbedSwf (library path / LibraryData).
    /// </summary>
    public class ResUtils
    {
        public static string SETTING = "$setting$MC_stgSetUI";
        public static string CONGRATULATIONS = "$common$MC_congratulations";
        public static string WINNER = "$loading$MC_stageWinner";
        public static string TITLE = "$title$MC_stgTitle";
        public static string GAME_OVER = "$game_over$MC_stgGameOver";
        public static string SELECT = "$select$MC_stgSelect";
        public static string BIG_MAP = "$big_map$MC_bigMap";
        public static ISwfLib swfLib;
        private static ResUtils _i;

        public static ResUtils I
        {
            get
            {
                _i ??= new ResUtils();
                return _i;
            }
        }

        private Dictionary<object, object> _swfPool;
        private Delegate _initBack;
        private Delegate _initError;
        private bool _inited;
        private bool _initing;

        public virtual void initialize(Delegate back = null, Delegate error = null)
        {
            if (_initing)
            {
                throw new Exception(GetLang("debug.error.data.res_utils.init_in_progress"));
            }

            if (swfLib == null)
            {
                throw new Exception(GetLang("debug.error.data.res_utils.swf_lib_not_init"));
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

            _swfPool ??= new Dictionary<object, object>();

            _initBack = back;
            _initError = error;

            // describeType(swfLib).accessor → public instance properties
            foreach (var j in swfLib.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!j.CanRead || j.GetIndexParameters().Length > 0)
                {
                    continue;
                }
                var cls = j.GetValue(swfLib);
                if (cls == null || _swfPool.ContainsKey(cls))
                {
                    continue;
                }

                var swf = new EmbedSwf(cls);
                swf.ready = (Action<EmbedSwf>)swfReadyBack;
                swf.error = (Action<string>)swfErrorBack;
                _swfPool[cls] = swf;
            }

            if (_swfPool.Count == 0)
            {
                finish();
            }
        }

        public virtual void addSwf(object c)
        {
            _swfPool ??= new Dictionary<object, object>();

            var swf = new EmbedSwf(c);
            swf.ready = (Action<EmbedSwf>)swfReadyBack;
            swf.error = (Action<string>)swfErrorBack;
            _swfPool[c] = swf;
        }

        public virtual object createDisplayObject(object embedSwf, string itemName)
        {
            var cls = getItemClass(embedSwf, itemName);
            if (cls != null)
            {
                var d = New(cls);
                if (d is Sprite)
                {
                    // AS3 applied GameData.I.config.soundVolume to the clip's soundTransform here;
                    // BVN.Flash sprites have no per-clip soundTransform (timeline sounds use SoundSystem.EffectVolume).
                    return d as Sprite;
                }
                return d;
            }
            return null;
        }

        public virtual BitmapData createBitmapData(object embedSwf, string itemName, int width, int height)
        {
            var cls = getItemClass(embedSwf, itemName);
            if (cls == null)
            {
                return null;
            }
            return EmbedSwf.CreateBitmapData(cls, width, height);
        }

        public virtual object getItemClass(object embedSwf, string itemName)
        {
            if (_swfPool == null)
            {
                throw new Exception(GetLang("debug.error.data.res_utils.not_initialized"));
            }

            var swf = getSwf(embedSwf);
            if (swf == null)
            {
                throw new Exception("swf is undefined!");
            }
            return swf.getClass(itemName);
        }

        public virtual object getItemProperty(object embedSwf, string name)
        {
            if (_swfPool == null)
            {
                throw new Exception(GetLang("debug.error.data.res_utils.not_initialized"));
            }

            var swf = getSwf(embedSwf);
            if (swf == null)
            {
                throw new Exception("swf is undefined!");
            }
            return swf.getProperty(name);
        }

        public virtual object callSwfFunction(object embedSwf, string func, List<object> @params = null)
        {
            if (_swfPool == null)
            {
                throw new Exception(GetLang("debug.error.data.res_utils.not_initialized"));
            }

            var swf = getSwf(embedSwf);
            if (swf == null)
            {
                throw new Exception("swf is undefined!");
            }
            return swf.call(func, @params);
        }

        private EmbedSwf getSwf(object embedSwf)
        {
            if (embedSwf == null)
            {
                return null;
            }
            _swfPool.TryGetValue(embedSwf, out var s);
            return s as EmbedSwf;
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

        private void swfErrorBack(string msg)
        {
            if (_initError != null)
            {
                Call(_initError);
            }
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
