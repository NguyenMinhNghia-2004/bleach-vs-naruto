using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EffectModel
    {
        private static EffectModel _i;

        public static EffectModel I
        {
            get
            {
                _i ??= new EffectModel();
                return _i;
            }
        }

        //		private var _hitEffects:Object;
        private ASObject _effect;

        /// <summary>object literal helper: O("k1", v1, "k2", v2, ...)</summary>
        private static ASObject O(params object[] kv)
        {
            var o = new ASObject();
            for (int i = 0; i + 1 < kv.Length; i += 2) o[(string)kv[i]] = kv[i + 1];
            return o;
        }

        private static List<object> A(params object[] items) => new List<object>(items);

        public virtual void initialize()
        {
            initEffects();
            cacheEffects();
        }

        public virtual EffectVO getEffect(string id)
        {
            return id == null ? null : _effect[id] as EffectVO;
        }

        public virtual EffectVO getHitEffect(int type)
        {
            return _effect["hit_" + type] as EffectVO;
        }

        public virtual EffectVO getMusouEnemyHitEffect(int type)
        {
            return _effect["hit_enemy_" + type] as EffectVO;
        }

        public virtual EffectVO getMusouEnemyDefenseEffect(int hitType, int defenseType)
        {
            switch (defenseType)
            {
                case FighterDefenseType.SWORD:
                    break;
                case FighterDefenseType.HAND:
                    if (hitType == HitType.KAN)
                    {
                        hitType = HitType.DA;
                    }
                    if (hitType == HitType.KAN_HEAVY)
                    {
                        hitType = HitType.DA_HEAVY;
                    }
                    break;
            }
            return _effect["defense_enemy_" + hitType] as EffectVO;
        }

        public virtual EffectVO getDefenseEffect(int hitType, int defenseType)
        {
            switch (defenseType)
            {
                case FighterDefenseType.SWORD:
                    break;
                case FighterDefenseType.HAND:
                    if (hitType == HitType.KAN)
                    {
                        hitType = HitType.DA;
                    }
                    if (hitType == HitType.KAN_HEAVY)
                    {
                        hitType = HitType.DA_HEAVY;
                    }
                    break;
            }
            EffectVO effect = _effect["defense_" + hitType] as EffectVO;
            return effect != null ? effect : _effect["defense_" + HitType.MAGIC_HEAVY] as EffectVO;
        }

        private void initEffects()
        {
            _effect = new ASObject();

            _effect["bisha"] = new EffectVO("XG_bs", O("sound", "snd_bs", "blendMode", BlendModes.ADD));
            _effect["bisha_super"] = new EffectVO("XG_cbs", O("sound", "snd_cbs", "blendMode", BlendModes.ADD));

            _effect["dash"] = new EffectVO("XG_rush", O("sound", "snd_dash_air"));
            _effect["dash_air"] = new EffectVO("XG_rush_air", O("sound", "snd_dash_air"));

            _effect["jump"] = new EffectVO("XG_jump", O("sound", "snd_jump"));
            _effect["jump_air"] = new EffectVO("XG_jump_air", O("sound", "snd_jump", "blendMode", BlendModes.ADD));
            _effect["touch_floor"] = new EffectVO("XG_luodi", O("sound", "snd_luodi"));

            _effect["hit_floor"] = new EffectVO("XG_hitfloor", O("sound", "snd_hitfloor"));
            _effect["hit_floor_low"] = new EffectVO("xg_hitfloor_low", O("sound", "snd_hitfloor_low"));
            _effect["hit_floor_heavy"] = new EffectVO("xg_hitfllor_heavy", O("sound", "snd_hitfloor_heavy"));
            _effect["hit_floor_yan"] = new EffectVO("xg_hitfloor_yan", O("blendMode", BlendModes.LIGHTEN));

            _effect["hit_end"] = new EffectVO("xg_hitover");

            _effect["break_def"] = new EffectVO("XG_mfdjx", O(
                "sound", "snd_mfdjx", "blendMode", BlendModes.ADD, "slowDown", O("rate", 1.5, "time", 1500), "freeze", 500,
                "shake", O("x", 6, "time", 400)
            ));

            _effect["fz_bleach"] = new EffectVO("XG_fz_bleach_mc", O("sound", "snd_dash"));
            _effect["fz_naruto"] = new EffectVO("XG_fz_naruto_mc", O("sound", "snd_fz"));

            _effect["replaceSp"] = new EffectVO("XG_tishen", O("sound", "snd_fz"));
            _effect["replaceSp2"] = new EffectVO("XG_tishen2");
            _effect["explodeSp"] = new EffectVO("XG_bsq", O("sound", "snd_baoqi1"));
            _effect["explodeSp2"] = new EffectVO("XG_baoqi", O("sound", "snd_baoqi"));

            _effect["ghost_step"] = new EffectVO("XG_ghost_step", O("sound", "snd_ghost_step", "blendMode", BlendModes.ADD));

            _effect["kobg"] = new EffectVO("kobg_effect_mc");

            _effect["level_up"] = new EffectVO("xg_mc_levelup", O("sound", "snd_level_up"));
            _effect["level_up_new_act"] = new EffectVO("xg_mc_levelup_newaction", O("sound", "snd_level_up"));

            _effect["team_change"] = new EffectVO("xg_mc_change", O("sound", "snd_change1", "blendMode", BlendModes.ADD));

            initHitEffect();
            initDefenseEffect();
            initSpeicalEffect();
            initBuffEffect();
            initSteelHitEffect();

            initMusouEnemyHitEffect();
            initMusouEnemyDefenseEffect();
        }

        private void cacheEffects()
        {
            foreach (var o in _effect.Values)
            {
                if (o is EffectVO e)
                {
                    e.cacheBitmapData();
                }
            }
        }

        private void initHitEffect()
        {
            addHitEffect(HitType.CATCH, "xg_catch_hit", O("freeze", 400, "sound", "snd_hit_cache"));
            addHitEffect(HitType.KAN, "XG_kan2",
                O("sound", "snd_kan1", "freeze", 50, "blendMode", BlendModes.ADD, "randRotate", true));
            addHitEffect(HitType.KAN_HEAVY, "XG_kanx2", O(
                "sound", "snd_kan2", "freeze", 400, "blendMode", BlendModes.ADD, "shine", O("color", 0xffffff, "alpha", 0.2),
                "randRotate", true, "shake", O("pow", 6, "time", 400)));
            addHitEffect(HitType.DA, "XG_qdj", O("sound", "snd_hit2", "blendMode", BlendModes.ADD, "freeze", 50, "randRotate", true));
            addHitEffect(HitType.DA_HEAVY, "XG_qdjx", O(
                "sound", "snd_hit_heavy", "blendMode", BlendModes.ADD, "randRotate", true, "freeze", 400,
                "shine", O("color", 0xffffff, "alpha", 0.2), "shake", O("pow", 6, "time", 400)));
            addHitEffect(HitType.MAGIC, "XG_mfdj",
                O("sound", "snd_hit2", "freeze", 50, "blendMode", BlendModes.ADD, "randRotate", true));
            addHitEffect(HitType.MAGIC_HEAVY, "XG_mfdjx", O(
                "sound", "snd_mfdjx", "freeze", 400, "blendMode", BlendModes.ADD, "randRotate", true,
                "shine", O("color", 0xffffff, "alpha", 0.2), "shake", O("pow", 6, "time", 400)));
            addHitEffect(HitType.FIRE, "XG_fire", O(
                "sound", "snd_hit_fire", "blendMode", BlendModes.ADD, "specialEffectId", "fire_ing", "freeze", 400,
                "shine", O("color", 0xFFFF67, "alpha", 0.2), "shake", O("pow", 6, "time", 400)));
            addHitEffect(HitType.ICE, "XG_ice", O(
                "sound", "snd_hit_ice", "blendMode", BlendModes.ADD, "specialEffectId", "ice_ing", "freeze", 400,
                "shine", O("color", 0xA3E5F5, "alpha", 0.2), "shake", O("pow", 6, "time", 400)));
            addHitEffect(HitType.ELECTRIC, "XG_leidian", O(
                "sound", "snd_hit_dian", "blendMode", BlendModes.HARDLIGHT, "specialEffectId", "shock_ing", "freeze", 400,
                "shine", O("color", 0x8288D2, "alpha", 0.2), "shake", O("pow", 6, "time", 400)));
            //			addHitEffect(HitType.STONE , "XG_kanx" , {freeze:500});
        }

        private void initDefenseEffect()
        {
            addDefenseEffect(HitType.KAN, "XG_fykan",
                O("sound", "snd_fykan", "freeze", 50, "blendMode", BlendModes.ADD, "followDirect", true));
            addDefenseEffect(HitType.KAN_HEAVY, "XG_fykanx", O(
                "sound", "snd_fykanx", "freeze", 400, "blendMode", BlendModes.ADD,
                "shine", O("color", 0xffffff, "blendMode", BlendModes.SCREEN, "alpha", 0.1), "followDirect", true,
                "shake", O("pow", 6, "time", 400)));
            addDefenseEffect(HitType.DA, "XG_fyq",
                O("sound", "snd_def", "freeze", 50, "blendMode", BlendModes.ADD, "followDirect", true));
            addDefenseEffect(HitType.DA_HEAVY, "XG_fyqx", O(
                "sound", "snd_defx", "freeze", 400, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0xffffff, "alpha", 0.1), "shake", O("pow", 6, "time", 400)));
            addDefenseEffect(HitType.MAGIC, "XG_mffy",
                O("sound", "snd_def", "freeze", 50, "blendMode", BlendModes.ADD, "followDirect", true));
            addDefenseEffect(HitType.MAGIC_HEAVY, "XG_mffyx", O(
                "sound", "snd_defx", "freeze", 400, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0xffffff, "alpha", 0.15), "shake", O("pow", 6, "time", 400)));
            addDefenseEffect(HitType.FIRE, "XG_fire_fy", O(
                "sound", "snd_defx", "freeze", 400, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0xFFFF67, "alpha", 0.2), "shake", O("pow", 6, "time", 400)));
            addDefenseEffect(HitType.ICE, "XG_ice_fy", O(
                "sound", "snd_defx", "freeze", 400, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0xA3E5F5, "alpha", 0.2), "shake", O("pow", 6, "time", 400)));
            addDefenseEffect(HitType.ELECTRIC, "XG_dian_fy", O(
                "sound", "snd_defx", "freeze", 400, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0x8288D2, "alpha", 0.2), "shake", O("pow", 6, "time", 400)));
            //			addDefenseEffect(HitType.STONE , "XG_kanx" , {freeze:500});
        }

        private void initMusouEnemyHitEffect()
        {
            addEnemyHitEffect(HitType.CATCH, "xg_catch_hit", O("freeze", 200, "sound", "snd_hit_cache"));
            addEnemyHitEffect(HitType.KAN, "XG_kan2",
                O("sound", "snd_kan1", "freeze", 50, "blendMode", BlendModes.ADD, "randRotate", true));
            addEnemyHitEffect(HitType.KAN_HEAVY, "XG_kanx2", O(
                "sound", "snd_kan2", "freeze", 200, "blendMode", BlendModes.ADD, "shine", O("color", 0xffffff, "alpha", 0.2),
                "randRotate", true, "shake", O("pow", 2, "time", 100)));
            addEnemyHitEffect(HitType.DA, "XG_qdj",
                O("sound", "snd_hit2", "blendMode", BlendModes.ADD, "freeze", 0, "randRotate", true));
            addEnemyHitEffect(HitType.DA_HEAVY, "XG_qdjx", O(
                "sound", "snd_hit_heavy", "blendMode", BlendModes.ADD, "randRotate", true, "freeze", 200,
                "shine", O("color", 0xffffff, "alpha", 0.2), "shake", O("pow", 2, "time", 100)));
            addEnemyHitEffect(HitType.MAGIC, "XG_mfdj",
                O("sound", "snd_hit2", "freeze", 0, "blendMode", BlendModes.ADD, "randRotate", true));
            addEnemyHitEffect(HitType.MAGIC_HEAVY, "XG_mfdjx", O(
                "sound", "snd_mfdjx", "freeze", 200, "blendMode", BlendModes.ADD, "randRotate", true,
                "shine", O("color", 0xffffff, "alpha", 0.2), "shake", O("pow", 2, "time", 100)));
            addEnemyHitEffect(HitType.FIRE, "XG_fire", O(
                "sound", "snd_hit_fire", "blendMode", BlendModes.ADD, "specialEffectId", "fire_ing", "freeze", 200,
                "shine", O("color", 0xFFFF67, "alpha", 0.2), "shake", O("pow", 2, "time", 100)));
            addEnemyHitEffect(HitType.ICE, "XG_ice", O(
                "sound", "snd_hit_ice", "blendMode", BlendModes.ADD, "specialEffectId", "ice_ing", "freeze", 200,
                "shine", O("color", 0xA3E5F5, "alpha", 0.2), "shake", O("pow", 2, "time", 100)));
            addEnemyHitEffect(HitType.ELECTRIC, "XG_leidian", O(
                "sound", "snd_hit_dian", "blendMode", BlendModes.HARDLIGHT, "specialEffectId", "shock_ing", "freeze", 200,
                "shine", O("color", 0x8288D2, "alpha", 0.2), "shake", O("pow", 2, "time", 100)));
        }

        private void initMusouEnemyDefenseEffect()
        {
            addEnemyDefenseEffect(HitType.KAN, "XG_fykan",
                O("sound", "snd_fykan", "freeze", 50, "blendMode", BlendModes.ADD, "followDirect", true));
            addEnemyDefenseEffect(HitType.KAN_HEAVY, "XG_fykanx", O(
                "sound", "snd_fykanx", "freeze", 100, "blendMode", BlendModes.ADD,
                "shine", O("color", 0xffffff, "blendMode", BlendModes.SCREEN, "alpha", 0.1), "followDirect", true,
                "shake", O("pow", 2, "time", 100)));
            addEnemyDefenseEffect(HitType.DA, "XG_fyq",
                O("sound", "snd_def", "freeze", 50, "blendMode", BlendModes.ADD, "followDirect", true));
            addEnemyDefenseEffect(HitType.DA_HEAVY, "XG_fyqx", O(
                "sound", "snd_defx", "freeze", 100, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0xffffff, "alpha", 0.1), "shake", O("pow", 2, "time", 100)));
            addEnemyDefenseEffect(HitType.MAGIC, "XG_mffy",
                O("sound", "snd_def", "freeze", 50, "blendMode", BlendModes.ADD, "followDirect", true));
            addEnemyDefenseEffect(HitType.MAGIC_HEAVY, "XG_mffyx", O(
                "sound", "snd_defx", "freeze", 100, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0xffffff, "alpha", 0.15), "shake", O("pow", 2, "time", 100)));
            addEnemyDefenseEffect(HitType.FIRE, "XG_fire_fy", O(
                "sound", "snd_defx", "freeze", 100, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0xFFFF67, "alpha", 0.2), "shake", O("pow", 2, "time", 100)));
            addEnemyDefenseEffect(HitType.ICE, "XG_ice_fy", O(
                "sound", "snd_defx", "freeze", 100, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0xA3E5F5, "alpha", 0.2), "shake", O("pow", 2, "time", 100)));
            addEnemyDefenseEffect(HitType.ELECTRIC, "XG_dian_fy", O(
                "sound", "snd_defx", "freeze", 100, "blendMode", BlendModes.ADD, "followDirect", true,
                "shine", O("color", 0x8288D2, "alpha", 0.2), "shake", O("pow", 2, "time", 100)));
        }

        private void initSpeicalEffect()
        {
            _effect["fire_ing"] = new EffectVO("xg_fire_ing",
                O("blendMode", BlendModes.ADD, "isSpecial", true, "targetColorOffset", A(-255, -255, -255)));
            _effect["ice_ing"] = new EffectVO("xg_ice_ing",
                O("blendMode", BlendModes.ADD, "isSpecial", true, "targetColorOffset", A(0, 0, 255)));
            _effect["shock_ing"] = new EffectVO("xg_dian_ing",
                O("blendMode", BlendModes.ADD, "isSpecial", true, "targetColorOffset", A(50, -75, 255)));
        }

        private void initBuffEffect()
        {
            _effect["buff_effect_speed"] = new EffectVO("xg_buff_effect_speed", O("blendMode", BlendModes.ADD));
            _effect["buff_effect_power"] = new EffectVO("xg_buff_effect_power", O("blendMode", BlendModes.ADD));
            _effect["buff_effect_defense"] = new EffectVO("xg_buff_effect_defense", O("blendMode", BlendModes.ADD));

            _effect["buff_speed"] = new EffectVO("xg_buff_speed", O("blendMode", BlendModes.ADD, "isBuff", true));
            _effect["buff_power"] = new EffectVO("xg_buff_power", O("blendMode", BlendModes.ADD, "isBuff", true));
            _effect["buff_defense"] = new EffectVO("xg_buff_defense", O("blendMode", BlendModes.ADD, "isBuff", true));
        }

        private void initSteelHitEffect()
        {
            _effect["steel_hit_kan"] = new EffectVO("XG_kan2", O(
                "sound", "snd_hit11", "freeze", 400, "blendMode", BlendModes.ADD,
                "shine", O("color", 0xffffff, "alpha", 0.2), "randRotate", true, "isSteelHit", true));
            _effect["steel_hit_qdj"] = new EffectVO("XG_qdj", O(
                "sound", "snd_hit11", "freeze", 400, "blendMode", BlendModes.ADD,
                "shine", O("color", 0xffffff, "alpha", 0.2), "randRotate", true, "isSteelHit", true));
            _effect["steel_hit_mfdj"] = new EffectVO("XG_mfdj", O(
                "sound", "snd_hit11", "freeze", 400, "blendMode", BlendModes.ADD,
                "shine", O("color", 0xffffff, "alpha", 0.2), "randRotate", true, "isSteelHit", true));
        }

        private void addHitEffect(int type, string className, object param = null)
        {
            EffectVO effect = new EffectVO(className, param);
            _effect["hit_" + type] = effect;
        }

        private void addDefenseEffect(int type, string className, object param = null)
        {
            EffectVO effect = new EffectVO(className, param);
            _effect["defense_" + type] = effect;
        }

        private void addEnemyHitEffect(int type, string className, object param = null)
        {
            EffectVO effect = new EffectVO(className, param);
            _effect["hit_enemy_" + type] = effect;
        }

        private void addEnemyDefenseEffect(int type, string className, object param = null)
        {
            EffectVO effect = new EffectVO(className, param);
            _effect["defense_enemy_" + type] = effect;
        }
    }
}
