using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterEventCtrl : BaseFighterEventCtrl
    {
        public override void initialize()
        {
            base.initialize();

            FighterEventDispatcher.addEventListener(FighterEvent.DO_SPECIAL, (Action<FighterEvent>)addAssister);

            FighterEventDispatcher.addEventListener(FighterEvent.HIT_TARGET, (Action<FighterEvent>)onHitTarget);
            FighterEventDispatcher.addEventListener(FighterEvent.HURT_RESUME, (Action<FighterEvent>)onHurtResume);
            FighterEventDispatcher.addEventListener(FighterEvent.DEAD, (Action<FighterEvent>)onDead);
            FighterEventDispatcher.addEventListener(FighterEvent.IDLE, (Action<FighterEvent>)onIdle);
            FighterEventDispatcher.addEventListener(FighterEvent.DIE, (Action<FighterEvent>)onDie);
        }

        private void removeAssister(Assister assister)
        {
            GameCtrl.I.removeGameSprite(assister);
        }

        /// <summary>增加显示连击数字</summary>
        private void addHits(FighterMain fighter, IGameSprite target)
        {
            string targetId = target != null && target is BaseGameSprite ? (target as BaseGameSprite).id : null;

            int uiId = fighter.team.id;

            int hits = GameLogic.addHits(fighter.id, targetId, uiId);
            if (hits > 1)
            {
                GameCtrl.I.gameState.gameUI.getUI().showHits(hits, uiId);
            }
        }

        //清除连击数字
        private void removeHits(string targetId)
        {
            object o = GameLogic.getHitsObjByTargetId(targetId);
            if (o != null)
            {
                GameCtrl.I.gameState.gameUI.getUI().hideHits(Int(Get(o, "uiID")));
            }
            GameLogic.clearHitsByTargetId(targetId);
        }

        /// <summary>添加辅助</summary>
        private void addAssister(FighterEvent e)
        {
            FighterMain fighter = e.fighter as FighterMain;

            if (fighter.actionState != FighterActionState.NORMAL &&
                fighter.actionState != FighterActionState.DEFENSE_ING)
            {
                return;
            }
            if (fighter.fzqi < fighter.fzqiMax) return;

            fighter.fzqi = 0;

            // 练习模式：仅在援助真正召唤成功时记一次 O
            FighterEvent trainEvt = new FighterEvent(FighterEvent.DO_ACTION);
            trainEvt.@params = new ASObject { ["input"] = FighterInputCmd.ASSIST };
            fighter.dispatchEvent(trainEvt);

            GameRunFighterGroup group = TeamID.TEAM_1 == fighter.team.id
                ? GameCtrl.I.gameRunData.p1FighterGroup
                : GameCtrl.I.gameRunData.p2FighterGroup;
            Assister assister = group.currentAssister;
            assister.setOwner(fighter);
            assister.direct = fighter.direct;
            assister.x = fighter.x - 30 * assister.direct;
            assister.y = fighter.y;
            assister.onRemove = (Action<Assister>)removeAssister;

            // P2 相同辅助变色逻辑
            GameSpriteUtil.autoChangeSpColor(assister, fighter);

            GameCtrl.I.addGameSprite(e.fighter.team.id, assister);
            EffectCtrl.I.assisterEffect(assister);
            assister.goFight();
        }

        /// <summary>攻击到目标事件</summary>
        private void onHitTarget(FighterEvent e)
        {
            addHits(e.fighter as FighterMain, Get(e.@params, "target") as IGameSprite);

            if (GameMode.isArcade() && TeamID.TEAM_1 == e.fighter.team.id)
            {
                GameLogic.addScoreByHitTarget(Get(e.@params, "hitvo") as HitVO);
            }
        }

        //角色受击恢复事件
        private void onHurtResume(FighterEvent e)
        {
            removeHits(e.fighter.id);
        }

        private void onDead(FighterEvent e)
        {
            removeHits(e.fighter.id);
        }

        //角色倒地事件
        private void onIdle(FighterEvent e)
        {
            removeHits(e.fighter.id);
        }

        private void onDie(FighterEvent e)
        {
            GameCtrl.I.onFighterDie(e.fighter as FighterMain);
        }
    }
}
