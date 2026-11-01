using System;
using System.Collections.Generic;
using BVN.Data;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Default UI SWF library set (AS3: shared/lib/swf/*.swf embedded with [Embed]).
    /// In this port each embedded SWF is a converted library under Resources/bvn/ui/&lt;name&gt;; every getter returns that
    /// <see cref="LibraryData"/> (loaded lazily, cached), which ResUtils keeps using as its "embedSwf" key.
    /// Embed mapping: shared/lib/swf/&lt;name&gt;.swf → LibraryData.Load("ui/&lt;name&gt;").
    /// Static helpers (<see cref="getClass"/>, <see cref="createRoot"/>, <see cref="getLibrary"/>) replace
    /// EmbedSwf's Loader.loadBytes + ApplicationDomain.getDefinition.
    /// </summary>
    public class SwfLib : ISwfLib
    {
        /// <summary>Folder of the converted UI libraries (relative to Resources/bvn/).</summary>
        public const string UI_ROOT = "ui/";

        /// <summary>All UI library names embedded by the AS3 SwfLib.</summary>
        public static readonly List<string> UI_LIBS = new List<string>
        {
            "big_map", "common", "dialog", "fight", "game_over", "how2play",
            "language", "loading", "musou", "select", "setting", "title"
        };

        /// <summary>Other converted shared libraries searched as a last resort (Flash shared one ApplicationDomain).</summary>
        public static readonly List<string> EXTRA_LIBS = new List<string>
        {
            "swc/component", "swc/shared", "swc/sound", "effect/effect"
        };

        /// <summary>big map UI</summary>
        private object _big_map;
        /// <summary>shared common UI</summary>
        private object _common;
        /// <summary>dialog UI</summary>
        private object _dialog;
        /// <summary>fight UI</summary>
        private object _fight;
        /// <summary>game over UI</summary>
        private object _game_over;
        /// <summary>how to play UI</summary>
        private object _how2play;
        /// <summary>language select UI</summary>
        private object _language;
        /// <summary>loading UI</summary>
        private object _loading;
        /// <summary>musou UI</summary>
        private object _musou;
        /// <summary>fighter / map select UI</summary>
        private object _select;
        /// <summary>setting UI</summary>
        private object _setting;
        /// <summary>title (main menu) UI</summary>
        private object _title;

        public virtual object big_map => _big_map ??= loadUILib("big_map");
        public virtual object common => _common ??= loadUILib("common");
        public virtual object dialog => _dialog ??= loadUILib("dialog");
        public virtual object fight => _fight ??= loadUILib("fight");
        public virtual object game_over => _game_over ??= loadUILib("game_over");
        public virtual object how2play => _how2play ??= loadUILib("how2play");
        public virtual object language => _language ??= loadUILib("language");
        public virtual object loading => _loading ??= loadUILib("loading");
        public virtual object musou => _musou ??= loadUILib("musou");
        public virtual object select => _select ??= loadUILib("select");
        public virtual object setting => _setting ??= loadUILib("setting");
        public virtual object title => _title ??= loadUILib("title");

        /// <summary>Loads (cached) the converted UI library <c>ui/&lt;name&gt;</c>; null when it does not exist.</summary>
        public static LibraryData loadUILib(string name)
        {
            return tryLoad(UI_ROOT + name);
        }

        private static LibraryData tryLoad(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                return LibraryData.Load(path);
            }
            catch (Exception e)
            {
                trace("SwfLib: library not found", path, e.Message);
                return null;
            }
        }

        /// <summary>
        /// Resolves an "embedSwf" value to its library: LibraryData itself, a LinkedClass's library,
        /// or a swf url / library path string (via AssetBridge).
        /// </summary>
        public static LibraryData getLibrary(object embedSwf)
        {
            switch (embedSwf)
            {
                case LibraryData lib: return lib;
                case LinkedClass lc: return lc.Library;
                case string s:
                    if (UI_LIBS.IndexOf(s) != -1) return loadUILib(s);
                    return AssetBridge.LoadSwf(s);
            }
            return null;
        }

        /// <summary>
        /// AS3 ApplicationDomain.getDefinition(name) for an embedded SWF. All embedded SWFs were loaded into
        /// ApplicationDomain.currentDomain, so (like Flash) the lookup falls back to every UI library:
        /// 1) the given library, 2) the library named by the "$lib$" prefix of the linkage name,
        /// 3) all UI libraries, 4) the shared swc/effect libraries, 5) any library already loaded (AssetBridge).
        /// </summary>
        public static LinkedClass getClass(object embedSwf, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var cls = findIn(getLibrary(embedSwf), name);
            if (cls != null) return cls;

            // "$fight$MC_fightUI" → ui/fight
            if (name.Length > 2 && name[0] == '$')
            {
                int end = name.IndexOf('$', 1);
                if (end > 1)
                {
                    var prefix = name.Substring(1, end - 1);
                    if (UI_LIBS.IndexOf(prefix) != -1)
                    {
                        cls = findIn(loadUILib(prefix), name);
                        if (cls != null) return cls;
                    }
                }
            }

            foreach (var n in UI_LIBS)
            {
                cls = findIn(loadUILib(n), name);
                if (cls != null) return cls;
            }

            foreach (var p in EXTRA_LIBS)
            {
                if (!LibraryData.Exists(p)) continue;
                cls = findIn(tryLoad(p), name);
                if (cls != null) return cls;
            }

            return AssetBridge.GetClass(name);
        }

        private static LinkedClass findIn(LibraryData lib, string name)
        {
            if (lib == null) return null;
            if (lib.Linkage.TryGetValue(name, out var l)) return new LinkedClass(lib, name, l[0]);
            return null;
        }

        /// <summary>The embedded SWF's main timeline (AS3 LoaderInfo.content), for property access / function calls.</summary>
        public static MovieClip createRoot(object embedSwf)
        {
            var lib = getLibrary(embedSwf);
            return lib == null ? null : MovieClip.CreateRoot(lib);
        }
    }
}
