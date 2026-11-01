using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏主逻辑控制类</summary>
    public class GameMainLogicCtrl
    {
        public bool renderHit = true;

        /// 身体重叠粗测距离（像素）
        private const double BODY_SKIP_DIST = 200;
        /// 攻击判定粗测距离（像素）
        private const double HIT_SKIP_DIST = 600;

        // reusable result holders (AS3 {A:0,B:0} / {hitVO:null, hitRect:null})
        private class VecOut { public double A, B; }
        private class HitResult { public HitVO hitVO; public Rectangle hitRect; }

        private Rectangle _bodyHitRect = new Rectangle();
        private Rectangle _hitRectA = new Rectangle();
        private Rectangle _hitRectB = new Rectangle();
        /// getVec 复用
        private VecOut _vecOut = new VecOut();
        private HitResult _hitResultA = new HitResult();
        private HitResult _hitResultB = new HitResult();
        private GameStage _gameState;
        private double _leftSide = 0;
        private double _rightSide = 0;
        private TeamMap _teamMap;
        private bool _renderAnimate;

        public virtual void initialize(GameStage gameState, TeamMap teamMap)
        {
            _gameState = gameState;
            _teamMap = teamMap;

            MapMain map = gameState.getMap();

            _leftSide = map.left + GameConfig.X_SIDE_OFFSET;
            _rightSide = map.right - GameConfig.X_SIDE_OFFSET;
        }

        public virtual void setSpeedPlus(double v)
        {
            GameConfig.SPEED_PLUS = v;
            int l = _teamMap.teams.Count;

            for (int i = 0; i < l; i++)
            {
                TeamVO team = _teamMap.teams[i];
                int cl = team.children.Count;
                for (int j = 0; j < cl; j++)
                {
                    IGameSprite sp = team.children[j];
                    if (sp != null && !sp.isDestroyed())
                    {
                        sp.setSpeedRate(v);
                    }
                }
            }
        }

        public virtual void destroy()
        {
        }

        public virtual void render()
        {
            renderMainLogic();
        }

        public virtual void renderAnimate()
        {
            _renderAnimate = true;
        }

        /// <summary>暂停时，需要运行的</summary>
        public virtual void renderPause()
        {
        }

        /// <summary>游戏主逻辑</summary>
        private void renderMainLogic()
        {
            List<TeamVO> teams = _teamMap.teams;
            int i, j, k, l;
            IGameSprite sp, sp2;
            TeamVO team, team2;
            List<IGameSprite> children, children2;
            int teamLen = teams.Count;
            int childLen;
            List<IGameSprite> displays = _gameState.getGameSprites();

            for (i = 0; i < displays.Count; i++)
            {
                renderGameSprite(displays[i]);
            }

            for (i = 0; i < teamLen; i++)
            {
                team = teams[i];
                children = team.children;
                for (j = 0; j < children.Count; j++)
                {
                    sp = children[j];

                    if (sp == null || sp.isDestroyed()) continue;

                    //两两匹配，比如有3个：1与2,3比较，2与3比较
                    for (k = i + 1; k < teamLen; k++)
                    {
                        team2 = teams[k];
                        children2 = team2.children;
                        childLen = children2.Count;

                        for (l = 0; l < childLen; l++)
                        {
                            sp2 = children2[l];

                            if (sp2 == null || sp2.isDestroyed()) continue;

                            if (!farApart(sp, sp2, BODY_SKIP_DIST))
                            {
                                checkBodyHit(sp, sp2);
                            }
                            if (_renderAnimate && !farApart(sp, sp2, HIT_SKIP_DIST))
                            {
                                checkHit(sp, sp2);
                            }
                        }
                    }
                }
            }

            _renderAnimate = false;
        }

        //身体之间的碰撞处理
        private void checkBodyHit(IGameSprite A, IGameSprite B)
        {
            if (!renderHit) return;

            if (!(A is BaseGameSprite)) return;
            if (!(B is BaseGameSprite)) return;

            BaseGameSprite ba = A as BaseGameSprite;
            BaseGameSprite bb = B as BaseGameSprite;

            if (ba.isCross || bb.isCross) return;

            Rectangle bodyA = A.getBodyArea();
            Rectangle bodyB = B.getBodyArea();

            if (bodyA == null) return;
            if (bodyB == null) return;

            if (!intersectInto(bodyA, bodyB, _bodyHitRect) || _bodyHitRect.isEmpty()) return;

            double vecA = ba.getVecX();
            double vecB = bb.getVecX();

            VecOut overVec = null;

            if (ba.x < bb.x)
            {
                if ((vecA < 0 && vecA < vecB) || (vecB > 0 && vecB > vecA)) return;
                if (_bodyHitRect.width > 2)
                {
                    overVec ??= fillVec(5 * GameConfig.SPEED_PLUS, ba, bb);
                    ba.move(-overVec.A);
                    bb.move(overVec.B);
                }
            }
            else
            {
                if ((vecA > 0 && vecA > vecB) || (vecB < 0 && vecB < vecA)) return;
                if (_bodyHitRect.width > 2)
                {
                    overVec ??= fillVec(5 * GameConfig.SPEED_PLUS, ba, bb);
                    ba.move(overVec.A);
                    bb.move(-overVec.B);
                }
            }

            if (vecA != 0)
            {
                VecOut vo = fillVec(vecA, ba, bb);
                bb.move(vo.B);
                ba.move(-vo.A);
            }

            if (vecB != 0)
            {
                VecOut vo2 = fillVec(vecB, ba, bb);
                ba.move(vo2.A);
                bb.move(-vo2.B);
            }
        }

        /// 按双方重量分配位移量，写入复用对象。
        private VecOut fillVec(double vec, BaseGameSprite ba, BaseGameSprite bb)
        {
            double rate = (bb.heavy / ba.heavy) * 0.5;
            if (rate > 0.9) rate = 0.9;
            if (rate < 0.1) rate = 0.1;

            double vecA = vec * rate;
            double vecB = vec * (1 - rate);

            if (ba.getIsTouchSide() && bb.getIsTouchSide())
            {
                vecA = vec;
                vecB = vec;
            }
            else if (ba.getIsTouchSide())
            {
                vecA = 0;
                vecB = vec;
            }
            else if (bb.getIsTouchSide())
            {
                vecB = 0;
                vecA = vec;
            }

            _vecOut.A = vecA;
            _vecOut.B = vecB;
            return _vecOut;
        }

        //运行游戏元件
        private void renderGameSprite(IGameSprite sp)
        {
            try
            {
                GameLogic.fixGameSpritePosition(sp);

                if (sp is BaseGameSprite)
                {
                    BaseGameSprite bsp = sp as BaseGameSprite;
                    bool inAir = GameLogic.isInAir(bsp);
                    if (inAir)
                    {
                        bsp.applayG(GameConfig.G);
                    }
                    bsp.setInAir(inAir);
                }

                sp.render();
                if (_renderAnimate && !sp.isDestroyed())
                {
                    sp.renderAnimate();
                }
            }
            catch (Exception e)
            {
                Debugger.log(GetLang("debug.log.data.game_main_logic_ctrler.render_game_sprite", new ASObject { ["error"] = e }));
            }
        }

        //攻击碰撞检测
        private void checkHit(IGameSprite A, IGameSprite B)
        {
            if (!renderHit) return;

            List<object> hitsA = A.getCurrentHits();
            List<object> hitsB = B.getCurrentHits();

            Rectangle bodyA;
            Rectangle bodyB;

            if (A is BaseGameSprite && !(A as BaseGameSprite).isAllowBeHit)
            {
                bodyA = null;
            }
            else
            {
                bodyA = A.getBodyArea();
            }

            if (B is BaseGameSprite && !(B as BaseGameSprite).isAllowBeHit)
            {
                bodyB = null;
            }
            else
            {
                bodyB = B.getBodyArea();
            }

            if (Debugger.DRAW_AREA)
            {
                if (bodyA != null) _gameState.drawGameRect(bodyA, 0xffff00, 0.5);
                if (bodyB != null) _gameState.drawGameRect(bodyB, 0xffff00, 0.5);
            }

            HitResult AhitB = getHitObj(hitsA, bodyB, _hitResultA, _hitRectA);
            HitResult BhitA = getHitObj(hitsB, bodyA, _hitResultB, _hitRectB);

            if (AhitB != null)
            {
                B.beHit(AhitB.hitVO, AhitB.hitRect);
                A.hit(AhitB.hitVO, B);
                if (Debugger.DRAW_AREA)
                {
                    _gameState.drawGameRect(AhitB.hitRect, 0xffffff, 1);
                }
            }

            if (BhitA != null)
            {
                A.beHit(BhitA.hitVO, BhitA.hitRect);
                B.hit(BhitA.hitVO, A);
                if (Debugger.DRAW_AREA)
                {
                    _gameState.drawGameRect(BhitA.hitRect, 0xffffff, 1);
                }
            }
        }

        /// <summary>获取攻击到角色的区域：{ hitVO , hitRect } 或 null</summary>
        private HitResult getHitObj(List<object> hitArray, Rectangle body, HitResult @out, Rectangle hitRectOut)
        {
            if (body == null) return null;
            if (hitArray == null || hitArray.Count < 1) return null;

            int hitLen = hitArray.Count;

            for (int i = 0; i < hitLen; i++)
            {
                HitVO hitvo = hitArray[i] as HitVO;
                if (hitvo == null) continue;

                Rectangle hitArea = hitvo.currentArea;
                if (hitArea == null) continue;

                if (Debugger.DRAW_AREA)
                {
                    _gameState.drawGameRect(hitArea);
                }

                if (intersectInto(hitArea, body, hitRectOut) && !hitRectOut.isEmpty())
                {
                    @out.hitVO = hitvo;
                    @out.hitRect = hitRectOut;
                    return @out;
                }
            }

            return null;
        }

        /// 粗测：双方锚点距离是否超出阈值，超出则可跳过细测。
        private static bool farApart(IGameSprite A, IGameSprite B, double maxDist)
        {
            double dx = A.x - B.x;
            if (dx > maxDist || dx < -maxDist) return true;
            double dy = A.y - B.y;
            return dy > maxDist || dy < -maxDist;
        }

        /// 将 a∩b 写入 out，避免 Rectangle.intersection 分配。有交集时为 true。
        private static bool intersectInto(Rectangle a, Rectangle b, Rectangle @out)
        {
            double x = a.x > b.x ? a.x : b.x;
            double y = a.y > b.y ? a.y : b.y;
            double r = a.right < b.right ? a.right : b.right;
            double bot = a.bottom < b.bottom ? a.bottom : b.bottom;
            if (r <= x || bot <= y)
            {
                @out.setTo(0, 0, 0, 0);
                return false;
            }
            @out.setTo(x, y, r - x, bot - y);
            return true;
        }
    }
}
