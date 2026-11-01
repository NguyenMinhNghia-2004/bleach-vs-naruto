using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameData
    {
        private const string SAVE_ID = "bvn3.5A";

        // 是否启动无双模式的数据保存
        private const bool __MUSOU_DATA_ENABLED = true;

        private static GameData _i;

        public static GameData I
        {
            get
            {
                _i ??= new GameData();
                return _i;
            }
        }

        public ConfigVO config = new ConfigVO();
        public MusouPlayerData musouData = new MusouPlayerData();

        public SelectVO p1Select;
        public SelectVO p2Select;
        public string selectMap;

        public int score = 0;

        public string winnerId;

        public bool isFristRun = true;

        public virtual void loadConfig(Delegate back, Delegate fail = null)
        {
            Delegate failWith(string langKey)
            {
                return (Action)(() =>
                {
                    string msg = GetLang(langKey);
                    Debugger.log(msg);
                    if (fail != null)
                    {
                        Call(fail, msg);
                    }
                });
            }

            void loadMusouDataBack()
            {
                MusouFighterModel.I.init();

                validateSelect();
                validateMissionData();
                validateMusouData();

                if (back != null)
                {
                    Call(back);
                }
            }

            void loadMissionBack(object data)
            {
                MessionModel.I.initByObject(data);
                MusouModel.I.loadMapData((Action)loadMusouDataBack, failWith("txt.game_data.load_musou_fail"));
            }

            void loadSelectBack(object data)
            {
                config.select_config.initByObject(data);
                AssetManager.I.loadJSON("config/mission.json", (Action<object>)loadMissionBack, failWith("txt.game_data.load_mission_fail"));
            }

            void loadPacksBack()
            {
                AssetManager.I.loadJSON("config/select.json", (Action<object>)loadSelectBack, failWith("txt.game_data.load_select_fail"));
            }

            PackRegistry.I.load((Action)loadPacksBack, failWith("txt.game_data.load_fighter_fail"));
        }

        public virtual void initData()
        {
            musouData.init();
            //			GameData.I.loadSaveData();
        }

        public virtual void saveData()
        {
            ASObject o = new ASObject();
            o["id"] = SAVE_ID;

            o["config"] = config.toSaveObj();
            o["musou"] = musouData.toSaveObj();

            GameInterface.instance.saveGame(o);
        }

        public virtual void loadSaveData()
        {
            object o = GameInterface.instance.loadGame();
            if (o == null || Str(Get(o, "id")) != SAVE_ID)
            {
                return;
            }

            if (Bool(Get(o, "config")))
            {
                config.readSaveObj(Get(o, "config"));
            }

            if (Bool(Get(o, "musou")) && __MUSOU_DATA_ENABLED)
            {
                musouData.readSaveObj(Get(o, "musou"));
            }
        }

        public virtual void loadSelect(string url)
        {
            AssetManager.I.loadJSON(url, (Action<object>)(data =>
            {
                setSelectData(data);
            }), (Action)(() =>
            {
                TraceLang("debug.trace.data.game_data.load_select_error");
            }));
        }

        public virtual void setSelectData(object data)
        {
            config.select_config.initByObject(data);
        }

        // 验证选人（缺失软降级：空槽 / 过滤 more，不抛错）
        private void validateSelect()
        {
            List<object> missFighters = new List<object>();
            List<object> missAssisters = new List<object>();
            FighterVO fighter;

            foreach (var so in config.select_config.charList.list)
            {
                var s = (SelectCharListItemVO)so;
                if (!string.IsNullOrEmpty(s.fighterID) && FighterModel.I.getFighter(s.fighterID) == null)
                {
                    if (missFighters.IndexOf(s.fighterID) == -1)
                    {
                        missFighters.Add(s.fighterID);
                    }
                    s.fighterID = null;
                }
                s.moreFighterIDs = filterExistingIds(s.moreFighterIDs, false, missFighters);
                foreach (var fo in s.getAllFighterIDs())
                {
                    string f = Str(fo);
                    fighter = FighterModel.I.getFighter(f);
                    if (fighter == null && missFighters.IndexOf(f) == -1)
                    {
                        missFighters.Add(f);
                    }
                }
            }

            foreach (var so in config.select_config.assistList.list)
            {
                var s = (SelectCharListItemVO)so;
                if (!string.IsNullOrEmpty(s.fighterID) && AssisterModel.I.getAssister(s.fighterID) == null)
                {
                    if (missAssisters.IndexOf(s.fighterID) == -1)
                    {
                        missAssisters.Add(s.fighterID);
                    }
                    s.fighterID = null;
                }
                s.moreFighterIDs = filterExistingIds(s.moreFighterIDs, true, missAssisters);
                foreach (var fo in s.getAllFighterIDs())
                {
                    string f = Str(fo);
                    fighter = AssisterModel.I.getAssister(f);
                    if (fighter == null && missAssisters.IndexOf(f) == -1)
                    {
                        missAssisters.Add(f);
                    }
                }
            }

            if (missFighters.Count > 0 || missAssisters.Count > 0)
            {
                string msg = "";
                if (missFighters.Count > 0)
                {
                    msg += "fighter : " + join(missFighters, " , ") + " ; ";
                }
                if (missAssisters.Count > 0)
                {
                    msg += "assister : " + join(missAssisters, " , ") + " ; ";
                }
                Debugger.log(GetLang("debug.error.data.game_data.verify_select_fail", new ASObject { ["file"] = "select.json", ["message"] = msg }));
            }
        }

        /// <summary>过滤仍存在于模型中的 id 列表；全无则 null。</summary>
        private List<object> filterExistingIds(List<object> ids, bool assister, List<object> missOut)
        {
            if (ids == null || ids.Count < 1)
            {
                return ids;
            }
            List<object> kept = new List<object>();
            foreach (var idObj in ids)
            {
                string id = Str(idObj);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }
                bool ok = assister ? AssisterModel.I.getAssister(id) != null : FighterModel.I.getFighter(id) != null;
                if (ok)
                {
                    kept.Add(id);
                }
                else if (missOut.IndexOf(id) == -1)
                {
                    missOut.Add(id);
                }
            }
            return kept.Count > 0 ? kept : null;
        }

        // 验证关卡（缺失软降级：跳过缺角 id，不抛错）
        private void validateMissionData()
        {
            List<object> missFighters = new List<object>();
            List<object> missMaps = new List<object>();
            List<object> missAssisters = new List<object>();

            List<object> missions = MessionModel.I.getAllMissions();
            if (missions != null)
            {
                foreach (var mo in missions)
                {
                    var m = mo as MessionVO;
                    if (m == null || m.stageList == null) continue;
                    List<MessionStageVO> ms = m.stageList;
                    foreach (var s in ms)
                    {
                        if (s.fighters != null)
                        {
                            List<object> kept = new List<object>();
                            foreach (var fo in s.fighters)
                            {
                                string f = Str(fo);
                                FighterVO fighter = FighterModel.I.getFighter(f);
                                if (fighter == null)
                                {
                                    if (missFighters.IndexOf(f) == -1)
                                    {
                                        missFighters.Add(f);
                                    }
                                }
                                else
                                {
                                    kept.Add(f);
                                }
                            }
                            s.fighters = kept;
                        }

                        MapVO map = MapModel.I.getMap(s.map);
                        if (map == null)
                        {
                            if (missMaps.IndexOf(s.map) == -1)
                            {
                                missMaps.Add(s.map);
                            }
                        }

                        if (!string.IsNullOrEmpty(s.assister))
                        {
                            FighterVO assister = AssisterModel.I.getAssister(s.assister);
                            if (assister == null)
                            {
                                if (missAssisters.IndexOf(s.assister) == -1)
                                {
                                    missAssisters.Add(s.assister);
                                }
                                s.assister = null;
                            }
                        }
                    }
                }
            }

            if (missFighters.Count > 0 || missAssisters.Count > 0 || missMaps.Count > 0)
            {
                string msg = "";
                if (missFighters.Count > 0)
                {
                    msg += "fighter : " + join(missFighters, " , ") + " ; ";
                }
                if (missAssisters.Count > 0)
                {
                    msg += "assister : " + join(missAssisters, " , ") + " ; ";
                }
                if (missMaps.Count > 0)
                {
                    msg += "map : " + join(missMaps, " , ") + " ; ";
                }
                Debugger.log(GetLang("debug.error.data.game_data.verify_mission_fail", new ASObject { ["file"] = "mission.json", ["message"] = msg }));
            }
        }

        // 验证无双关卡（缺失软降级：仅日志，不抛错）
        private void validateMusouData()
        {
            object mapObj = MusouModel.I.getAllMap();
            foreach (var mo in Values(mapObj))
            {
                var mwv = mo as MusouWorldMapVO;
                if (mwv == null || mwv.areas == null) continue;
                foreach (var a in mwv.areas)
                {
                    if (a.missions == null) continue;
                    foreach (var mv in a.missions)
                    {
                        string musouId = mwv.id + " - " + a.id + " - " + mv.id;

                        MapVO map = MapModel.I.getMap(mv.map);
                        if (map == null)
                        {
                            Debugger.log(GetLang("debug.error.data.game_data.verify_musou_fail", new ASObject
                            {
                                ["musouId"] = musouId,
                                ["type"] = "map",
                                ["value"] = mv.map,
                            }));
                        }

                        List<object> ememies = mv.getAllEnemieIds();
                        foreach (var fo in ememies)
                        {
                            string f = Str(fo);
                            FighterVO fighter = FighterModel.I.getFighter(f);
                            if (fighter == null)
                            {
                                Debugger.log(GetLang("debug.error.data.game_data.verify_musou_fail", new ASObject
                                {
                                    ["musouId"] = musouId,
                                    ["type"] = "fighter",
                                    ["value"] = f,
                                }));
                            }
                        }
                    }
                }
            }

            List<object> missFighters = new List<object>();
            List<MusouFighterSellVO> fighters = MusouFighterModel.I.fighters;
            if (fighters != null)
            {
                foreach (var s in fighters)
                {
                    FighterVO fv = FighterModel.I.getFighter(s.id);
                    if (fv == null && missFighters.IndexOf(s.id) == -1)
                    {
                        missFighters.Add(s.id);
                    }
                }
            }

            if (missFighters.Count > 0)
            {
                string msg = "";
                msg += "fighter : " + join(missFighters, " , ") + " ; ";
                Debugger.log(GetLang("debug.error.data.game_data.verify_fighter_model_fail", new ASObject { ["message"] = msg }));
            }
        }
    }
}
