using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Game mode ids plus category queries based on <c>currentMode</c>.</summary>
    public class GameMode
    {
        /// <summary>Team arcade</summary>
        public const int TEAM_ARCADE = 10;
        /// <summary>Team 2P versus</summary>
        public const int TEAM_VS_PEOPLE = 11;
        /// <summary>Team versus CPU</summary>
        public const int TEAM_VS_CPU = 12;
        /// <summary>Team watch</summary>
        public const int TEAM_WATCH = 13;

        /// <summary>Single arcade</summary>
        public const int SINGLE_ARCADE = 20;
        /// <summary>Single 2P versus</summary>
        public const int SINGLE_VS_PEOPLE = 21;
        /// <summary>Single versus CPU</summary>
        public const int SINGLE_VS_CPU = 22;
        /// <summary>Single watch</summary>
        public const int SINGLE_WATCH = 23;

        /// <summary>Survivor (unused)</summary>
        public const int SURVIVOR = 30;

        /// <summary>Training</summary>
        public const int TRAINING = 40;

        /// <summary>Musou adventure</summary>
        public const int MUSOU_ARCADE = 100;

        /// <summary>Current mode (written by the entry when switching modes).</summary>
        public static int currentMode;

        /// <summary>All team modes (team arcade, vs CPU, vs people, watch, musou).</summary>
        private static readonly List<int> _isTeamModes = new List<int>
        {
            TEAM_ARCADE,
            TEAM_VS_CPU,
            TEAM_VS_PEOPLE,
            TEAM_WATCH,
            MUSOU_ARCADE
        };

        /// <summary>All single modes.</summary>
        private static readonly List<int> _isSingleModes = new List<int>
        {
            SINGLE_ARCADE,
            SINGLE_VS_CPU,
            SINGLE_VS_PEOPLE,
            SINGLE_WATCH
        };

        /// <summary>All 2P versus modes.</summary>
        private static readonly List<int> _isVsPeopleModes = new List<int>
        {
            TEAM_VS_PEOPLE,
            SINGLE_VS_PEOPLE
        };

        /// <summary>All versus-CPU modes (including watch).</summary>
        private static readonly List<int> _isVsCPUModes = new List<int>
        {
            TEAM_VS_CPU,
            TEAM_WATCH,
            SINGLE_VS_CPU,
            SINGLE_WATCH
        };

        /// <summary>All watch modes.</summary>
        private static readonly List<int> _isWatchModes = new List<int>
        {
            TEAM_WATCH,
            SINGLE_WATCH
        };

        /// <summary>All arcade modes (team arcade, single arcade, survivor).</summary>
        private static readonly List<int> _isArcadeModes = new List<int>
        {
            TEAM_ARCADE,
            SINGLE_ARCADE,
            SURVIVOR
        };

        /// <summary>Default team list: objects with <c>id</c> / <c>name</c>.</summary>
        public static List<object> getAllTeams()
        {
            return new List<object>
            {
                new ASObject
                {
                    ["id"] = TeamID.TEAM_1,
                    ["name"] = "P1"
                },
                new ASObject
                {
                    ["id"] = TeamID.TEAM_2,
                    ["name"] = "P2"
                }
            };
        }

        public static bool isTeamMode()
        {
            return _isTeamModes.IndexOf(currentMode) != -1;
        }

        public static bool isSingleMode()
        {
            return _isSingleModes.IndexOf(currentMode) != -1;
        }

        public static bool isVsPeople()
        {
            return _isVsPeopleModes.IndexOf(currentMode) != -1;
        }

        /// <summary>Versus CPU; training counts unless <paramref name="hasTraining"/> is false.</summary>
        public static bool isVsCPU(bool hasTraining = true)
        {
            return (currentMode == TRAINING && hasTraining) ||
                   _isVsCPUModes.IndexOf(currentMode) != -1;
        }

        public static bool isWatch()
        {
            return _isWatchModes.IndexOf(currentMode) != -1;
        }

        public static bool isArcade()
        {
            return _isArcadeModes.IndexOf(currentMode) != -1;
        }
    }
}
