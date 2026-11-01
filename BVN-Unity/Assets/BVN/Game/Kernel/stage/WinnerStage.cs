using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class WinnerStage : IStage
    {
        private MovieClip _ui;
        private BitmapFontText _scoreText;
        private List<object> _winnerFaces;
        private string _winSay;
        private int _bgmDelay;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            buildData();

            _scoreText = new BitmapFontText(AssetManager.I.getFont("font1"));
            _scoreText.text = "SCORE " + Str(GameData.I.score);
            _scoreText.x = -_scoreText.width / 2;
            _scoreText.y = -_scoreText.height / 2;

            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.loading, ResUtils.WINNER) as MovieClip;
            _ui.addEventListener(Event.COMPLETE, onUIPlayComplete);

            (_ui.getChildByName("scoremc") as DisplayObjectContainer).addChild(_scoreText);

            for (int i = 0; i < 3; i++)
            {
                var f = _ui.getChildByName("f" + i) as DisplayObjectContainer;
                var face = i < _winnerFaces.Count ? _winnerFaces[i] as DisplayObject : null;
                if (face != null)
                {
                    f.addChildAt(face, 0);
                }
                else
                {
                    f.visible = false;
                }
            }

            SoundCtrl.I.BGM(null);
            SoundCtrl.I.playAssetSound("win");

            GameInputer.enabled = false;

            initText();

            _bgmDelay = (int)setTimeout((Action)(() =>
            {
                SoundCtrl.I.BGM(AssetManager.I.getSound("winloop"));
            }), 6500);
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            GameRender.remove((Action)render);
            GameInputer.enabled = false;
            clearTimeout((uint)_bgmDelay);
            GameEvent.dispatchEvent(GameEvent.WINNER_END);
            if (_ui != null)
            {
                _ui.gotoAndStop("destroy");
                _ui = null;
            }
        }

        private void buildData()
        {
            FighterVO winner = FighterModel.I.getFighter(GameData.I.winnerId);
            var winFighters = new List<FighterVO>();

            List<object> fs = GameData.I.p1Select.getSelectFighters();
            for (int i = 0; i < fs.Count; i++)
            {
                winFighters.Add(FighterModel.I.getFighter(Str(fs[i])));
            }

            _winSay = winner.getRandSay();

            if (winFighters.Count == 1)
            {
                _winnerFaces = new List<object> { AssetManager.I.getFighterFaceWin(winFighters[0]) };
            }
            else
            {
                _winnerFaces = new List<object>();
                int index = winFighters.IndexOf(winner);
                _winnerFaces.Add(AssetManager.I.getFighterFaceWin(index >= 0 ? winFighters[index] : null));
                AS3.splice(winFighters, index, 1);
                for (int j = 0; j < winFighters.Count; j++)
                {
                    _winnerFaces.Add(AssetManager.I.getFighterFaceWin(winFighters[j]));
                }
            }
        }

        private void initText()
        {
            var txtmc = _ui.getChildByName("txtmc") as MovieClip;

            void txtCompleteHandler(Event e)
            {
                txtmc.removeEventListener(Event.COMPLETE, txtCompleteHandler);

                var txt = new TextField();
                txt.x = 33;
                txt.width = 548;
                txt.height = 66;
                txt.multiline = true;
                txt.wordWrap = true;
                txt.mouseEnabled = false;

                var tf = new TextFormat();
                tf.font = "黑体";
                tf.color = 0;
                tf.size = 18;
                tf.align = TextFormatAlign.CENTER;
                tf.leading = 5;
                txt.defaultTextFormat = tf;

                setText(txt, _winSay);
                txtmc.addChild(txt);

                //testFighterSays(txt);
            }

            if (txtmc != null)
            {
                txtmc.addEventListener(Event.COMPLETE, txtCompleteHandler);
            }
        }

        private void testFighterSays(TextField txt)
        {
            FighterVO winner = FighterModel.I.getFighter(GameData.I.winnerId);

            int sayIndex = 0;
            var says = new List<object>();

            foreach (var k in Values(winner.says))
            {
                says.Add(new ASObject { ["id"] = winner.id, ["say"] = k });
            }

            foreach (var o in Values(FighterModel.I.getAllFighters()))
            {
                var i = o as FighterVO;
                if (i == null) continue;
                foreach (var j in Values(i.says))
                {
                    says.Add(new ASObject { ["id"] = i.id, ["say"] = j });
                }
            }

            void keyHandler(KeyboardEvent e)
            {
                switch (e.keyCode)
                {
                    case Keyboard.LEFT:
                        sayIndex--;
                        if (sayIndex < 0)
                        {
                            sayIndex = 0;
                        }
                        break;
                    case Keyboard.RIGHT:
                        sayIndex++;
                        if (sayIndex > says.Count - 1)
                        {
                            sayIndex = says.Count - 1;
                        }
                        break;
                    default:
                        return;
                }
                setText(txt, Str(Get(says[sayIndex], "say")));
                trace(Get(says[sayIndex], "id"));
            }

            MainGame.I.stage.addEventListener<KeyboardEvent>(KeyboardEvent.KEY_DOWN, keyHandler);
        }

        private void setText(TextField txt, string str)
        {
            txt.text = AS3.join(AS3.split(str, "|"), "\n");
            txt.height = txt.textHeight + 5;
            txt.y = 10 + (90 - txt.height) / 2;
        }

        private void render()
        {
            if (GameInputer.select(GameInputType.MENU))
            {
                goNext();
            }
        }

        private void goNext()
        {
            GameInputer.enabled = false;

            StateCtrl.I.transIn((Action)(() =>
            {
                MessionModel.I.messionComplete();
                MainGame.I.loadGame();
            }));
        }

        private void onUIPlayComplete(Event e)
        {
            _ui.removeEventListener(Event.COMPLETE, onUIPlayComplete);
            GameEvent.dispatchEvent(GameEvent.WINNER_SHOW);

            var btns = _ui.getChildByName("btns") as MovieClip;
            var btnMore = btns.getChildByName("btn_more");
            var btnCont = btns.getChildByName("btn_cont");
            var btnExit = btns.getChildByName("btn_exit");

            btnMore.addEventListener<MouseEvent>(MouseEvent.CLICK, btnHandler);
            btnCont.addEventListener<MouseEvent>(MouseEvent.CLICK, btnHandler);
            btnExit.addEventListener<MouseEvent>(MouseEvent.CLICK, btnHandler);

            btnMore.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, btnHandler);
            btnCont.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, btnHandler);
            btnExit.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, btnHandler);

            GameRender.add((Action)render);
            GameInputer.enabled = true;
        }

        private void btnHandler(MouseEvent e)
        {
            if (e.type == MouseEvent.MOUSE_OVER)
            {
                SoundCtrl.I.sndSelect();
                return;
            }

            var btns = _ui.getChildByName("btns") as MovieClip;
            var target = e.currentTarget;
            if (target == btns.getChildByName("btn_more"))
            {
                GameEvent.dispatchEvent(GameEvent.MORE_GAMES);
                GameInterface.instance.moreGames();
            }
            else if (target == btns.getChildByName("btn_cont"))
            {
                goNext();
            }
            else if (target == btns.getChildByName("btn_exit"))
            {
                GameUI.confrim(
                    GetLang("confirm.common.back_menu_title"),
                    GetLang("confirm.common.back_menu"),
                    (Action)MainGame.I.goMenu,
                    null,
                    IsMobile()
                );
                GameEvent.dispatchEvent(GameEvent.CONFRIM_BACK_MENU);
            }

            SoundCtrl.I.sndConfrim();
        }
    }
}
