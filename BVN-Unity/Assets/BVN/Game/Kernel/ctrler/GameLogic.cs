using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameLogic
    {
        private static MapMain _map;
        private static GameCamera _camera;
        private static Dictionary<object, FloorVO> _floorContact = new Dictionary<object, FloorVO>();
        // AS3 Object keyed by id (keys are stringified like AS3 property names)
        private static Dictionary<string, ASObject> _hitsObj = new Dictionary<string, ASObject>();

        private static string hitKey(object id) => id == null ? "null" : Ops.ToStr(id);

        public static void initGameLogic(MapMain map, GameCamera camera)
        {
            _map = map;
            _camera = camera;
        }

        public static void clear()
        {
            _map = null;
            _camera = null;
        }

        public static bool isInAir(BaseGameSprite target)
        {
            if (_map == null)
            {
                TraceLang("debug.trace.data.game_logic.map_null");
                return false;
            }

            if (target.getVecY() < 0)
            {
                target.isTouchBottom = false;
                return true;
            }

            if (target.y > _map.playerBottom - GameConfig.G)
            {
                target.y = _map.playerBottom;
                target.isTouchBottom = true;
                return false;
            }

            target.isTouchBottom = false;

            bool allowHitAirFloor = target.getVecY() < GameConfig.NO_TOUCH_BAN_ON_VECY * GameConfig.SPEED_PLUS;

            if (!allowHitAirFloor) return true;

            double floorSpd = 10 * GameConfig.SPEED_PLUS_DEFAULT;

            _floorContact.TryGetValue(target, out var fv);
            if (fv != null)
            {
                if (fv.hitTest(target.x, target.y, floorSpd))
                {
                    target.y = fv.y;
                    return false;
                }
                else
                {
                    _floorContact.Remove(target);
                    return true;
                }
            }

            fv = _map.getFloorHitTest(target.x, target.y, floorSpd);
            if (fv != null)
            {
                target.y = fv.y;
                _floorContact[target] = fv;
                return false;
            }

            return true;
        }

        public static bool isTouchBottomFloor(IGameSprite target)
        {
            if (_map == null)
            {
                TraceLang("debug.trace.data.game_logic.map_null");
                return false;
            }

            return target.y > _map.playerBottom;
        }

        public static bool isOutRange(IGameSprite target)
        {
            if (_map == null)
            {
                TraceLang("debug.trace.data.game_logic.map_null");
                return false;
            }

            int offset = 20;

            return target.x > _map.right + offset || target.x < _map.left - offset || target.y > _map.bottom + offset;
        }

        public static int addHits(object id, object targetID, int uiID)
        {
            string k = hitKey(id);
            if (!_hitsObj.TryGetValue(k, out var o) || o == null)
            {
                o = new ASObject { ["hits"] = 0, ["targetID"] = targetID, ["uiID"] = uiID };
                _hitsObj[k] = o;
            }
            o["targetID"] = targetID;
            o["hits"] = Num(o["hits"]) + 1;
            return Int(o["hits"]);
        }

        public static void clearHits(object id)
        {
            // AS3 would throw on an unknown id; guarded here
            if (!_hitsObj.TryGetValue(hitKey(id), out var o) || o == null) return;
            o["hits"] = 0;
            o["targetID"] = null;
            o["uiID"] = null;
        }

        public static object getHitsObj(object id)
        {
            _hitsObj.TryGetValue(hitKey(id), out var o);
            return o;
        }

        public static object getHitsObjByTargetId(object id)
        {
            foreach (var o in _hitsObj.Values)
            {
                if (o != null && Ops.LooseEquals(o["targetID"], id)) return o;
            }
            return null;
        }

        public static void clearHitsByTargetId(object id)
        {
            foreach (var k in new List<string>(_hitsObj.Keys))
            {
                if (Ops.LooseEquals(_hitsObj[k]?["targetID"], id)) _hitsObj.Remove(k);
            }
        }

        public static bool checkFighterDie(FighterMain v)
        {
            if (GameMode.currentMode == GameMode.TRAINING) return false;
            return v.hp <= 0;
        }

        public static void hitTarget(HitVO hitvo, IGameSprite attacker, IGameSprite target)
        {
            if (attacker == null || target == null) return;
            if (!(attacker is FighterMain)) return;
            if (!(target is FighterMain)) return;

            var tf = (FighterMain)target;
            if (tf.actionState != FighterActionState.HURT_ING && tf.actionState != FighterActionState.HURT_FLYING) return;

            var param = new ASObject();
            param["target"] = target;
            param["hitvo"] = hitvo;
            FighterEventDispatcher.dispatchEvent(attacker as FighterMain, FighterEvent.HIT_TARGET, param);
        }

        public static void addScoreByHitTarget(HitVO hitvo)
        {
            int baseScore = (int)hitvo.power;
            int score = baseScore;

            if (hitvo.isBisha()) score = baseScore * 2;

            if (hitvo.id == "sh1" || hitvo.id == "sh2") score += 500;

            if (hitvo.isBreakDef) score += 200;
            if (hitvo.hurtType == 1) score += 200;

            object hitsObj = getHitsObj(1);
            int hits = hitsObj != null ? Int(Get(hitsObj, "hits")) : 0;

            if (hits < 4) score += hits * 50;
            else score += hits * 100;

            addScore(score);

            GameEvent.dispatchEvent(GameEvent.SCORE_UPDATE);
        }

        /// <summary>添加分数 - 结算 KO 事件</summary>
        public static void addScoreByKO()
        {
            if (P1.lastHitVO != null && P1.lastHitVO.isBisha()) addScore(2000);

            if (P1.hp == P1.hpMax) addScore(20000);
            else addScore(3000);

            GameEvent.dispatchEvent(GameEvent.SCORE_UPDATE);
        }

        /// <summary>添加分数 - 结算通过一小关事件</summary>
        public static void addScoreByPassMission()
        {
            if (GameMode.currentMode == GameMode.TEAM_ARCADE &&
                P1.data == GameCtrl.I.gameRunData.p1FighterGroup.fighter1)
            {
                addScore(15000);
            }
            else
            {
                addScore(5000);
            }
        }

        public static void loseScoreByContinue()
        {
            GameData.I.score -= 10000;
            if (GameData.I.score < 0) GameData.I.score = 0;
        }

        /// <summary>防止游戏元件超出地图、当前显示区域</summary>
        public static void fixGameSpritePosition(IGameSprite sp)
        {
            if (!sp.allowCrossMapXY())
            {
                double left = _map.left + GameConfig.X_SIDE_OFFSET;
                double right = _map.right - GameConfig.X_SIDE_OFFSET;
                double offsetX = GameConfig.X_SIDE_OFFSET;

                double camzoom = _camera.getZoom();
                Rectangle camRect = _camera.getScreenRect();

                if (sp is FighterMain fm)
                {
                    if (fm.getVecX() != 0)
                    {
                        if (camzoom == _camera.autoZoomMin)
                        {
                            double camLeft = camRect.x / camzoom + offsetX;
                            double camRight = camLeft + camRect.width - offsetX;

                            if (left < camLeft) left = camLeft;
                            if (right > camRight) right = camRight;
                        }
                    }
                }

                bool isTouchSide = false;

                if (sp.x <= left)
                {
                    sp.x = left;
                    isTouchSide = true;
                }
                if (sp.x >= right)
                {
                    sp.x = right;
                    isTouchSide = true;
                }

                sp.setIsTouchSide(isTouchSide);
            }

            if (!sp.allowCrossMapBottom())
            {
                if (sp.y > _map.bottom) sp.y = _map.bottom;
            }
        }

        public static void resetFighterHP(FighterMain v)
        {
            double messionRate = 1;
            if (GameMode.isArcade() && MessionModel.I.getCurrentMessionStage() != null)
                messionRate = MessionModel.I.getCurrentMessionStage().hpRate;

            if (v.customHpMax > 0)
                v.hp = v.hpMax = v.customHpMax * GameData.I.config.fighterHP * messionRate;
            else
                v.hp = v.hpMax = GameConfig.FIGHTER_HP_MAX * GameData.I.config.fighterHP * messionRate;

            v.isAlive = true;
        }

        public static void setMessionEnemyAttack(FighterMain v)
        {
            MessionStageVO mv = MessionModel.I.getCurrentMessionStage();
            if (mv != null) v.attackRate = mv.attackRate;
        }

        public static bool canSelectFighter(string id)
        {
            SelectCharListConfigVO charList = GameData.I.config.select_config.charList;
            foreach (var o in charList.list)
            {
                var c = o as SelectCharListItemVO;
                if (c != null && c.fighterID == id) return true;
            }
            return false;
        }

        public static bool canSelectAssist(string id)
        {
            SelectCharListConfigVO assistList = GameData.I.config.select_config.assistList;
            foreach (var o in assistList.list)
            {
                var c = o as SelectCharListItemVO;
                if (c != null && c.fighterID == id) return true;
            }
            return false;
        }

        public static void setGameMode(int v)
        {
            TraceLang("debug.trace.data.game_logic.set_game_mode", new ASObject { ["mode"] = v });
            switch (v)
            {
                case 1:
                    GameConfig.SHOW_UI_STATUS = 1;
                    GameEndCtrl.SHOW_CONTINUE = true;
                    GameConfig.MAP_LOGO_STATE = MapLogoState.SHOW_MINE;
                    break;
                case 2:
                    GameConfig.SHOW_UI_STATUS = 1;
                    GameConfig.MAP_LOGO_STATE = MapLogoState.SHOW_MINE;
                    GameEndCtrl.SHOW_CONTINUE = true;
                    break;
                default:
                    GameData.I.loadSelect("config/select.json");
                    ResUtils.WINNER = "$loading$MC_stageWinner";
                    GameConfig.SHOW_UI_STATUS = 0;
                    GameConfig.MAP_LOGO_STATE = MapLogoState.SHOW_4399;
                    GameEndCtrl.SHOW_CONTINUE = false;
                    break;
            }
        }

        /// <summary>添加分数</summary>
        private static void addScore(int score)
        {
            double rate = GameData.I.config.keyInputMode == 0 ? 1 : 0.8;
            GameData.I.score = (int)(GameData.I.score + score * rate);
        }
    }
}
