using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameEvent : Event
    {
        public const string SCORE_UPDATE = "SCORE_UPDATE";
        public const string PAUSE_GAME = "PAUSE_GAME";
        public const string PAUSE_GAME_MENU = "PAUSE_GAME_MENU";
        public const string RESUME_GAME = "RESUME_GAME";
        public const string LOADING_GAME = "LOADING_GAME";
        public const string LOAD_GAME_START = "LOAD_GAME_START";
        public const string LOAD_GAME_COMPLETE = "LOAD_GAME_COMPLETE";
        public const string GAME_START = "GAME_START";
        public const string ROUND_START = "ROUND_START";
        public const string ROUND_END = "ROUND_END";
        public const string GAME_END = "GAME_END";
        public const string GAME_OVER = "GAME_OVER";
        public const string GAME_OVER_CONTINUE = "GAME_OVER_CONTINUE";
        public const string GAME_PASS_ALL = "GAME_PASS_ALL";
        public const string SELECT_FIGHTER = "SELECT_FIGHTER";
        public const string SELECT_MAP = "SELECT_MAP";
        public const string SELECT_FIGHTER_STEP = "SELECT_FIGHTER_STEP";
        public const string SELECT_FIGHTER_FINISH = "SELECT_FIGHTER_FINISH";
        public const string SELECT_FIGHTER_INDEX = "SELECT_FIGHTER_INDEX";
        public const string MONEY_UPDATE = "MONEY_UPDATE";
        public const string LEVEL_UP = "LEVEL_UP";
        public const string SELECT_FIGHTER_INDEX_FINISH = "SELECT_FIGHTER_INDEX_FINISH";
        public const string ENTER_MUSOU_STAGE = "ENTER_MUSOU_STAGE";
        public const string ENTER_TEAM_STAGE = "ENTER_TEAM_STAGE";
        public const string ENTER_SINGLE_STAGE = "ENTER_SINGLE_STAGE";
        public const string ENTER_TRAIN_STAGE = "ENTER_TRAIN_STAGE";
        public const string ENTER_STAGE = "ENTER_STAGE";
        public const string MORE_GAMES = "MORE_GAMES";
        public const string WINNER_SHOW = "SHOW_WINNER";
        public const string WINNER_END = "WINNER_END";
        public const string MUSOU_MAP = "MUSOU_MAP";
        public const string MUSOU_BACK_MAP = "MUSOU_BACK_MAP";
        public const string MUSOU_FIGHTER = "MUSOU_FIGHTER";
        public const string MUSOU_FIGHTER_CLOSE = "MUSOU_FIGHTER_CLOSE";
        public const string MUSOU_FIGHTER_UPDATE = "MUSOU_FIGHTER_UPDATE";
        public const string MUSOU_LOADING_START = "MUSOU_LOADING_START";
        public const string MUSOU_LOADING = "MUSOU_LOADING";
        public const string MUSOU_LOADING_FINISH = "MUSOU_LOADING_FINISH";
        public const string MUSOU_MISSION_START = "MUSOU_MISSION_START";
        public const string MUSOU_MISSION_FINISH = "MUSOU_MISSION_FINISH";
        public const string FIGHT_LOADING_START = "FIGHT_LOADING_START";
        public const string FIGHT_LOADING = "FIGHT_LOADING";
        public const string FIGHT_LOADING_FINISH = "FIGHT_LOADING_FINISH";
        public const string FIGHT_START = "FIGHT_START";
        public const string CONFRIM_BACK_MENU = "CONFRIM_BACK_MENU";
        public const string CONFRIM_MUSOU_NEXT_MISSION = "CONFRIM_MUSOU_NEXT_MISSION";
        public const string UI_CONFRIM = "UI_CONFRIM";
        public const string UI_CONFRIM_CLOSE = "UI_CONFRIM_CLOSE";
        public const string UI_ALERT = "UI_ALERT";
        public const string UI_ALERT_CLOSE = "UI_ALERT_CLOSE";

        private static EventDispatcher _dispatcher = new EventDispatcher();

        // untyped (Function) listeners are wrapped; the wrapper is kept so removeEventListener can find it
        private static readonly Dictionary<string, Dictionary<Delegate, Action<Event>>> _wrapped = new Dictionary<string, Dictionary<Delegate, Action<Event>>>();

        public static void dispatchEvent(string type, object param = null, bool bubbles = false, bool cancelable = false)
        {
            //			trace("dispatch GameEvent ::", type, param);
            _dispatcher.dispatchEvent(new GameEvent(type, param, bubbles, cancelable));
        }

        public static void addEventListener(string type, Action<Event> listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
        {
            _dispatcher.addEventListener(type, listener, useCapture, priority, useWeakReference);
        }

        public static void addEventListener<T>(string type, Action<T> listener, bool useCapture = false, int priority = 0, bool useWeakReference = false) where T : Event
        {
            _dispatcher.addEventListener<T>(type, listener, useCapture, priority, useWeakReference);
        }

        public static void addEventListener(string type, Action listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
        {
            _dispatcher.addEventListener(type, listener, useCapture, priority, useWeakReference);
        }

        /// <summary>AS3 Function listener (any delegate / script function).</summary>
        public static void addEventListener(string type, object listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
        {
            switch (listener)
            {
                case null: return;
                case Action<Event> a1: addEventListener(type, a1, useCapture, priority, useWeakReference); return;
                case Action a0: addEventListener(type, a0, useCapture, priority, useWeakReference); return;
            }
            if (!_wrapped.TryGetValue(type, out var map)) _wrapped[type] = map = new Dictionary<Delegate, Action<Event>>();
            var key = listener as Delegate;
            if (key == null)
            {
                // script function: wrap without removal support by identity
                _dispatcher.addEventListener(type, (Action<Event>)(e => Call(listener, e)), useCapture, priority, useWeakReference);
                return;
            }
            if (map.ContainsKey(key)) return;
            Action<Event> w = e => Call(key, e);
            map[key] = w;
            _dispatcher.addEventListener(type, w, useCapture, priority, useWeakReference);
        }

        public static void removeEventListener(string type, Action<Event> listener, bool useCapture = false)
        {
            _dispatcher.removeEventListener(type, listener, useCapture);
        }

        public static void removeEventListener<T>(string type, Action<T> listener, bool useCapture = false) where T : Event
        {
            _dispatcher.removeEventListener<T>(type, listener, useCapture);
        }

        public static void removeEventListener(string type, Action listener, bool useCapture = false)
        {
            _dispatcher.removeEventListener(type, listener, useCapture);
        }

        public static void removeEventListener(string type, object listener, bool useCapture = false)
        {
            var key = listener as Delegate;
            if (key == null) return;
            if (_wrapped.TryGetValue(type, out var map) && map.TryGetValue(key, out var w))
            {
                map.Remove(key);
                _dispatcher.removeEventListener(type, w, useCapture);
                return;
            }
            _dispatcher.removeEventListener(type, key, useCapture);
        }

        public GameEvent(string type, object param = null, bool bubbles = false, bool cancelable = false) : base(type, bubbles, cancelable)
        {
            this.param = param;
        }

        public object param;
    }
}
