using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Main scene switcher plus overlay layers on a root Sprite.</summary>
    public class KyoStageCtrl : EventDispatcher
    {
        public KyoStageCtrl(Sprite mainStage)
        {
            _mainStage = mainStage;
        }

        private Sprite _mainStage;
        private List<object> _layers = new List<object>();

        public int changeStateMouseGap = 0;
        private IStage _curStage;

        public virtual IStage currentStage => _curStage;

        public virtual bool noneLayer => _layers.Count == 0;

        private bool stageMouseChildren
        {
            set
            {
                if (_mainStage.stage != null) _mainStage.stage.mouseChildren = value;
            }
        }

        public virtual bool goStage(IStage stg, bool sameChange = false, bool buildAfterDestroy = false)
        {
            void newStage()
            {
                if (changeStateMouseGap > 0)
                {
                    stageMouseChildren = false;
                    setTimeout((Action)(() => { stageMouseChildren = true; }), changeStateMouseGap);
                }

                _curStage = stg;
                _curStage.build();
                _mainStage.addChild(_curStage.display);
                _curStage.afterBuild();
            }

            void detoryComplete()
            {
                try
                {
                    _mainStage.removeChild(_curStage.display);
                }
                catch (Exception e)
                {
                    trace("KyoStageCtrl: goStage:", e);
                }

                _curStage = null;
                newStage();
            }

            if (!sameChange)
            {
                string classname = getQualifiedClassName(stg);
                string classname2 = getQualifiedClassName(_curStage);
                if (classname == classname2) return false;
            }

            if (_curStage != null)
            {
                if (buildAfterDestroy)
                {
                    _curStage.destroy((Action)detoryComplete);
                }
                else
                {
                    _curStage.destroy();
                    detoryComplete();
                }
            }
            else
            {
                newStage();
            }

            dispatchEvent(new KyoStageEvent(KyoStageEvent.CHANGE_STATE, stg));

            return true;
        }

        public virtual void addLayer(IStage layer, double x = 0, double y = 0, bool removeElse = false, IStageFadeEffect effect = null,
            Delegate addBack = null)
        {
            if (removeElse) removeAllLayer();
            layer.build();

            double sw = _mainStage.stage != null ? _mainStage.stage.stageWidth : 0;
            double sh = _mainStage.stage != null ? _mainStage.stage.stageHeight : 0;

            double dw = layer.display.width * _mainStage.scaleX;
            double dh = layer.display.height * _mainStage.scaleY;

            layer.display.x = double.IsNaN(x) ? (sw - dw) / 2 : x;
            layer.display.y = double.IsNaN(y) ? (sh - dh) / 2 : y;
            _mainStage.addChild(layer.display);

            void effectBack()
            {
                layer.afterBuild();
                if (addBack != null) Call(addBack);
            }

            if (effect != null) effect.fadeIn(layer, (Action)effectBack);
            else effectBack();

            _layers.Add(layer);
        }

        /// <summary>layer: an IStage instance or a Class (System.Type) to test with `is`.</summary>
        public virtual bool hasLayer(object layer)
        {
            foreach (var o in _layers)
            {
                var i = (IStage)o;
                if (layer is IStage && i == layer) return true;
                if (layer is Type c && c.IsInstanceOfType(i)) return true;
            }
            return false;
        }

        public virtual void removeLayer(IStage layer, IStageFadeEffect effect = null, Delegate removeBack = null)
        {
            void onEffectFinish()
            {
                try
                {
                    _mainStage.removeChild(layer.display);
                    layer.destroy();
                }
                catch (Exception e)
                {
                    trace("KyoStageCtrl: removeLayer:", e);
                }

                int ix = _layers.IndexOf(layer);
                if (ix != -1) _layers.RemoveAt(ix);

                if (removeBack != null) Call(removeBack);
            }

            if (effect != null) effect.fadeOut(layer, (Action)onEffectFinish);
            else onEffectFinish();
        }

        public virtual void removeAllLayer()
        {
            // AS3 for-each iterated a live array while removeLayer spliced it; iterate a snapshot instead.
            foreach (var o in new List<object>(_layers)) removeLayer((IStage)o);
            _layers = new List<object>();
        }

        public virtual void clean(bool _removeAllLayer = true)
        {
            if (_removeAllLayer) removeAllLayer();
            if (_curStage != null)
            {
                _curStage.destroy();
                _mainStage.removeChild(_curStage.display);
                _curStage = null;
            }
        }
    }
}
