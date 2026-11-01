using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Mc 动作 Handler 共享基类：持有门面 / 运行时 / 受击域引用。</summary>
    public class FighterMcActsPart
    {
        public FighterMcCtrler _owner;
        public FighterMcRuntime _rt;
        public FighterMcHurtCtrl _hurt;

        /// <summary>绑定共享引用。</summary>
        public virtual void bind(FighterMcCtrler owner, FighterMcRuntime rt, FighterMcHurtCtrl hurt)
        {
            _owner = owner;
            _rt = rt;
            _hurt = hurt;
        }

        /// <summary>释放引用。</summary>
        public virtual void destroy()
        {
            _owner = null;
            _rt = null;
            _hurt = null;
        }

        public virtual FighterMain _fighter => _rt.fighter;

        public virtual FighterMC _mc => _rt.mc;

        public virtual FighterAction _action => _rt.action;

        public virtual FighterActionLogic _actionLogic => _rt.actionLogic;

        public virtual IFighterActionCtrl _actionCtrler => _rt.actionCtrler;

        public virtual bool _isTouchFloor
        {
            get => _rt.isTouchFloor;
            set => _rt.isTouchFloor = value;
        }

        public virtual bool _isFalling
        {
            get => _rt.isFalling;
            set => _rt.isFalling = value;
        }

        public virtual string _doingAction
        {
            get => _rt.doingAction;
            set => _rt.doingAction = value;
        }

        public virtual string _doingAirAction
        {
            get => _rt.doingAirAction;
            set => _rt.doingAirAction = value;
        }
    }
}
