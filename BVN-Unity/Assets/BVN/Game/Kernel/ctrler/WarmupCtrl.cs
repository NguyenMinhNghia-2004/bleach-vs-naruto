using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 启动与交互前的资源预热，消除首次悬停 / 点击时的卡顿。
    /// warmEarly 在语言页可用；warmBasic 在 AssetManager.loadBasic 完成后执行。
    /// </summary>
    public class WarmupCtrl
    {
        private static WarmupCtrl _i;

        public static WarmupCtrl I
        {
            get
            {
                _i ??= new WarmupCtrl();
                return _i;
            }
        }

        private bool _earlyDone;
        private bool _basicDone;
        private Sprite _probe;

        /// <summary>早期预热（语言页阶段）：TweenLite 引擎与菜单选择 / 确认音。幂等。</summary>
        public virtual void warmEarly()
        {
            if (_earlyDone) return;
            _earlyDone = true;

            warmTweenEngine();
            SoundCtrl.I.warmMenuSounds();
        }

        /// <summary>预热语言列表项的选中条光栅化。</summary>
        public virtual void warmCountryItems(List<object> items)
        {
            if (items == null) return;
            foreach (var o in items)
            {
                var country = o as CountryItem;
                if (country != null) country.warmUp();
            }
        }

        /// <summary>基础资源加载完成后的完整预热。幂等。</summary>
        public virtual void warmBasic()
        {
            if (_basicDone) return;
            _basicDone = true;

            // 若未走过语言页，补齐引擎与菜单音
            warmEarly();

            warmTweenPlugins();
            SoundCtrl.I.warmCommonSwcSounds();
            warmBitmapFont();
            MenuBtn.warmCommon();
            SetBtnLine.warmCommon();
            warmUiScaleProbes();
            warmBgmPlayer();
            DialogManager.warmUp();
            warmBitmapDataPool();
            warmFilters();
        }

        private void warmTweenEngine()
        {
            Sprite probe = getProbe();
            TweenLite.killTweensOf(probe);
            TweenLite.to(probe, 0, new ASObject { ["alpha"] = probe.alpha });
            // 非零时长 scale 缓动，覆盖悬停真实路径
            probe.scaleX = 0;
            TweenLite.to(probe, 0.01, new ASObject { ["scaleX"] = 1 });
            probe.scaleX = 1;
        }

        private void warmTweenPlugins()
        {
            Sprite probe = getProbe();
            TweenLite.killTweensOf(probe);
            TweenLite.to(probe, 0, new ASObject { ["scaleX"] = 1, ["ease"] = Back.easeOut });
            TweenLite.to(probe, 0, new ASObject { ["alpha"] = 1, ["ease"] = Elastic.easeOut });
        }

        private void warmBitmapFont()
        {
            BitmapFont font = AssetManager.I.getFont("font1");
            if (font == null) return;
            var txt = new BitmapFontText(font);
            txt.text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var w = txt.width;
            txt.dispose();
        }

        private void warmUiScaleProbes()
        {
            Sprite probe = getProbe();
            TweenLite.killTweensOf(probe);

            // MenuBtn.hover / MenuBtnGroup.fadIn
            probe.scaleX = 0.01;
            TweenLite.to(probe, 0, new ASObject { ["scaleX"] = 1, ["ease"] = Back.easeOut });

            // SetBtnLine.show
            probe.scaleX = 0.1;
            TweenLite.to(probe, 0, new ASObject { ["scaleX"] = 1 });

            // MenuBtn.select Elastic
            probe.alpha = 0;
            TweenLite.to(probe, 0, new ASObject { ["alpha"] = 1, ["ease"] = Elastic.easeOut });

            probe.scaleX = 1;
            probe.alpha = 1;
        }

        private void warmBgmPlayer()
        {
            try
            {
                double vol = GameData.I != null && GameData.I.config != null ? GameData.I.config.bgmVolume : 0.7;
                SoundCtrl.I.setBgmVolumn(vol);
            }
            catch (Exception)
            {
            }
        }

        private void warmBitmapDataPool()
        {
            BitmapData bd = BitmapDataPool.I.acquire(64, 64, true, 0);
            if (bd != null) BitmapDataPool.I.release(bd);
            bd = BitmapDataPool.I.acquire(128, 128, true, 0);
            if (bd != null) BitmapDataPool.I.release(bd);
        }

        private void warmFilters()
        {
            new GlowFilter(0xffffff, 1, 2, 2, 2);
            new DropShadowFilter(2, 45, 0, 0.5, 2, 2);
        }

        private Sprite getProbe()
        {
            _probe ??= new Sprite();
            return _probe;
        }
    }
}
