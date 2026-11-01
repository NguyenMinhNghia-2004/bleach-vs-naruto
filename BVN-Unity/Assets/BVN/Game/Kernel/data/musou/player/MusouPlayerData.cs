using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouPlayerData : ISaveData
    {
        public string userId;
        public string userName;

        private WrapInteger _money = new WrapInteger(0);
        private List<MusouWorldMapPlayerVO> _mapData = new List<MusouWorldMapPlayerVO>();
        private List<MusouFighterVO> _fighterData = new List<MusouFighterVO>(); //拥有的角色
        private List<MusouFighterVO> _fighterTeam = new List<MusouFighterVO>(); //参战的角色
        private WrapInteger _lastLogin = new WrapInteger(0);

        private string _currentMapId = "map1";
        private string _currentAreaId = "p1";

        public virtual void init()
        {
            initMap();
            _money.setValue(3000);

            _fighterData.Add(MusouFighterFactory.create("ichigo"));
            _fighterData.Add(MusouFighterFactory.create("naruto"));
            _fighterData.Add(MusouFighterFactory.create("sakura"));

            _fighterTeam = new List<MusouFighterVO>(_fighterData);
        }

        public virtual List<MusouFighterVO> getFighterData()
        {
            return _fighterData;
        }

        public virtual MusouFighterVO addFighter(string id)
        {
            MusouFighterVO fv = getFighterDataById(id);
            if (fv != null)
            {
                return fv;
            }
            fv = MusouFighterFactory.create(id);
            _fighterData.Add(fv);
            return fv;
        }

        public virtual List<MusouFighterVO> getFighterTeam()
        {
            return _fighterTeam;
        }

        public virtual List<object> getFighterTeamIds()
        {
            List<object> ids = new List<object>();
            for (int i = 0; i < _fighterTeam.Count; i++)
            {
                ids.Add(_fighterTeam[i].id);
            }
            return ids;
        }

        // AS3 Vector index assignment (index == length appends)
        private static void setAt(List<MusouFighterVO> list, int index, MusouFighterVO v)
        {
            if (index < list.Count) list[index] = v;
            else if (index == list.Count) list.Add(v);
            else throw new ArgumentOutOfRangeException(nameof(index));
        }

        public virtual void setFighterTeam(int index, string id)
        {
            MusouFighterVO mv = getFighterDataById(id);
            if (mv == null)
            {
                return;
            }

            setAt(_fighterTeam, index, mv);

            GameEvent.dispatchEvent(GameEvent.MUSOU_FIGHTER_UPDATE);
        }

        public virtual void setLeader(MusouFighterVO v)
        {
            int index = _fighterTeam.IndexOf(v);
            if (index == -1)
            {
                return;
            }
            if (index == 0)
            {
                return;
            }

            List<MusouFighterVO> tmp = new List<MusouFighterVO>(_fighterTeam);

            switch (index)
            {
                case 1:
                    setAt(_fighterTeam, 2, tmp[0]);
                    _fighterTeam[1] = tmp[2];
                    _fighterTeam[0] = tmp[1];
                    break;
                case 2:
                    _fighterTeam[2] = tmp[1];
                    _fighterTeam[1] = tmp[0];
                    _fighterTeam[0] = tmp[2];
                    break;
            }

            GameEvent.dispatchEvent(GameEvent.MUSOU_FIGHTER_UPDATE);
        }

        public virtual MusouFighterVO getLeader()
        {
            return _fighterTeam.Count > 0 ? _fighterTeam[0] : null;
        }

        public virtual MusouFighterVO getFighterDataById(string id)
        {
            foreach (var i in _fighterData)
            {
                if (i.id == id)
                {
                    return i;
                }
            }
            return null;
        }

        public virtual MusouWorldMapPlayerVO getCurrentMap()
        {
            return getMapById(_currentMapId);
        }

        public virtual MusouWorldMapAreaPlayerVO getCurrentArea()
        {
            MusouWorldMapPlayerVO map = getCurrentMap();
            if (map == null)
            {
                return null;
            }
            return map.getOpenArea(_currentAreaId);
        }

        public virtual void setCurrentArea(string areaId)
        {
            _currentAreaId = areaId;
        }

        public virtual MusouWorldMapAreaPlayerVO getCurrentMapAreaById(string id)
        {
            MusouWorldMapPlayerVO map = getCurrentMap();
            if (map == null)
            {
                return null;
            }
            return map.getOpenArea(id);
        }

        public virtual MusouWorldMapPlayerVO getMapById(string id)
        {
            foreach (var i in _mapData)
            {
                if (i.id == id)
                {
                    return i;
                }
            }
            return null;
        }

        public virtual int getMoney()
        {
            return _money.getValue();
        }

        public virtual void addMoney(int v)
        {
            int vv = _money.getValue() + v;
            _money.setValue(vv);
            GameEvent.dispatchEvent(GameEvent.MONEY_UPDATE);
        }

        public virtual void loseMoney(int v)
        {
            int vv = _money.getValue() - v;
            if (vv < 0)
            {
                vv = 0;
            }
            _money.setValue(vv);
            GameEvent.dispatchEvent(GameEvent.MONEY_UPDATE);
        }

        public virtual void addFighterExp(int v)
        {
            for (int i = 0; i < _fighterTeam.Count; i++)
            {
                if (i == 0)
                {
                    _fighterTeam[i].addExp(v * 2);
                }
                else
                {
                    _fighterTeam[i].addExp(v);
                }
            }
        }

        public virtual void login(string userId = null, string userName = null)
        {
            this.userId = userId;
            this.userName = userName;

            // AS3: int(new Date().getTime())
            int date = Int((double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _lastLogin.setValue(date);
        }

        public virtual object toSaveObj()
        {
            int i;
            object d;
            ASObject o = new ASObject();
            o["userId"] = userId;
            o["userName"] = userName;
            o["money"] = _money.getValue();
            o["lastLogin"] = _lastLogin.getValue();

            List<object> mapData = new List<object>();
            o["mapData"] = mapData;
            for (i = 0; i < _mapData.Count; i++)
            {
                d = _mapData[i].toSaveObj();
                mapData.Add(d);
            }

            List<object> fighterData = new List<object>();
            o["fighterData"] = fighterData;
            for (i = 0; i < _fighterData.Count; i++)
            {
                d = _fighterData[i].toSaveObj();
                fighterData.Add(d);
            }

            o["currentMapId"] = _currentMapId;
            o["currentAreaId"] = _currentAreaId;

            List<object> fighterTeam = new List<object>();
            o["fighterTeam"] = fighterTeam;
            for (i = 0; i < _fighterTeam.Count; i++)
            {
                fighterTeam.Add(_fighterTeam[i].id);
            }

            return o;
        }

        public virtual void readSaveObj(object o)
        {
            int i;
            object d;
            MusouWorldMapPlayerVO mv;
            MusouFighterVO fv;

            userId = Str(Get(o, "userId"));
            userName = Str(Get(o, "userName"));

            if (Bool(Get(o, "money")))
            {
                _money.setValue(Int(Get(o, "money")));
            }

            if (Bool(Get(o, "lastLogin")))
            {
                _lastLogin.setValue(Int(Get(o, "lastLogin")));
            }

            List<object> mapData = Get(o, "mapData") != null ? ToArray(Get(o, "mapData")) : null;
            if (mapData != null)
            {
                _mapData = new List<MusouWorldMapPlayerVO>();
                for (i = 0; i < mapData.Count; i++)
                {
                    d = mapData[i];
                    mv = new MusouWorldMapPlayerVO();
                    mv.readSaveObj(d);
                    _mapData.Add(mv);
                }
            }

            List<object> fighterData = Get(o, "fighterData") != null ? ToArray(Get(o, "fighterData")) : null;
            if (fighterData != null)
            {
                _fighterData = new List<MusouFighterVO>();
                for (i = 0; i < fighterData.Count; i++)
                {
                    d = fighterData[i];
                    fv = new MusouFighterVO();
                    fv.readSaveObj(d);
                    _fighterData.Add(fv);
                }
            }

            if (Bool(Get(o, "currentMapId")))
            {
                _currentMapId = Str(Get(o, "currentMapId"));
            }

            if (Bool(Get(o, "currentAreaId")))
            {
                _currentAreaId = Str(Get(o, "currentAreaId"));
            }

            List<object> fighterTeam = Get(o, "fighterTeam") != null ? ToArray(Get(o, "fighterTeam")) : null;
            if (fighterTeam != null)
            {
                _fighterTeam = new List<MusouFighterVO>();
                for (i = 0; i < fighterTeam.Count; i++)
                {
                    string id = Str(fighterTeam[i]);
                    fv = getFighterDataById(id);
                    if (fv != null)
                    {
                        _fighterTeam.Add(fv);
                    }
                }
            }
        }

        private void initMap()
        {
            TraceLang("debug.trace.data.musou_player_data.init_map");

            MusouWorldMapPlayerVO map = new MusouWorldMapPlayerVO();
            map.id = _currentMapId;
            _mapData.Add(map);

            MusouWorldMapVO map2 = MusouModel.I.getMap(map.id);
            foreach (var m in map2.areas)
            {
                if (m.preOpens == null || m.preOpens.Count < 1)
                {
                    MusouLogic.I.openMapArea(map.id, m.id);
                }
            }
        }
    }
}
