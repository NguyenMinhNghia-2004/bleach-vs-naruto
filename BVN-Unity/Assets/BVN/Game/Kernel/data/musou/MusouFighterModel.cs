using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouFighterModel
    {
        private static MusouFighterModel _i;

        public static MusouFighterModel I
        {
            get
            {
                _i ??= new MusouFighterModel();
                return _i;
            }
        }

        public List<MusouFighterSellVO> fighters;

        private bool _inited = false;

        public virtual void init()
        {
            if (!_inited)
            {
                initFighters();
                _inited = true;
            }
        }

        public virtual void allCustom()
        {
            fighters = new List<MusouFighterSellVO>();
            _inited = true;
        }

        public virtual void addFighter(string id, int price)
        {
            if (containsFighter(id))
            {
                TraceLang("debug.trace.data.musou_fighter_model.repeat_add", new ASObject { ["fighterId"] = id });
                return;
            }
            fighters.Add(new MusouFighterSellVO(id, price));
        }

        private void initFighters()
        {
            fighters = new List<MusouFighterSellVO>();

            fighters.Add(new MusouFighterSellVO("ichigo", 5000));
            fighters.Add(new MusouFighterSellVO("ichigo_zero", 5000));
            fighters.Add(new MusouFighterSellVO("rukia", 5000));
            fighters.Add(new MusouFighterSellVO("renji", 5000));
            fighters.Add(new MusouFighterSellVO("naruto", 5000));
            fighters.Add(new MusouFighterSellVO("sakura", 5000));
            fighters.Add(new MusouFighterSellVO("uryuu", 5000));
            fighters.Add(new MusouFighterSellVO("lee", 5000));
            fighters.Add(new MusouFighterSellVO("neji", 5000));
            fighters.Add(new MusouFighterSellVO("temari", 5000));

            fighters.Add(new MusouFighterSellVO("chad", 7000));
            fighters.Add(new MusouFighterSellVO("karin", 7000));
            fighters.Add(new MusouFighterSellVO("gaara", 7000));

            fighters.Add(new MusouFighterSellVO("sasuke", 8000));
            fighters.Add(new MusouFighterSellVO("orihime", 8000));
            fighters.Add(new MusouFighterSellVO("ikkaku", 8000));
            fighters.Add(new MusouFighterSellVO("gin", 8000));
            fighters.Add(new MusouFighterSellVO("juggo", 8000));
            fighters.Add(new MusouFighterSellVO("suigetsu", 8000));
            fighters.Add(new MusouFighterSellVO("kakashi", 8000));
            fighters.Add(new MusouFighterSellVO("sakura_sr", 8000));
            fighters.Add(new MusouFighterSellVO("hinata", 8000));

            fighters.Add(new MusouFighterSellVO("kimimaro", 9000));
            fighters.Add(new MusouFighterSellVO("killerbee", 9000));

            fighters.Add(new MusouFighterSellVO("toushirou", 10000));
            fighters.Add(new MusouFighterSellVO("byakuya", 10000));
            fighters.Add(new MusouFighterSellVO("jiraiya", 10000));
            fighters.Add(new MusouFighterSellVO("orochimaru", 10000));
            fighters.Add(new MusouFighterSellVO("deidara", 10000));
            fighters.Add(new MusouFighterSellVO("soifon", 10000));
            fighters.Add(new MusouFighterSellVO("konan", 10000));
            fighters.Add(new MusouFighterSellVO("grimmjow", 10000));

            fighters.Add(new MusouFighterSellVO("mayuri", 12000));

            fighters.Add(new MusouFighterSellVO("yoruichi", 15000));
            fighters.Add(new MusouFighterSellVO("kenpachi", 15000));
            fighters.Add(new MusouFighterSellVO("itachi", 15000));
            fighters.Add(new MusouFighterSellVO("pain", 15000));
            fighters.Add(new MusouFighterSellVO("ichigo_bankai", 15000));
            fighters.Add(new MusouFighterSellVO("naruto_oneTail", 15000));
            fighters.Add(new MusouFighterSellVO("sasuke_susanoo", 15000));

            fighters.Add(new MusouFighterSellVO("ichigo_vizored_half", 18000));
            fighters.Add(new MusouFighterSellVO("naruto_hermit", 18000));

            fighters.Add(new MusouFighterSellVO("starrk", 20000));
            fighters.Add(new MusouFighterSellVO("ulquiorra", 23000));
            fighters.Add(new MusouFighterSellVO("obito", 25000));

            fighters.Add(new MusouFighterSellVO("aizen", 40000));
            fighters.Add(new MusouFighterSellVO("madara", 42000));
            fighters.Add(new MusouFighterSellVO("kenshin", 45000));
        }

        private bool containsFighter(string id)
        {
            if (fighters == null) return false;
            foreach (var f in fighters)
            {
                if (f.id == id)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
