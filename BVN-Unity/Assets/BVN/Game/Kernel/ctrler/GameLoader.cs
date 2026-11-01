using System;
using System.Collections.Generic;
using BVN.Data;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameLoader
    {
        // 角色缓存{ String: <FighterCacheVO> }
        private static Dictionary<string, FighterCacheVO> _fighterCache = new Dictionary<string, FighterCacheVO>();

        // swf缓存 (AS3 Loader list → loaded swf url + loader object handed back by IAssetLoader)
        private static List<KeyValuePair<string, object>> _loadedSwfCache = new List<KeyValuePair<string, object>>();

        public static void loadAndCacheFighter(string fighterId, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            if (fighterId != null && _fighterCache.TryGetValue(fighterId, out var cached) && cached != null)
            {
                if (back != null) Call(back);
                return;
            }

            FighterVO fv = FighterModel.I.getFighter(fighterId, true);
            if (fv == null)
            {
                TraceLang("debug.trace.data.game_loader.fighter_id_error", new ASObject { ["fighterId"] = fighterId });
                if (fail != null) Call(fail, GetLang("txt.game_loader.fighter_id_error"));
                return;
            }

            string url = fv.fileUrl;

            void loadComplete(object loader)
            {
                var cachevo = new FighterCacheVO();
                cachevo.loader = loader;
                cachevo.url = url;
                cachevo.fighterData = fv;

                _fighterCache[fighterId] = cachevo;

                try
                {
                    var mainMcClass = AssetBridge.GetClass("main_mc", url);
                    if (mainMcClass == null)
                    {
                        var lib = AssetBridge.LoadSwf(url);
                        if (lib != null) mainMcClass = new LinkedClass(lib, "main_mc", "root");
                    }
                    if (mainMcClass == null) throw new Exception("Variable main_mc is not defined.");
                    cachevo.MainClass = mainMcClass;
                }
                catch (Exception e)
                {
                    TraceLang("debug.trace.data.game_loader.fighter_load_error", new ASObject { ["error"] = e.Message });
                }

                if (back != null) Call(back);
            }

            loadSWF(url, (Action<object>)loadComplete, fail, progress);
        }

        public static void loadFighter(string fighterId, Delegate back, Delegate fail = null, Delegate progress = null, object customBackParam = null)
        {
            FighterMain result = createCacheFighter(fighterId);

            if (result != null)
            {
                if (back != null)
                {
                    if (Bool(customBackParam)) Call(back, result, customBackParam);
                    else Call(back, result);
                }
                return;
            }

            void loadComplete()
            {
                result = createCacheFighter(fighterId);

                if (back != null)
                {
                    if (Bool(customBackParam)) Call(back, result, customBackParam);
                    else Call(back, result);
                }
            }

            loadAndCacheFighter(fighterId, (Action)loadComplete, fail, progress);
        }

        public static FighterMain createCacheFighter(string fighterId)
        {
            if (fighterId == null || !_fighterCache.TryGetValue(fighterId, out var fcv) || fcv == null) return null;

            MovieClip mainMc = null;
            if (fcv.MainClass != null)
                mainMc = New(fcv.MainClass) as MovieClip;
            else if (fcv.loader != null)
                mainMc = getLoaderContent(fcv.loader, fcv.url) as MovieClip;

            if (mainMc == null && !string.IsNullOrEmpty(fcv.url))
            {
                var lib = AssetBridge.LoadSwf(fcv.url);
                if (lib != null) mainMc = MovieClip.CreateRoot(lib);
            }

            var fighter = new FighterMain(mainMc);
            fighter.data = fcv.fighterData;

            return fighter;
        }

        public static void loadAssister(string fighterId, Delegate back, Delegate fail = null, Delegate progress = null, object customBackParam = null)
        {
            FighterVO fv = AssisterModel.I.getAssister(fighterId, true);
            if (fv == null)
            {
                TraceLang("debug.trace.data.game_loader.assister_id_error", new ASObject { ["fighterId"] = fighterId });
                if (fail != null) Call(fail, GetLang("txt.game_loader.assister_id_error"));
                return;
            }

            string url = fv.fileUrl;

            void loadComplete(object loader)
            {
                var fighter = new Assister(getLoaderContent(loader, url) as MovieClip);
                fighter.data = fv;
                if (back != null)
                {
                    if (Bool(customBackParam)) Call(back, fighter, customBackParam);
                    else Call(back, fighter);
                    back = null;
                }
            }

            loadSWF(url, (Action<object>)loadComplete, fail, progress);
        }

        public static void loadMap(string mapId, Delegate back, Delegate fail = null, Delegate progress = null, object customBackParam = null)
        {
            MapVO mv = MapModel.I.getMap(mapId);
            if (mv == null)
            {
                TraceLang("debug.trace.data.game_loader.map_id_error", new ASObject { ["mapId"] = mapId });
                if (fail != null) Call(fail, GetLang("txt.game_loader.map_id_error"));
                return;
            }

            string url = mv.fileUrl;

            void loadComplete(object loader)
            {
                var map = new MapMain(getLoaderContent(loader, url) as Sprite);
                map.data = mv;
                if (back != null)
                {
                    if (Bool(customBackParam)) Call(back, map, customBackParam);
                    else Call(back, map);
                    back = null;
                }
            }

            loadSWF(url, (Action<object>)loadComplete, fail, progress);
        }

        public static void dispose()
        {
            foreach (var cv in _fighterCache.Values)
            {
                if (cv == null) continue;
                cv.fighterData = null;
                cv.MainClass = null;
                cv.loader = null;
            }

            while (_loadedSwfCache.Count > 0)
            {
                var l = shift(_loadedSwfCache);
                try
                {
                    // Loader.unloadAndStop → release the converted library
                    AssetBridge.Unload(l.Key);
                }
                catch (Exception e)
                {
                    TraceLang("debug.trace.data.game_loader.unload_cache_error", new ASObject { ["error"] = e.Message });
                }
            }

            _fighterCache = new Dictionary<string, FighterCacheVO>();
        }

        public static void loadSWF(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            void loadComplete(object l)
            {
                _loadedSwfCache.Add(new KeyValuePair<string, object>(url, l));
                if (back != null)
                {
                    Call(back, l);
                    back = null;
                }
            }

            void loadIOError()
            {
                Debugger.log(GetLang("debug.log.data.game_loader.load_swf_error", new ASObject { ["url"] = url }));
                if (fail != null) Call(fail, GetLang("txt.game_loader.load_swf_error"));
            }

            AssetManager.I.loadSWF(url, (Action<object>)loadComplete, (Action)loadIOError, progress);
        }

        /// <summary>
        /// AS3 `loader.content`: accepts whatever the IAssetLoader handed back (display object, LibraryData,
        /// an object with a `content` member) and falls back to the SWF main timeline from AssetBridge.
        /// </summary>
        private static DisplayObject getLoaderContent(object loader, string url)
        {
            switch (loader)
            {
                case DisplayObject d: return d;
                case LibraryData lib: return MovieClip.CreateRoot(lib);
            }
            if (loader != null && Get(loader, "content") is DisplayObject c) return c;
            return AssetBridge.CreateSwfRoot(url);
        }
    }

    /// <summary>AS3 file-internal class of GameLoader.</summary>
    public class FighterCacheVO
    {
        public object loader;
        public string url;
        public object MainClass;
        public FighterVO fighterData;
    }
}
