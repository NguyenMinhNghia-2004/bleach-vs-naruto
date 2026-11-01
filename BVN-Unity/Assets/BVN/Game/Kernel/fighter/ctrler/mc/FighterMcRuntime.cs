using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Runtime state shared between FighterMcCtrler and its Hurt/Action collaborators.</summary>
    public class FighterMcRuntime
    {
        /// <summary>角色主类。</summary>
        public FighterMain fighter;

        /// <summary>角色 MC 封装。</summary>
        public FighterMC mc;

        /// <summary>动作定义。</summary>
        public FighterAction action;

        /// <summary>动作输入判定。</summary>
        public FighterActionLogic actionLogic;

        /// <summary>操作控制（键/AI）。</summary>
        public IFighterActionCtrl actionCtrler;

        /// <summary>是否已贴地。</summary>
        public bool isTouchFloor = true;

        /// <summary>是否正在下落。</summary>
        public bool isFalling;

        /// <summary>当前动作帧标签。</summary>
        public string doingAction;

        /// <summary>当前空中动作帧标签。</summary>
        public string doingAirAction;

        /// <summary>当前 doAction 已持续帧数（供卍解等判定）。</summary>
        public int doActionFrame;
    }
}
