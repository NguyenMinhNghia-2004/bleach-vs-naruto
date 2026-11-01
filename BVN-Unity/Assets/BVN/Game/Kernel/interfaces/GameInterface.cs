using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 壳层 IGameInterface 的静态入口与默认菜单。入口启动时设置 instance；未注入时部分方法提供安全默认行为。
    /// </summary>
    public class GameInterface
    {
        /// <summary>当前壳实现；未注入时部分静态方法走默认逻辑。</summary>
        public static IGameInterface instance;

        /// <summary>默认主菜单结构（多语言文案）。</summary>
        public static List<object> getDefaultMenu()
        {
            List<object> a = new List<object>
            {
                new ASObject
                {
                    ["txt"] = "TEAM PLAY", ["cn"] = GetLang("txt.game_interface.team_play"), ["children"] = new List<object>
                    {
                        new ASObject { ["txt"] = "TEAM ACRADE", ["cn"] = GetLang("txt.game_interface.team_play_acrade") },
                        new ASObject { ["txt"] = "TEAM VS PEOPLE", ["cn"] = GetLang("txt.game_interface.team_play_vs_people") },
                        new ASObject { ["txt"] = "TEAM VS CPU", ["cn"] = GetLang("txt.game_interface.team_play_vs_CPU") },
                        new ASObject { ["txt"] = "TEAM WATCH", ["cn"] = GetLang("txt.game_interface.team_play_watch") },
                    }
                },
                new ASObject
                {
                    ["txt"] = "SINGLE PLAY", ["cn"] = GetLang("txt.game_interface.single_play"), ["children"] = new List<object>
                    {
                        new ASObject { ["txt"] = "SINGLE ACRADE", ["cn"] = GetLang("txt.game_interface.single_play_acrade") },
                        new ASObject { ["txt"] = "SINGLE VS PEOPLE", ["cn"] = GetLang("txt.game_interface.single_play_vs_people") },
                        new ASObject { ["txt"] = "SINGLE VS CPU", ["cn"] = GetLang("txt.game_interface.single_play_vs_CPU") },
                        new ASObject { ["txt"] = "SINGLE WATCH", ["cn"] = GetLang("txt.game_interface.single_play_watch") },
                    }
                },
                new ASObject
                {
                    ["txt"] = "MUSOU PLAY", ["cn"] = GetLang("txt.game_interface.musou_play"), ["children"] = new List<object>
                    {
                        new ASObject { ["txt"] = "MUSOU ACRADE", ["cn"] = GetLang("txt.game_interface.musou_acrade") },
                    }
                },
                new ASObject { ["txt"] = "OPTION", ["cn"] = GetLang("txt.game_interface.option") },
                new ASObject { ["txt"] = "TRAINING", ["cn"] = GetLang("txt.game_interface.training") },
                new ASObject { ["txt"] = "CREDITS", ["cn"] = GetLang("txt.game_interface.credits") },
                //            {txt: 'MORE GAMES', cn: GetLang('txt.game_interface.more_games')}
            };
            return a;
        }

        /// <summary>校验资源文件；无实例时默认通过。</summary>
        public static bool checkFile(string url, ByteArray file)
        {
            if (instance != null)
            {
                return instance.checkFile(url, file);
            }
            return true;
        }

        /// <summary>增加无双金币；无实例时回退随机小数额。back 签名为 function(money:int):void。</summary>
        public static void addMoney(Delegate back)
        {
            void addMoneyBack(object money)
            {
                int mm = Int(money);
                if (mm < 1)
                {
                    return;
                }
                if (mm > 5000)
                {
                    return;
                }
                Call(back, mm);
            }

            if (instance != null)
            {
                instance.addMusouMoney((Action<object>)addMoneyBack);
                return;
            }
            addMoneyBack(100 + random() * 500);
        }
    }
}
