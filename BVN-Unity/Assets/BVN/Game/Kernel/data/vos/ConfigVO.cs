using System;
using System.Collections.Generic;
using System.Reflection;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class ConfigVO : ISaveData, IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public readonly KeyConfigVO key_menu = new KeyConfigVO(0);
        public readonly KeyConfigVO key_p1 = new KeyConfigVO(1);
        public readonly KeyConfigVO key_p2 = new KeyConfigVO(2);

        public ConfigVO()
        {
            if (GameInterface.instance != null)
            {
                extendConfig = GameInterface.instance.getConfigExtend();
            }

            setDefaultConfig(key_menu);
            setDefaultConfig(key_p1);
            setDefaultConfig(key_p2);
        }

        public SelectStageConfigVO select_config = new SelectStageConfigVO();

        // 显示语言
        public string language = null;

        public int AI_level = 1;
        public double fighterHP = 1; //HP比例
        public int fightTime = 60;
        public string quality = StageQuality.LOW;
        public double soundVolume = 0.7; // SOUND音量
        public double bgmVolume = 0.7; // BGM音量
        public int keyInputMode = 1; //0标准, 1经典（长按式）

        // 是否显示血量
        public bool isShowHp = false;

        public IExtendConfig extendConfig;

        public virtual void setDefaultConfig(KeyConfigVO keyConfig)
        {
            switch (keyConfig.id)
            {
                case 0: //MENU
                    keyConfig.setKeys(
                        Keyboard.W, Keyboard.S, Keyboard.A, Keyboard.D,
                        Keyboard.J, Keyboard.K, Keyboard.L,
                        Keyboard.U, Keyboard.I, Keyboard.O
                    );
                    keyConfig.selects = new List<object> { Keyboard.J, Keyboard.K, Keyboard.L, Keyboard.U, Keyboard.I, Keyboard.O };
                    break;
                case 1:
                    keyConfig.setKeys(
                        Keyboard.W, Keyboard.S, Keyboard.A, Keyboard.D,
                        Keyboard.J, Keyboard.K, Keyboard.L,
                        Keyboard.U, Keyboard.I, Keyboard.O
                    );
                    break;
                case 2:
                    keyConfig.setKeys(
                        Keyboard.UP, Keyboard.DOWN, Keyboard.LEFT, Keyboard.RIGHT,
                        Keyboard.NUMPAD_1, Keyboard.NUMPAD_2, Keyboard.NUMPAD_3,
                        Keyboard.NUMPAD_4, Keyboard.NUMPAD_5, Keyboard.NUMPAD_6
                    );
                    break;
            }
        }

        public virtual object toSaveObj()
        {
            ASObject o = new ASObject();
            o["key_p1"] = key_p1.toSaveObj();
            o["key_p2"] = key_p2.toSaveObj();

            o["language"] = language;
            o["AI_level"] = AI_level;
            o["fighterHP"] = fighterHP;
            o["fightTime"] = fightTime;
            o["quality"] = quality;
            o["keyInputMode"] = keyInputMode;
            o["soundVolume"] = soundVolume;
            o["bgmVolume"] = bgmVolume;
            o["isShowHp"] = isShowHp;

            if (extendConfig != null)
            {
                o["extend_config"] = extendConfig.toSaveObj();
            }

            return o;
        }

        public virtual void readSaveObj(object o)
        {
            key_p1.readSaveObj(Get(o, "key_p1"));
            key_p2.readSaveObj(Get(o, "key_p2"));

            if (Bool(Get(o, "extend_config")) && extendConfig != null)
            {
                extendConfig.readSaveObj(Get(o, "extend_config"));
            }

            if (o is IDictionary<string, object> d)
            {
                d.Remove("key_p1");
                d.Remove("key_p2");
            }

            KyoUtils.setValueByObject(this, o);
        }

        /// <summary>AS3 this.hasOwnProperty(key) for declared instance fields/properties.</summary>
        private bool hasOwnProperty(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public;
            return GetType().GetField(key, f) != null || GetType().GetProperty(key, f) != null;
        }

        public virtual object getValueByKey(string key)
        {
            if (hasOwnProperty(key))
            {
                return Get(this, key);
            }

            if (extendConfig != null)
            {
                try
                {
                    return Get(extendConfig, key);
                }
                catch (Exception e)
                {
                    trace(e);
                }
            }

            return null;
        }

        public virtual void setValueByKey(string key, object value)
        {
            if (hasOwnProperty(key))
            {
                Set(this, key, value);

                switch (key)
                {
                    case "bgmVolume":
                        SoundCtrl.I.setBgmVolumn(bgmVolume);
                        break;
                    case "soundVolume":
                        SoundCtrl.I.setSoundVolumn(soundVolume);
                        break;
                }

                return;
            }

            if (extendConfig != null)
            {
                try
                {
                    Set(extendConfig, key, value);
                }
                catch (Exception e)
                {
                    trace(e);
                }
            }
        }

        public virtual void applyConfig()
        {
            switch (quality)
            {
                case GameQuality.LOW:
                    GameConfig.QUALITY_GAME = StageQuality.LOW;
                    GameConfig.setGameFps(30);
                    GameConfig.FPS_SHINE_EFFECT = 15;
                    EffectCtrl.EFFECT_SMOOTHING = false;
                    break;
                case GameQuality.MEDIUM:
                    GameConfig.QUALITY_GAME = StageQuality.LOW;
                    GameConfig.setGameFps(60);
                    GameConfig.FPS_SHINE_EFFECT = 30;
                    EffectCtrl.EFFECT_SMOOTHING = false;
                    break;
                case GameQuality.HIGH:
                    GameConfig.QUALITY_GAME = StageQuality.MEDIUM;
                    GameConfig.setGameFps(60);
                    GameConfig.FPS_SHINE_EFFECT = 30;
                    EffectCtrl.EFFECT_SMOOTHING = false;
                    break;
                case GameQuality.HIGHER:
                    GameConfig.QUALITY_GAME = StageQuality.HIGH;
                    GameConfig.setGameFps(60);
                    GameConfig.FPS_SHINE_EFFECT = 30;
                    EffectCtrl.EFFECT_SMOOTHING = true;
                    break;
                case GameQuality.BEST:
                    GameConfig.QUALITY_GAME = StageQuality.HIGH;
                    GameConfig.setGameFps(60);
                    GameConfig.FPS_SHINE_EFFECT = 60;
                    EffectCtrl.EFFECT_SMOOTHING = true;
                    break;
            }

            GameInterface.instance.applyConfig(this);

            SoundCtrl.I.setBgmVolumn(bgmVolume);
            SoundCtrl.I.setSoundVolumn(soundVolume);
        }
    }
}
