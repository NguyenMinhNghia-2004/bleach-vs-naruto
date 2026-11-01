using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameConfig
    {
        // 调试模式
        public static bool DEBUG_MODE = false;
        // 触屏模式
        public static bool TOUCH_MODE = false;
        // 触屏返回按钮素材（由 SHELL_Mob 注入 Embed Class）
        public static object TOUCH_BACK_BTN;

        ///////////////////////////////////////////////////////////////////////////

        // 重力
        public const double G = 12;
        // 重力加速度
        public const double G_ADD = 1.2;
        // 在地面上的重力
        public const double G_ON_FLOOR = 4;

        ///////////////////////////////////////////////////////////////////////////

        // 游戏尺寸
        public static readonly Point GAME_SIZE = new Point(800, 600);
        // 游戏与场景之间的比例
        public static Point GAME_SCALE = new Point(1, 1);

        ///////////////////////////////////////////////////////////////////////////

        // 游戏帧率
        public static int FPS_GAME = 60;
        // UI界面帧率
        public const int FPS_UI = 30;
        // 动画帧率
        public const int FPS_ANIMATE = 30;
        // 闪光效果的FPS
        public static int FPS_SHINE_EFFECT = 15;

        ///////////////////////////////////////////////////////////////////////////

        public const double BULLET_HOLD_FRAME_PLUS = 2; //发射的波，子弹的持续时间比率
        public const int JUMP_DELAY_FRAME = 2; //跳时，停留在起跳时的帧数，延时(动画FPS)
        public const int JUMP_DELAY_FRAME_AIR = 1; //跳时，停留在起跳时的帧数，延时(动画FPS)
        public const double JUMP_DAMPING = 0.5; //跳跃阻尼
        public const int HURT_JUMP_FRAME = 15; //击飞时，在空中停留的最短时间(动画FPS)
        public const int HURT_GAP_FRAME = 4; //受攻击的间隔(无敌时间 动画FPS)
        public const int BREAK_DEF_GAP_FRAME = 4; //破防时的无敌时间(动画FPS)
        public const int BREAK_DEF_DOWN_GAP_FRAME = 10; //破防时的无敌时间(击飞)(动画FPS)
        public const int BREAK_DEF_HOLD_FRAME = 42; //破防时的停顿(动画FPS) (0.7 * 60)
        public const int DEFENSE_GAP_FRAME = 4; //防御攻击的间隔(动画FPS)
        public const double DEFENSE_DAMPING_X = 1; //被打时的速度阻尼X
        public const int DEFENSE_GAP_FRAME_DOWN = 8; //防御击倒型攻击的间隔(动画FPS)
        public const double DEFENSE_LOSE_HP_RATE = 0.05; //防御攻击时的受伤比率
        public const int DEFENSE_HOLD_FRAME_MIN = 5; //防御击倒型攻击的间隔(动画FPS)
        public const int DEFENSE_HOLD_FRAME_MAX = 10; //防御击倒型攻击的间隔(动画FPS)
        public const int DEFENSE_HOLD_FRAME_DOWN = 10; //防御击倒型攻击的间隔(动画FPS)
        public const double HURT_DAMPING_X = 0.1; //被打时的速度阻尼X
        public const double HURT_DAMPING_Y = 0.5; //被打时的速度阻尼Y
        public const double HURT_Y_ADD_INAIR = 3; //被打时，在空中时，被打到空中时的Y增加
        public const double HURT_Y_ADD = 6; //被打到空中时的Y增加
        public const int STEEL_HURT_GAP_FRAME = 4; //刚身受攻击的间隔(无敌时间 动画FPS)
        public const int STEEL_HURT_DOWN_GAP_FRAME = 10; //刚身受攻击(击飞)的间隔(无敌时间 动画FPS)
        public const double STEEL_HURT_HP_PERCENT = 0.65; //刚身受攻击的损血百分比
        public const double STEEL_SUPER_HURT_HP_PERCENT = 0.3; //刚身(超级)受攻击的损血百分比
        public const double HURT_FLY_DAMPING_X = 0; //被击飞时的速度阻尼X
        public const double HURT_FLY_DAMPING_Y = 0.5; //被击飞时的速度阻尼Y
        public const double HIT_FLOOR_DAMPING_X = 2; //被击飞时的，接触到地面的阻尼X
        public const double HIT_FLOOR_DAMPING_X_HEAVY = 4; //被重击地面时，接触到地面的阻尼X
        public const double HIT_FLOOR_TAN_Y_MIN = 3; //被击飞时的，接触到地面弹起的Y，最小值
        public const double HIT_FLOOR_TAN_Y_MAX = 8; //被击飞时的，接触到地面弹起的Y，最大值
        public const int HIT_DOWN_FRAME = 15; //在地面躺上时间
        public const int HIT_DOWN_FRAME_HEAVY = 30; //重击到地面时，躺的时间
        public const int HIT_DOWN_BY_HITY = 5; //当hity大于该值时，执行直接击倒，重击效果
        public const int NO_TOUCH_BAN_ON_VECY = 5; //当vecY大于该值时，不能站在空中的板上
        public const int X_SIDE_OFFSET = 10; //角色边界距离补正，像素
        public const int HURT_DOWN_JUMP_FRAME = 20; //倒地起身的帧数
        public const int HURT_DOWN_JUMP_DAMPING = 1; //倒地起身的速度减值

        ///////////////////////////////////////////////////////////////////////////

        // 使用能量后，等待一定时间后自动加能量（秒）
        public const double USE_ENERGY_CD = 0.8;
        // 每一帧增加的能量
        public const double ENERGY_ADD_NORMAL = 2;
        // 防御时每一帧增加的能量
        public const double ENERGY_ADD_DEFENSE = 0.8;
        // 攻击时每一帧增加的能量
        public const double ENERGY_ADD_ATTACKING = 1.1;
        // 每一帧增加的能量(超载)
        public const double ENERGY_ADD_OVER_LOAD_PERFRAME = 0.6;
        // 体力到达多少时，恢复超载
        public const double ENERGY_ADD_OVER_LOAD_RESUME = 30;
        // 受到破防攻击时候，耐力减少的比例
        public const double ENERGY_LOSE_DEFENSE_BREAK_RATE = 0.9;

        ///////////////////////////////////////////////////////////////////////////

        // 0.02 必杀攻击到对方的气增量
        public const double QI_ADD_HIT_BISHA_RATE = 0;
        // 攻击到对方的气增量
        public const double QI_ADD_HIT_RATE = 0.17;
        // 攻击到对方的气增量
        public const double QI_ADD_HIT_BULLET_RATE = 0.1;
        // 攻击到对方的气增量
        public const double QI_ADD_HIT_ATTACKER_RATE = 0.13;
        // 辅助到对方的气增量
        public const double QI_ADD_HIT_ASSISTER_RATE = 0.15;
        // 攻击到对方的气增加最大值
        public const double QI_ADD_HIT_MAX = 15;
        // 受到对方攻击时的气增量
        public const double QI_ADD_HURT_RATE = 0.08;
        // 受到对方攻击时的气增加最大值
        public const double QI_ADD_HURT_MAX = 20;
        // 每一帧增加的辅助气量
        public const double FUZHU_QI_ADD_PERFRAME = 0.2;

        ///////////////////////////////////////////////////////////////////////////

        public const double CAMERA_TWEEN_SPD = 2.5;
        public const int FIGHTER_HP_MAX = 1000;

        public static string QUALITY_UI = StageQuality.LOW; //UI界面画质
        public static string QUALITY_GAME = StageQuality.LOW; //游戏界面画质
        public static bool SHOW_HOW_TO_PLAY = true;
        public static bool ALLOW_SHOW_HP_TEXT = true;
        public static double SPEED_PLUS = SPEED_PLUS_DEFAULT; //速度比率
        public static int HURT_FRAME_OFFSET = 3; //受击时间补正，帧数
        public static int MAP_LOGO_STATE = MapLogoState.HIDE; //是否显示地图中的LOGO 0=不显示，1=显示
        public static int SHOW_UI_STATUS = 0; // UI 布局模式：0 默认；1 紧凑（选人顺序栏/战斗 HUD 等）

        //默认速度比率
        public static double SPEED_PLUS_DEFAULT => (double)FPS_ANIMATE / FPS_GAME;

        public static void setGameFps(int v)
        {
            FPS_GAME = v;
            SPEED_PLUS = SPEED_PLUS_DEFAULT;
            //			HURT_FRAME_OFFSET = v == 30 ? 4 : 3;
        }
    }
}
