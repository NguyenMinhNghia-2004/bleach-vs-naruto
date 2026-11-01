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
    /// Loads all maps / fighters / assisters / BGMs of a match before the fight stage starts.
    /// Unity port: the AS3 ApplicationDomain of a loaded SWF is represented by its converted LibraryData
    /// (AssetBridge), and Loader.content is read reflectively from whatever GameLoader passes back.
    /// </summary>
    public class GameStageLoadCtrl : EventDispatcher
    {
        // 是否忽略旧版角色
        public static bool IGNORE_OLD_FIGHTER = false;
        private static GameStageLoadCtrl _i;

        public static GameStageLoadCtrl I
        {
            get
            {
                _i ??= new GameStageLoadCtrl();
                return _i;
            }
        }

        /// <summary>AS3 cache entry { domain, mc, mcSlave }.</summary>
        private class SwfCacheVO
        {
            public LibraryData domain;
            public DisplayObject mc;
            public DisplayObject mcSlave;
        }

        /// <summary>AS3 internal class LoadAssetVO.</summary>
        private class LoadAssetVO
        {
            public string id;
            public string url;
            public string name;
        }

        private int _loadingType;

        private Dictionary<string, SwfCacheVO> _fighterCache;
        private Dictionary<string, SwfCacheVO> _assisterCache;
        private Dictionary<string, DisplayObject> _mapCache;

        private Delegate _progressCallBack;
        private Delegate _errorCallBack;

        private int _loadStep;
        private int _loadStepLength;

        private int _curLoadStep;
        private int _curLoadStepLength;

        private string _curLoadName;

        ////

        private List<MapVO> _loadMapDatas;
        private List<FighterVO> _loadFighterDatas;
        private List<FighterVO> _loadAssisterDatas;
        private List<BgmVO> _loadBgmDatas;

        private Delegate _loadFinishBack;

        public virtual void init(Delegate progressCallBack = null, Delegate errorCallBack = null)
        {
            _fighterCache = new Dictionary<string, SwfCacheVO>();
            _assisterCache = new Dictionary<string, SwfCacheVO>();
            _mapCache = new Dictionary<string, DisplayObject>();

            _loadStep = 0;
            _curLoadStep = 0;

            _progressCallBack = progressCallBack;
            _errorCallBack = errorCallBack;
        }

        public virtual void dispose()
        {
            _fighterCache = null;
            _assisterCache = null;
            _mapCache = null;
        }

        public virtual MovieClip getFighterMc(string fileUrl, string playerId)
        {
            return getMainMc(_fighterCache, fileUrl, playerId);
        }

        public virtual MovieClip getAssisterMc(string fileUrl, string playerId)
        {
            return getMainMc(_assisterCache, fileUrl, playerId);
        }

        private static MovieClip getMainMc(Dictionary<string, SwfCacheVO> cache, string fileUrl, string playerId)
        {
            SwfCacheVO obj = null;
            if (cache != null && fileUrl != null)
            {
                cache.TryGetValue(fileUrl, out obj);
            }

            LibraryData domain = obj?.domain ?? AssetBridge.LoadSwf(fileUrl);
            if (domain != null)
            {
                try
                {
                    object cls = domain.Linkage.TryGetValue(DefinedClass.MC_MAIN, out var link)
                        ? new LinkedClass(domain, DefinedClass.MC_MAIN, link[0])
                        : new LinkedClass(domain, DefinedClass.MC_MAIN, "root");
                    var mc = New(cls) as MovieClip;
                    if (mc != null) return mc;
                }
                catch (Exception)
                {
                }
            }

            if (obj != null)
            {
                if (playerId == "1" && obj.mc != null)
                {
                    return obj.mc as MovieClip;
                }
                if (obj.mcSlave != null)
                {
                    return obj.mcSlave as MovieClip;
                }
            }

            return domain != null ? MovieClip.CreateRoot(domain) : null;
        }

        public virtual MovieClip getMapMc(string fileUrl)
        {
            if (_mapCache == null || fileUrl == null) return null;
            return _mapCache.TryGetValue(fileUrl, out var mc) ? mc as MovieClip : null;
        }

        /// <summary>加载游戏所有素材，所有参数均为String数组，可以为NULL</summary>
        public virtual void loadGame(List<object> maps, List<object> fighters, List<object> assisters, List<object> bgms,
            Delegate finishBack = null)
        {
            _loadStep = 0;
            _loadStepLength = 0;

            List<MapVO> mapDatas = null;
            List<FighterVO> fighterDatas = null;
            List<FighterVO> assisterDatas = null;
            List<BgmVO> bgmDatas = null;

            _loadStepLength++;
            if (maps != null)
            {
                maps = unique(maps);
                mapDatas = new List<MapVO>();

                foreach (var id in maps)
                {
                    MapVO mv = MapModel.I.getMap(Str(id));
                    if (mv == null)
                    {
                        throw new Exception(GetLang("debug.error.data.game_stage_load_ctrl.get_map_data_fail"));
                    }
                    mapDatas.Add(mv);
                }
            }

            _loadStepLength++;
            if (fighters != null)
            {
                fighters = unique(fighters);
                fighterDatas = new List<FighterVO>();

                foreach (var id in fighters)
                {
                    FighterVO fv = FighterModel.I.getFighter(Str(id));
                    if (fv == null)
                    {
                        throw new Exception(GetLang("debug.error.data.game_stage_load_ctrl.get_fighter_data_fail"));
                    }
                    fighterDatas.Add(fv);
                }
            }

            _loadStepLength++;
            if (assisters != null)
            {
                assisters = unique(assisters);
                assisterDatas = new List<FighterVO>();

                foreach (var id in assisters)
                {
                    FighterVO av = AssisterModel.I.getAssister(Str(id));
                    if (av == null)
                    {
                        throw new Exception(GetLang("debug.error.data.game_stage_load_ctrl.get_assistant_data_fail"));
                    }
                    assisterDatas.Add(av);
                }
            }

            _loadStepLength++;
            if (bgms != null)
            {
                bgms = unique(bgms);
                bgmDatas = new List<BgmVO>();

                foreach (var o in bgms)
                {
                    string id = Str(o);
                    BgmVO bv = FighterModel.I.getFighterBGM(id);
                    if (bv == null) bv = MapModel.I.getMapBGM(id);
                    if (bv == null) bv = FighterModel.I.getFighterBGM(id);
                    if (bv == null) bv = FighterModel.I.getBossBGM(id);
                    if (bv != null) bgmDatas.Add(bv);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            _loadMapDatas = mapDatas;
            _loadFighterDatas = fighterDatas;
            _loadAssisterDatas = assisterDatas;
            _loadBgmDatas = bgmDatas;

            _loadFinishBack = finishBack;

            _loadStep = 1;

            setTimeout((Action)startLoadingMaps, 300);
        }

        private static ASObject assetKeyMap() => new ASObject { ["id"] = "id", ["name"] = "name", ["url"] = "fileUrl" };

        private void startLoadingMaps()
        {
            loadMaps(convertLoadAssets(_loadMapDatas, assetKeyMap()), () =>
            {
                setTimeout((Action)startloadFighters, 100);
            });
        }

        private void startloadFighters()
        {
            loadFighters(convertLoadAssets(_loadFighterDatas, assetKeyMap()), () =>
            {
                setTimeout((Action)startloadAssisters, 100);
            });
        }

        private void startloadAssisters()
        {
            loadAssisters(convertLoadAssets(_loadAssisterDatas, assetKeyMap()), () =>
            {
                setTimeout((Action)startloadBGM, 100);
            });
        }

        private void startloadBGM()
        {
            loadBgms(_loadBgmDatas, () =>
            {
                TraceLang("debug.trace.data.game_stage_load_ctrl.load_all_finish");
                if (_loadFinishBack != null)
                {
                    var back = _loadFinishBack;
                    _loadFinishBack = null;
                    Call(back);
                }
            });
        }

        private void loadMaps(List<LoadAssetVO> maps, Action callback)
        {
            void load(LoadAssetVO lv, Action succBack)
            {
                void loadSucc(object l)
                {
                    _mapCache[lv.url] = loaderContent(l, lv.url);
                    disposeLoader(l);
                    succBack();
                }

                GameLoader.loadSWF(lv.url, (Action<object>)loadSucc, (Action<string>)onLoadError, (Action<double>)onLoadProgress);
            }

            _loadingType = GameStageLoadDefine.TYPE_MAP;
            loadAssets(maps, load, callback);
        }

        private void loadFighters(List<LoadAssetVO> fighters, Action callback)
        {
            void load(LoadAssetVO lv, Action succBack)
            {
                void loadSucc(object l)
                {
                    _fighterCache[lv.url] = new SwfCacheVO
                    {
                        domain = loaderDomain(l, lv.url),
                        mc = null
                    };
                    disposeLoader(l);
                    succBack();
                }

                void loadSucc2(object l)
                {
                    _fighterCache[lv.url] = new SwfCacheVO
                    {
                        domain = loaderDomain(l, lv.url),
                        mc = loaderContent(l, lv.url)
                    };
                    disposeLoader(l);
                    GameLoader.loadSWF(lv.url, (Action<object>)loadSucc3, (Action<string>)onLoadError, (Action<double>)onLoadProgress);
                }

                void loadSucc3(object l)
                {
                    _fighterCache[lv.url].mcSlave = loaderContent(l, lv.url, true);
                    disposeLoader(l);
                    succBack();
                }

                if (IGNORE_OLD_FIGHTER)
                {
                    GameLoader.loadSWF(lv.url, (Action<object>)loadSucc2, (Action<string>)onLoadError, (Action<double>)onLoadProgress);
                }
                else
                {
                    GameLoader.loadSWF(lv.url, (Action<object>)loadSucc, (Action<string>)onLoadError, (Action<double>)onLoadProgress);
                }
            }

            _loadingType = GameStageLoadDefine.TYPE_FIGHTER;
            loadAssets(fighters, load, callback);
        }

        private void loadAssisters(List<LoadAssetVO> assissters, Action callback)
        {
            void load(LoadAssetVO lv, Action succBack)
            {
                void loadSucc(object l)
                {
                    _assisterCache[lv.url] = new SwfCacheVO
                    {
                        domain = loaderDomain(l, lv.url),
                        mc = IGNORE_OLD_FIGHTER ? loaderContent(l, lv.url) : null
                    };
                    disposeLoader(l);
                    succBack();
                }

                void loadSucc2(object l)
                {
                    _assisterCache[lv.url] = new SwfCacheVO
                    {
                        domain = loaderDomain(l, lv.url),
                        mc = loaderContent(l, lv.url)
                    };
                    disposeLoader(l);
                    GameLoader.loadSWF(lv.url, (Action<object>)loadSucc3, (Action<string>)onLoadError, (Action<double>)onLoadProgress);
                }

                void loadSucc3(object l)
                {
                    _assisterCache[lv.url].mcSlave = loaderContent(l, lv.url, true);
                    disposeLoader(l);
                    succBack();
                }

                if (IGNORE_OLD_FIGHTER)
                {
                    GameLoader.loadSWF(lv.url, (Action<object>)loadSucc2, (Action<string>)onLoadError, (Action<double>)onLoadProgress);
                }
                else
                {
                    GameLoader.loadSWF(lv.url, (Action<object>)loadSucc, (Action<string>)onLoadError, (Action<double>)onLoadProgress);
                }
            }

            _loadingType = GameStageLoadDefine.TYPE_ASSISTER;
            loadAssets(assissters, load, callback);
        }

        private void loadBgms(List<BgmVO> bgms, Action callback)
        {
            void succBack()
            {
                _loadStep++;
                _curLoadName = null;
                callback();
            }

            if (bgms == null)
            {
                callback();
                return;
            }

            _loadingType = GameStageLoadDefine.TYPE_BGM;
            _curLoadStep = 0;
            _curLoadStepLength = 1;
            SoundCtrl.I.loadFightBGM(bgms, (Action)succBack, callback, (Action<double>)onLoadProgress);
        }

        private void onLoadProgress(double v)
        {
            string itemName = null;
            switch (_loadingType)
            {
                case GameStageLoadDefine.TYPE_MAP:
                    itemName = GetLang("txt.game_stage_load_ctrl.map");
                    break;
                case GameStageLoadDefine.TYPE_FIGHTER:
                    itemName = GetLang("txt.game_stage_load_ctrl.fighter");
                    break;
                case GameStageLoadDefine.TYPE_ASSISTER:
                    itemName = GetLang("txt.game_stage_load_ctrl.assistant");
                    break;
                case GameStageLoadDefine.TYPE_BGM:
                    itemName = GetLang("txt.game_stage_load_ctrl.bgm");
                    break;
            }

            string msg;
            if (!string.IsNullOrEmpty(_curLoadName))
            {
                msg = GetLang("txt.game_stage_load_ctrl.loading_has_name", new ASObject
                {
                    ["itemName"] = itemName,
                    ["loadName"] = _curLoadName,
                    ["step"] = _loadStep,
                    ["stepLength"] = _loadStepLength
                });
            }
            else
            {
                msg = GetLang("txt.game_stage_load_ctrl.loading_no_name", new ASObject
                {
                    ["itemName"] = itemName,
                    ["step"] = _loadStep,
                    ["stepLength"] = _loadStepLength
                });
            }

            double val = (_curLoadStep + v) / _curLoadStepLength;

            Call(_progressCallBack, msg, val);
        }

        private void onLoadError(string msg)
        {
            Call(_errorCallBack, msg);
        }

        /// <summary>AS3 Loader.content. The loader object comes from GameLoader.loadSWF; it may be the content itself,
        /// or an object exposing `content`. Falls back to a fresh main timeline of the SWF.</summary>
        private static DisplayObject loaderContent(object l, string url, bool fresh = false)
        {
            if (fresh)
            {
                // the "slave" copy must be a separate instance (AS3 loaded the SWF a second time)
                var r = AssetBridge.CreateSwfRoot(url);
                if (r != null) return r;
            }
            if (l is DisplayObject d) return d;
            if (l is LibraryData lib) return MovieClip.CreateRoot(lib);
            DisplayObject c = l != null ? Get(l, "content") as DisplayObject : null;
            return c ?? AssetBridge.CreateSwfRoot(url);
        }

        /// <summary>AS3 Loader.contentLoaderInfo.applicationDomain → the converted library of the SWF.</summary>
        private static LibraryData loaderDomain(object l, string url)
        {
            if (l is LibraryData lib) return lib;
            if (l is MovieClip mc && mc.Library != null) return mc.Library;
            if (l != null && Get(l, "content") is MovieClip cmc && cmc.Library != null) return cmc.Library;
            return AssetBridge.LoadSwf(url);
        }

        /// <summary>
        /// AS3 called Loader.unloadAndStop(true) here. In Unity the converted library must stay loaded while the cached
        /// clips / class definitions are in use (GameLoader.dispose releases loaders at match end), so nothing is unloaded.
        /// </summary>
        private static void disposeLoader(object l)
        {
        }

        // 数组去重
        private static List<object> unique(List<object> arr)
        {
            var hash = new List<object>();
            for (int i = 0; i < arr.Count; i++)
            {
                if (IsNull(arr[i])) continue;

                for (int j = i + 1; j < arr.Count; j++)
                {
                    if (Ops.LooseEquals(arr[i], arr[j]))
                    {
                        ++i;
                    }
                }
                hash.Add(arr[i]);
            }
            return hash;
        }

        // 加载资源主要逻辑
        private void loadAssets(List<LoadAssetVO> assets, Action<LoadAssetVO, Action> loadFunc, Action callback)
        {
            LoadAssetVO currentAsset;

            void loadNext()
            {
                if (assets.Count < 1)
                {
                    _loadStep++;
                    _curLoadName = null;
                    callback();
                    return;
                }
                LoadAssetVO fv = shift(assets);
                currentAsset = fv;
                _curLoadName = fv.name;

                loadFunc(fv, loadSucess);
            }

            void loadSucess()
            {
                _curLoadStep++;
                loadNext();
            }

            if (assets == null)
            {
                callback();
                return;
            }

            _curLoadStep = 0;
            _curLoadStepLength = assets.Count;

            loadNext();
        }

        private static List<LoadAssetVO> convertLoadAssets(object source, object keyMap)
        {
            var result = new List<LoadAssetVO>();
            var urlMap = new HashSet<string>();
            if (source == null) return result;
            foreach (var i in Values(source))
            {
                var lv = new LoadAssetVO();
                foreach (var jo in Keys(keyMap))
                {
                    string j = Str(jo);
                    string k = Str(Get(keyMap, j));
                    string v = Str(Get(i, k));
                    switch (j)
                    {
                        case "id": lv.id = v; break;
                        case "url": lv.url = v; break;
                        case "name": lv.name = v; break;
                    }
                }
                string urlKey = lv.url ?? "null";
                if (urlMap.Contains(urlKey)) continue;

                urlMap.Add(urlKey);
                result.Add(lv);
            }
            return result;
        }
    }
}
