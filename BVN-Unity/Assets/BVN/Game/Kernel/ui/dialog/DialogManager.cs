using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class DialogManager
    {
        private static Sprite _dialogBG;
        private static List<BaseDialog> _showingDialogs = new List<BaseDialog>();

        public static bool showingDialog()
        {
            return _showingDialogs.Count > 0;
        }

        public static void showDialog(BaseDialog v, bool hideOthers = true)
        {
            if (hideOthers && _showingDialogs.Count > 0)
            {
                foreach (var d in _showingDialogs)
                {
                    d.hide();
                }
            }
            else
            {
                addDialogBg();
            }

            double x = (v.width > 0) ? (GameConfig.GAME_SIZE.x - v.width) / 2 : 0;
            double y = (v.height > 0) ? (GameConfig.GAME_SIZE.y - v.height) / 2 : 0;
            v.show(x, y);

            fadIn(v);

            _showingDialogs.Add(v);
        }

        public static void closeDialog(BaseDialog v)
        {
            fadOut(v, (Action)tweenBack);

            void tweenBack()
            {
                int index = _showingDialogs.IndexOf(v);
                if (index != -1)
                {
                    splice(_showingDialogs, index, 1);
                }

                if (_showingDialogs.Count < 1)
                {
                    try
                    {
                        MainGame.I.root.removeChild(_dialogBG);
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    BaseDialog last = _showingDialogs[_showingDialogs.Count - 1];
                    if (last.hiding())
                    {
                        last.resume();
                    }

                    addDialogBg();
                    fadIn(last);
                }
            }
        }

        /// <summary>预创建对话框遮罩，避免首次弹窗时分配卡顿（不加入显示列表）。</summary>
        public static void warmUp()
        {
            ensureDialogBg();
        }

        private static void addDialogBg()
        {
            ensureDialogBg();
            MainGame.I.root.addChild(_dialogBG);
        }

        /// <summary>确保遮罩 Sprite 已创建。</summary>
        private static void ensureDialogBg()
        {
            if (_dialogBG != null)
            {
                return;
            }
            var bp = new BitmapData(1, 1, false, 0);
            _dialogBG = new Sprite();
            _dialogBG.graphics.beginBitmapFill(bp, null, true, false);
            _dialogBG.graphics.drawRect(0, 0, GameConfig.GAME_SIZE.x, GameConfig.GAME_SIZE.y);
            _dialogBG.graphics.endFill();
            _dialogBG.alpha = 0.7;
        }

        private static void fadIn(BaseDialog d, Delegate back = null)
        {
            DisplayObject view = d.getDisplay();

            double y = view.y;
            view.alpha = 0;
            view.y -= 10;

            MainGame.I.root.addChild(d.getDisplay());

            TweenLite.to(view, 0.3, new ASObject
            {
                ["y"] = y,
                ["alpha"] = 1.0,
                ["onComplete"] = (Action)(() =>
                {
                    d.init();
                    if (back != null)
                    {
                        Call(back);
                    }
                })
            });
        }

        private static void fadOut(BaseDialog d, Delegate back)
        {
            DisplayObject view = d.getDisplay();
            TweenLite.to(view, 0.2, new ASObject
            {
                ["y"] = view.y - 10,
                ["alpha"] = 0.0,
                ["onComplete"] = (Action)(() =>
                {
                    d.close();
                    d.destroy();
                    try
                    {
                        MainGame.I.root.removeChild(d.getDisplay());
                    }
                    catch (Exception)
                    {
                    }

                    // if(back != null) back();
                })
            });
            if (back != null)
            {
                Call(back);
            }
        }
    }
}
