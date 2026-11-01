using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SoundCtrl
    {
        private static SoundCtrl _i;

        public static SoundCtrl I
        {
            get
            {
                _i ??= new SoundCtrl();
                return _i;
            }
        }

        /// <summary>SWC library holding the snd_* linkage classes (converted swc/sound).</summary>
        public const string SWC_SOUND_SWF = "swc/sound.swf";

        /// <summary>
        /// AS3 top-level SWC sound class (snd_menu1, snd_ko ...) as a LinkedClass from the converted swc/sound library
        /// (falls back to any loaded library).
        /// </summary>
        public static object GetSwcSoundClass(string className)
        {
            return (object)AssetBridge.GetClass(className, SWC_SOUND_SWF) ?? AssetBridge.GetClass(className);
        }

        private static object snd_menu1 => GetSwcSoundClass("snd_menu1");
        private static object snd_menu2 => GetSwcSoundClass("snd_menu2");
        private static object snd_menu5 => GetSwcSoundClass("snd_menu5");
        private static object snd_gameover => GetSwcSoundClass("snd_gameover");
        private static object snd_over_hit => GetSwcSoundClass("snd_over_hit");
        private static object snd_ko => GetSwcSoundClass("snd_ko");
        private static object snd_ko_bs => GetSwcSoundClass("snd_ko_bs");
        private static object snd_ghost_jump => GetSwcSoundClass("snd_ghost_jump");
        private static object snd_ghost_jump2 => GetSwcSoundClass("snd_ghost_jump2");
        private static object snd_ghost_dash => GetSwcSoundClass("snd_ghost_dash");

        private KyoBGSounder _bgSound;
        private KyoSoundLoader _soundLoader;
        private Dictionary<string, object> _bgmObj;
        private bool _bgmPaused = false;
        private object _waitingSound;
        private SoundTransform _sndTransform = new SoundTransform();
        private int _lastSndTime;
        /// <summary>SWC 声音类 → 复用实例，避免每次 new</summary>
        private Dictionary<object, Sound> _swcSoundCache;
        /// <summary>静音预热用</summary>
        private SoundTransform _silentTransform;

        public virtual void setSoundVolumn(double v)
        {
            _sndTransform.volume = v;
        }

        public virtual void setBgmVolumn(double v)
        {
            _bgSound ??= new KyoBGSounder();
            _bgSound.volume = v;
        }

        public virtual void playAssetSound(string name, double vol = 1)
        {
            if (name == null) return;

            Sound sound = AssetManager.I.getSound(name);
            if (sound != null)
            {
                SoundTransform st = _sndTransform;
                if (vol != 1) st = new SoundTransform(vol * _sndTransform.volume);

                sound.play(0, 0, st);
                _lastSndTime = getTimer();
            }
        }

        public virtual void playEffectSound(string name, double vol = 1)
        {
            if (name == null) return;
            if (keepSoundNoise()) return;

            Sound sound = AssetManager.I.getEffect(name) as Sound;
            if (sound != null)
            {
                SoundTransform st = _sndTransform;
                if (vol != 1) st = new SoundTransform(vol * _sndTransform.volume);
                sound.play(0, 0, st);
                _lastSndTime = getTimer();
            }
        }

        public virtual void playAssetSoundRandom(params object[] @params)
        {
            if (keepSoundNoise()) return;
            string name = Str(KyoRandom.getRandomInArray(new List<object>(@params)));
            playAssetSound(name);
        }

        /// <summary>播放声音（类链接）</summary>
        public virtual void playSwcSound(object sndCls)
        {
            if (sndCls == null) return;

            playSound(getCachedSwcSound(sndCls));
        }

        /// <summary>播放声音</summary>
        public virtual void playSound(Sound snd)
        {
            if (snd == null) return;

            if (keepSoundNoise()) return;

            snd.play(0, 0, _sndTransform);
            _lastSndTime = getTimer();
        }

        public virtual void BGM(object sound, bool loop = true)
        {
            if (_bgmPaused)
            {
                _waitingSound = sound;
                return;
            }

            _bgSound ??= new KyoBGSounder();

            if (_bgSound.sound == sound) return;
            if (_bgSound.playing) _bgSound.stop();

            if (sound != null) _bgSound.play(sound, loop);
        }

        public virtual void pauseBGM()
        {
            if (_bgmPaused) return;
            _bgmPaused = true;
            if (_bgSound != null) _bgSound.pause();
        }

        public virtual void resumeBGM()
        {
            if (!_bgmPaused) return;
            _bgmPaused = false;

            if (_waitingSound != null)
            {
                BGM(_waitingSound);
                _waitingSound = null;
                return;
            }

            if (_bgSound != null) _bgSound.resume();
        }

        /// <summary>
        /// 加载战斗BGM. arr: {id, url, rate(0-1)}；场景 BGM 的 id 为 'map'.
        /// </summary>
        public virtual void loadFightBGM(List<BgmVO> arr, Delegate success, Delegate fail = null, Delegate progress = null)
        {
            _bgmObj = new Dictionary<string, object>();
            var urls = new List<object>();
            foreach (var o in arr)
            {
                _bgmObj[Ops.ToStr(Get(o, "id"))] = o;
                urls.Add(Get(o, "url"));
            }

            _soundLoader = new KyoSoundLoader();
            string curUrl = null;
            int sndLen = urls.Count;

            void loadNext()
            {
                if (urls.Count < 1)
                {
                    if (success != null) Call(success);
                    return;
                }
                curUrl = Str(shift(urls));
                AssetManager.I.loadSound(curUrl, (Action<object>)loadBack, (Action)loadFail, (Action<double>)loadProgress);
            }

            void loadBack(object snd)
            {
                _soundLoader.addSound(curUrl, snd as Sound);
                loadNext();
            }

            void loadFail()
            {
                TraceLang("debug.trace.data.sound_ctrl.load_fight_bgm_fail", new ASObject { ["url"] = curUrl });
                loadNext();
            }

            void loadProgress(double v)
            {
                if (progress != null)
                {
                    double cur = sndLen - urls.Count - 1 + v;
                    double p = cur / sndLen;
                    Call(progress, v); // (sic) AS3 reports v, not p
                }
            }

            loadNext();
        }

        public virtual void playBossBGM(bool isNaruto)
        {
            playFighterBGM(isNaruto ? "boss_naruto" : "boss_bleach");
        }

        public virtual bool playFighterBGM(string id)
        {
            object o = null;
            if (_bgmObj == null || id == null || !_bgmObj.TryGetValue(id, out o) || o == null) return true;

            Sound sound = _soundLoader.getSound(Str(Get(o, "url")));
            BGM(sound);

            return false;
        }

        public virtual void smartPlayGameBGM(string id)
        {
            object o = null;
            if (_bgmObj != null && id != null) _bgmObj.TryGetValue(id, out o);

            if (id == "map")
            {
                if (o == null) return;
            }
            else
            {
                if (o == null)
                {
                    smartPlayGameBGM("map");
                    return;
                }
                if (random() > Num(Get(o, "rate")))
                {
                    smartPlayGameBGM("map");
                    return;
                }
            }

            Sound sound = _soundLoader.getSound(Str(Get(o, "url")));
            BGM(sound);
        }

        public virtual void unloadFightBGM()
        {
            BGM(null);
            if (_soundLoader != null)
            {
                _soundLoader.unload();
                _soundLoader = null;
            }
        }

        //选择音效
        public virtual void sndSelect()
        {
            playSwcSound(snd_menu1);
        }

        /// <summary>预实例化菜单选择 / 确认音（语言页早期可用）。</summary>
        public virtual void warmMenuSounds()
        {
            warmSwcSound(snd_menu1);
            warmSwcSound(snd_menu2);
        }

        /// <summary>预实例化常用 SWC 音效，避免首次 new Sound 卡顿。</summary>
        public virtual void warmCommonSwcSounds()
        {
            warmMenuSounds();
            warmSwcSound(snd_menu5);
            warmSwcSound(snd_gameover);
            warmSwcSound(snd_over_hit);
            warmSwcSound(snd_ko);
            warmSwcSound(snd_ko_bs);
            warmSwcSound(snd_ghost_jump);
            warmSwcSound(snd_ghost_jump2);
            warmSwcSound(snd_ghost_dash);
        }

        /// <summary>deprecated: use warmMenuSounds.</summary>
        public virtual void warmMenuSelectSound()
        {
            warmMenuSounds();
        }

        /// <summary>预实例化单个 SWC 声音类并做一次静音播放。</summary>
        public virtual void warmSwcSound(object sndCls)
        {
            if (sndCls == null) return;
            try
            {
                Sound snd = getCachedSwcSound(sndCls);
                if (snd == null) return;
                _silentTransform ??= new SoundTransform(0);
                SoundChannel ch = snd.play(0, 0, _silentTransform);
                if (ch != null) ch.stop();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>取得或创建并缓存 SWC 声音实例。</summary>
        private Sound getCachedSwcSound(object sndCls)
        {
            _swcSoundCache ??= new Dictionary<object, Sound>();
            // LinkedClass instances are created per lookup: key them by class name
            object key = sndCls is LinkedClass lc ? lc.ClassName : sndCls;
            _swcSoundCache.TryGetValue(key, out var snd);
            if (snd == null)
            {
                snd = sndCls as Sound ?? New(sndCls) as Sound;
                _swcSoundCache[key] = snd;
            }
            return snd;
        }

        //确定音效
        public virtual void sndConfrim()
        {
            playSwcSound(snd_menu2);
        }

        /// <summary>保持声音噪音</summary>
        private bool keepSoundNoise()
        {
            if (GameMode.currentMode != GameMode.MUSOU_ARCADE) return false;

            return getTimer() - _lastSndTime < 20;
        }
    }
}
