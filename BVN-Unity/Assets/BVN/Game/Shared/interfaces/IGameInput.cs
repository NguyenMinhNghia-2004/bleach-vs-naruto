using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Game input device abstraction.</summary>
    public interface IGameInput
    {
        bool enabled { get; set; }
        void initialize(Stage stage);
        void setConfig(object config);
        void focus();
        bool anyKey();
        bool back();
        bool select();
        bool up();
        bool down();
        bool left();
        bool right();
        bool attack();
        bool jump();
        bool dash();
        bool skill();
        bool superSkill();
        bool special();
        bool wankai();
        void clear();
    }
}
