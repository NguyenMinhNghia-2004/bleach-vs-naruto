using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>练习模式控制类</summary>
    public class TrainingCtrl
    {
        public static bool RECOVER_HP = true;
        public static bool RECOVER_QI = true;
        public static bool RECOVER_FZ_QI = true;

        // AS3 {} keyed by "<id>_hp" / "<id>_qi" / "<id>fz"; a missing key == undefined
        private Dictionary<string, double> _trainAddDelay;
        private List<object> _fighters;

        public virtual void initialize(List<object> fighters)
        {
            _fighters = fighters;

            foreach (var o in _fighters)
            {
                var i = o as FighterMain;
                if (i == null) continue;
                i.qi = i.qiMax;
            }

            _trainAddDelay = new Dictionary<string, double>();

            StateCtrl.I.transOut(null, true);
        }

        public virtual void destroy()
        {
            _fighters = null;
            _trainAddDelay = null;
        }

        public virtual void render()
        {
            if (_fighters == null) return;
            foreach (var o in _fighters)
            {
                var i = o as FighterMain;
                if (i == null) continue;

                string hpid = i.id + "_hp";
                if (i.actionState == FighterActionState.HURT_ING || i.actionState == FighterActionState.HURT_FLYING)
                {
                    _trainAddDelay[hpid] = 1 * GameConfig.FPS_GAME;
                }
                else
                {
                    if (_trainAddDelay.TryGetValue(hpid, out var hpDelay) && hpDelay > 0)
                    {
                        _trainAddDelay[hpid] = --hpDelay;
                        if (hpDelay <= 0 && RECOVER_HP)
                        {
                            i.hp = i.hpMax;
                        }
                    }
                    else
                    {
                        if (i.hp == i.hpMax)
                        {
                            _trainAddDelay[hpid] = 0;
                        }
                        else
                        {
                            _trainAddDelay[hpid] = 2 * GameConfig.FPS_GAME;
                        }
                    }
                }

                string qiid = i.id + "_qi";
                if (_trainAddDelay.TryGetValue(qiid, out var qiDelay) && qiDelay > 0)
                {
                    _trainAddDelay[qiid] = --qiDelay;
                    if (qiDelay <= 0 && RECOVER_QI)
                    {
                        i.qi = i.qiMax;
                    }
                }
                else
                {
                    if (i.qi == i.qiMax)
                    {
                        _trainAddDelay[qiid] = 0;
                    }
                    else
                    {
                        _trainAddDelay[qiid] = 2 * GameConfig.FPS_GAME;
                    }
                }

                string fzid = i.id + "fz";
                if (_trainAddDelay.TryGetValue(fzid, out var fzDelay) && fzDelay > 0)
                {
                    _trainAddDelay[fzid] = --fzDelay;
                    if (fzDelay <= 0 && RECOVER_FZ_QI)
                    {
                        i.fzqi = i.fzqiMax;
                    }
                }
                else
                {
                    if (i.fzqi == i.fzqiMax)
                    {
                        _trainAddDelay[fzid] = 0;
                    }
                    else
                    {
                        _trainAddDelay[fzid] = 2 * GameConfig.FPS_GAME;
                    }
                }
            }
        }
    }
}
