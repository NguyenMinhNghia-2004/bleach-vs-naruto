using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class TransUI
    {
        public MovieClip ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.common, "$common$MC_transition") as MovieClip;
        private int _renderAnimateGap = 0; //刷新动画间隔
        private int _renderAnimateFrame = 0;
        private Delegate _fadInBack;

        // private bool _justRender;
        private Delegate _fadOutBack;
        private bool _rendering = true;

        public virtual void destroy()
        {
            GameRender.remove((Action)render);
        }

        public virtual void fadIn(Delegate back = null)
        {
            _fadOutBack = null;

            _fadInBack = back;
            ui.gotoAndStop("fadin");
            startRender();
        }

        public virtual void fadOut(Delegate back = null)
        {
            _fadInBack = null;

            _fadOutBack = back;
            ui.gotoAndStop("fadout");
            startRender();
        }

        private void startRender()
        {
            _renderAnimateGap = (int)Math.Ceiling(MainGame.I.getFPS() / GameConfig.FPS_UI) - 1;
            _renderAnimateFrame = 0;

            _rendering = true;

            GameRender.add((Action)render);
        }

        private void stopRender()
        {
            _rendering = false;
            GameRender.remove((Action)render);
        }

        private void render()
        {
            if (!_rendering)
            {
                return;
            }

            if (_renderAnimateGap > 0)
            {
                if (_renderAnimateFrame++ >= _renderAnimateGap)
                {
                    _renderAnimateFrame = 0;
                    renderAnimate();
                }
            }
            else
            {
                renderAnimate();
            }
        }

        private void renderAnimate()
        {
            if (ui.currentFrameLabel == "stop")
            {
                //这里要处理在fadinback中定义了fadoutback的情况，所以只能这样写
                if (_fadInBack != null)
                {
                    Call(_fadInBack);
                    _fadInBack = null;
                    return;
                }
                if (_fadOutBack != null)
                {
                    Call(_fadOutBack);
                    _fadOutBack = null;
                    return;
                }

                stopRender();

                return;
            }
            ui.nextFrame();
        }
    }
}
