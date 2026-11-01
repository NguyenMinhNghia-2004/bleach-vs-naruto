using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BaseFighterEventCtrl
    {
        private List<object> _attackers = new List<object>();

        public virtual void initialize()
        {
            FighterEventDispatcher.removeAllListeners();

            FighterEventDispatcher.addEventListener(FighterEvent.FIRE_BULLET, (Action<FighterEvent>)fireBullet);
            FighterEventDispatcher.addEventListener(FighterEvent.ADD_ATTACKER, (Action<FighterEvent>)addAttacker);
            FighterEventDispatcher.addEventListener(FighterEvent.ADD_EFFECT, (Action<FighterEvent>)addFollowEffect);
        }

        public virtual FighterAttacker getAttacker(string name, int team)
        {
            foreach (var o in _attackers)
            {
                var i = o as FighterAttacker;
                if (i == null) continue;
                if (i.name == name && i.team.id == team) return i;
            }
            return null;
        }

        public virtual void destroy()
        {
            FighterEventDispatcher.removeAllListeners();
        }

        /// <summary>移除子弹 , 通过bullet.onRemove传入</summary>
        private void removeBullet(Bullet bullet)
        {
            GameCtrl.I.removeGameSprite(bullet);
        }

        private void removeAttacker(FighterAttacker attacker)
        {
            GameCtrl.I.removeGameSprite(attacker);
            int id = _attackers.IndexOf(attacker);
            if (id != -1) _attackers.RemoveAt(id);
        }

        /// <summary>移除效果</summary>
        private void removeEffect(FollowEffectView effect)
        {
            GameCtrl.I.removeGameSprite(effect);
        }

        /// <summary>发射子弹 ，通过事件帧听</summary>
        private void fireBullet(FighterEvent @event)
        {
            object @params = @event.@params;
            if (@params == null || !Bool(Get(@params, "mc"))) return;

            Bullet bullet = new Bullet(Get(@params, "mc") as MovieClip, @params);
            bullet.onRemove = (Action<Bullet>)removeBullet;
            bullet.setHitVO(Get(@params, "hitVO") as HitVO);

            // P2 相同角色下召唤的飞行道具变色逻辑（上级可能是 FighterMain / Assister / Attacker）
            GameSpriteUtil.autoChangeSpColor(bullet, @event.fighter);

            GameCtrl.I.addGameSprite(@event.fighter.team.id, bullet);
        }

        private void addAttacker(FighterEvent @event)
        {
            object @params = @event.@params;
            if (@params == null || !Bool(Get(@params, "mc"))) return;

            FighterAttacker attacker = new FighterAttacker(Get(@params, "mc") as MovieClip, @params);
            attacker.onRemove = (Action<FighterAttacker>)removeAttacker;
            attacker.setOwner(@event.fighter);
            attacker.init();

            // P2 相同角色下召唤的独立道具变色逻辑
            GameSpriteUtil.autoChangeSpColor(attacker, @event.fighter);

            _attackers.Add(attacker);

            GameCtrl.I.addGameSprite(@event.fighter.team.id, attacker);
        }

        /// <summary>增加跟随效果视图</summary>
        private void addFollowEffect(FighterEvent @event)
        {
            object @params = @event.@params;
            if (@params == null || !Bool(Get(@params, "mc"))) return;

            FollowEffectView effect = new FollowEffectView(Get(@params, "mc") as MovieClip);
            effect.onRemove = (Action<FollowEffectView>)removeEffect;
            effect.setTarget(@event.fighter);

            // P2 相同角色下召唤的跟随效果变色逻辑
            GameSpriteUtil.autoChangeSpColor(effect, @event.fighter);

            bool isUnderBody = Bool(Get(@params, "isUnderBody"));
            GameCtrl.I.addGameSprite(@event.fighter.team.id, effect, isUnderBody ? 0 : -1);
        }
    }
}
