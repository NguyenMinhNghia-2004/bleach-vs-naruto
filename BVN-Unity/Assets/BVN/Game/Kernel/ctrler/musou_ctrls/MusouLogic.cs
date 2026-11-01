using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouLogic
    {
        private static MusouLogic _i;

        public static MusouLogic I
        {
            get
            {
                _i ??= new MusouLogic();
                return _i;
            }
        }

        private int _hitNum;
        private List<FighterMain> _hitTargets = new List<FighterMain>();

        public virtual bool checkCurrentArea(string areaId)
        {
            MusouPlayerData md = GameData.I.musouData;
            if (md.getCurrentArea() == null) return false;

            return md.getCurrentArea().id == areaId;
        }

        public virtual bool checkAreaIsOpen(string areaId)
        {
            MusouWorldMapPlayerVO map = GameData.I.musouData.getCurrentMap();
            return map.getOpenArea(areaId) != null;
        }

        public virtual MusouMissionVO getNextMission(MusouWorldMapAreaVO area2)
        {
            MusouWorldMapPlayerVO map = GameData.I.musouData.getCurrentMap();
            MusouWorldMapAreaPlayerVO area = map.getOpenArea(area2.id);

            if (area == null) return null;

            MusouMissionPlayerVO m1 = area.getLastPassedMission();

            if (m1 == null) return area2.missions[0];

            MusouMissionVO m2 = area2.getNextMission(m1.id);
            if (m2 != null) return m2;
            return area2.missions[area2.missions.Count - 1];
        }

        public virtual double getAreaPercent(string areaId)
        {
            MusouWorldMapPlayerVO map = GameData.I.musouData.getCurrentMap();
            MusouWorldMapAreaPlayerVO area = map.getOpenArea(areaId);
            if (area == null) return 0;

            MusouWorldMapAreaVO area2 = MusouModel.I.getMapArea(map.id, area.id);
            if (area2 == null || area2.missions == null) return 0;

            double per = (double)area.getPassedMissionAmount() / area2.missions.Count;
            return per;
        }

        public virtual void openMapArea(string mapId, string areaId)
        {
            TraceLang("debug.trace.data.musou_logic.open_map_area", new ASObject { ["areaId"] = areaId });

            MusouWorldMapPlayerVO map = GameData.I.musouData.getMapById(mapId);
            map.openArea(areaId);
        }

        public virtual void passMission(MusouMissionVO mission)
        {
            MusouWorldMapAreaPlayerVO area = GameData.I.musouData.getCurrentArea();

            if (area.passMission(mission.id))
            {
                GameData.I.musouData.addMoney(2000);
            }
            else
            {
                GameData.I.musouData.addMoney(500);
            }

            updateMapAreas();
            GameData.I.saveData();
        }

        public virtual void updateMapAreas()
        {
            MusouWorldMapPlayerVO pmap = GameData.I.musouData.getCurrentMap();

            MusouWorldMapVO map = MusouModel.I.getMap(pmap.id);
            foreach (var i in map.areas)
            {
                if (i.preOpens != null && i.preOpens.Count > 0)
                {
                    bool isOpenArea = true;

                    foreach (var p in i.preOpens)
                    {
                        if (!canPassNextArea(p.id))
                        {
                            isOpenArea = false;
                            break;
                        }
                    }

                    if (isOpenArea)
                    {
                        openMapArea(pmap.id, i.id);
                    }
                }
            }
        }

        public virtual void buyFighter(MusouFighterSellVO data, Delegate succback = null)
        {
            MusouPlayerData musouData = GameData.I.musouData;
            if (musouData.getMoney() < data.getPrice())
            {
                GameUI.alert(
                    GetLang("alert.musou_ctrl.need_more_money_title"),
                    GetLang("alert.musou_ctrl.need_more_money", new ASObject { ["amount"] = data.getPrice() })
                );
                return;
            }

            GameUI.confrim(
                GetLang("confirm.musou_ctrl.unlock_fighter_title"),
                GetLang("confirm.musou_ctrl.unlock_fighter", new ASObject { ["amount"] = data.getPrice() }),
                (Action)(() =>
                {
                    musouData.loseMoney(data.getPrice());
                    musouData.addFighter(data.id);
                    GameData.I.saveData();
                    if (succback != null) Call(succback);
                }),
                null,
                true
            );
        }

        /// <summary>敌人等级属性</summary>
        public virtual void initEnemyProps(FighterMain f)
        {
            if (f.musouEnemyData == null) return;

            MusouMissionVO mv = MusouModel.I.currentMission;
            int lv = mv.enemyLevel;

            f.musouEnemyData.level = lv;

            double hpRate;
            int atk, skl, bs;

            if (f.musouEnemyData.isBoss)
            {
                if (f.musouEnemyData.maxHp > 0)
                {
                    f.hp = f.hpMax = f.musouEnemyData.maxHp;
                }
                else
                {
                    f.hp = f.hpMax = 1000 + (lv * 400);
                }

                f.energy = f.energyMax = 80 + (lv * 10);

                atk = lv * 13;
                skl = lv * 14;
                bs = lv * 15;

                f.initAttackAddDmg(atk, skl, bs);
            }
            else
            {
                if (f.musouEnemyData.maxHp > 0)
                {
                    f.hp = f.hpMax = f.musouEnemyData.maxHp;
                }
                else
                {
                    hpRate = 1 + (lv - 1) * 0.08;
                    if (hpRate > 1)
                    {
                        f.hp = f.hpMax = f.hp * hpRate;
                    }
                }

                atk = lv * 5;
                skl = lv * 10;
                bs = lv * 20;

                f.initAttackAddDmg(atk, skl, bs);
            }
        }

        public virtual int addHits(FighterMain target)
        {
            if (_hitTargets.IndexOf(target) == -1)
            {
                _hitTargets.Add(target);
            }
            _hitNum++;

            if (_hitNum > 1 && GameUI.I.getUI() != null)
            {
                GameUI.I.getUI().showHits(_hitNum, 1);
            }

            return _hitNum;
        }

        public virtual void removeHitTarget(FighterMain target)
        {
            int index = _hitTargets.IndexOf(target);
            if (index == -1) return;

            _hitTargets.RemoveAt(index);

            if (_hitTargets.Count < 1)
            {
                _hitNum = 0;
                if (GameUI.I != null && GameUI.I.getUI() != null)
                {
                    GameUI.I.getUI().hideHits(1);
                }
            }
        }

        public virtual void clearHits()
        {
            _hitNum = 0;
            _hitTargets = new List<FighterMain>();
        }

        private bool canPassNextArea(string areaId)
        {
            return getAreaPercent(areaId) > 0.6;
        }
    }
}
