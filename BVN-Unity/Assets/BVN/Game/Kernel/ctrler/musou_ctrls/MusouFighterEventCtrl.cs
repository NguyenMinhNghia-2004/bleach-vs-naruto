using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouFighterEventCtrl : BaseFighterEventCtrl
    {
        public override void initialize()
        {
            base.initialize();

            FighterEventDispatcher.addEventListener(FighterEvent.HIT_TARGET, (Action<FighterEvent>)onHitTarget);

            FighterEventDispatcher.addEventListener(FighterEvent.BIRTH, (Action<FighterEvent>)onEnemyEvent);
            FighterEventDispatcher.addEventListener(FighterEvent.HURT, (Action<FighterEvent>)onEnemyEvent);
            FighterEventDispatcher.addEventListener(FighterEvent.DEFENSE, (Action<FighterEvent>)onEnemyEvent);
            FighterEventDispatcher.addEventListener(FighterEvent.IDLE, (Action<FighterEvent>)onEnemyEvent);
            FighterEventDispatcher.addEventListener(FighterEvent.HURT_RESUME, (Action<FighterEvent>)onEnemyEvent);
            FighterEventDispatcher.addEventListener(FighterEvent.HURT_DOWN, (Action<FighterEvent>)onEnemyEvent);

            FighterEventDispatcher.addEventListener(FighterEvent.DO_SPECIAL, (Action<FighterEvent>)changeFighter);
            FighterEventDispatcher.addEventListener(FighterEvent.DIE, (Action<FighterEvent>)onDie);
            FighterEventDispatcher.addEventListener(FighterEvent.DEAD, (Action<FighterEvent>)onDead);
        }

        private void onEnemyBeHit(FighterMain f)
        {
            GameCtrl.I.getMusouCtrl().updateEnemy(f);
        }

        private void onEnemyBirth(FighterMain f)
        {
            if (f.musouEnemyData.isBoss)
            {
                GameCtrl.I.getMusouCtrl().onBossBirth(f);
            }
            GameCtrl.I.getMusouCtrl().updateEnemy(f);
        }

        private void onEnemyDead(FighterMain f)
        {
            GameCtrl.I.getMusouCtrl().gameRunData.koNum++;
            GameUI.I.updateMusouKONum();

            GameData.I.musouData.addMoney(f.musouEnemyData.getMoney());
            GameData.I.musouData.addFighterExp(f.musouEnemyData.getExp());

            if (f.musouEnemyData.isBoss)
            {
                GameCtrl.I.getMusouCtrl().onBossDead(f);
                GameUI.I.updateMusouBossHp();
                return;
            }

            EffectCtrl.I.removeEnemyEffect(f, (Action)(() =>
            {
                GameCtrl.I.getMusouCtrl().removeEnemy(f);
            }));
        }

        private bool changeNextFighter()
        {
            GameRunFighterGroup group = GameCtrl.I.gameRunData.p1FighterGroup;
            FighterMain nextFighter = group.getNextAliveFighter();
            if (nextFighter == null) return false;

            GameCtrl.I.getMusouCtrl().changeFighter(group.currentFighter, nextFighter);

            return true;
        }

        /// <summary>增加显示连击数字</summary>
        private void addHits(FighterMain fighter, IGameSprite target)
        {
            if (!(target is FighterMain)) return;
            if (TeamID.TEAM_1 != fighter.team.id) return;

            MusouLogic.I.addHits(target as FighterMain);
        }

        private void onEnemyEvent(FighterEvent @event)
        {
            FighterMain f = @event.fighter as FighterMain;
            if (f.musouEnemyData == null) return;

            switch (@event.type)
            {
                case FighterEvent.BIRTH:
                    onEnemyBirth(f);
                    break;
                case FighterEvent.HURT:
                case FighterEvent.DEFENSE:
                    onEnemyBeHit(f);
                    break;
                case FighterEvent.IDLE:
                case FighterEvent.HURT_RESUME:
                case FighterEvent.HURT_DOWN:
                    MusouLogic.I.removeHitTarget(f);
                    break;
            }
        }

        private void onHitTarget(FighterEvent e)
        {
            addHits(e.fighter as FighterMain, Get(e.@params, "target") as IGameSprite);
        }

        /// <summary>改变角色事件</summary>
        private void changeFighter(FighterEvent e)
        {
            FighterMain fighter = e.fighter as FighterMain;
            if (TeamID.TEAM_1 != fighter.team.id) return;

            changeNextFighter();
        }

        /// <summary>角色死亡事件</summary>
        private void onDie(FighterEvent e)
        {
            FighterMain fighter = e.fighter as FighterMain;

            if (TeamID.TEAM_1 == fighter.team.id)
            {
                GameCtrl.I.getMusouCtrl().onSelfDie(fighter);
            }
            if (TeamID.TEAM_2 == fighter.team.id)
            {
                MusouLogic.I.removeHitTarget(fighter);

                bool isBoss = fighter.musouEnemyData != null && fighter.musouEnemyData.isBoss;
                if (isBoss)
                {
                    GameCtrl.I.getMusouCtrl().onBossDie(fighter);
                }
            }
        }

        /// <summary>角色死透状态</summary>
        private void onDead(FighterEvent e)
        {
            FighterMain fighter = e.fighter as FighterMain;

            if (TeamID.TEAM_1 == fighter.team.id)
            {
                GameCtrl.I.getMusouCtrl().onSelfDead(fighter);
            }
            if (TeamID.TEAM_2 == fighter.team.id)
            {
                onEnemyDead(fighter);
            }
        }
    }
}
