using System.Collections.Generic;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    [ConsoleCommand(new string[] { "rogue_start" })]
    public class RogueStartCommand
    {
        [Inject(false)]
        private readonly GameModeStateMachine _stateMachine = null;

        public static string Help(string command, bool verbose)
        {
            return "Start a roguelike run from the main menu. Syntax: rogue_start [character 0-2] [random] [easy|normal|hard]";
        }

        public string Execute(string[] tokens)
        {
            if (_stateMachine == null)
            {
                return "game mode state machine unavailable";
            }
            State state = AccessTools.Field(typeof(GameModeStateMachine), "_state").GetValue(_stateMachine) as State;
            if (state.Get<MainMenuGameMode>() == null)
            {
                return "rogue_start only works from the main menu";
            }
            if (RogueRun.Active)
            {
                return "a roguelike run is already active";
            }
            int candidate = 0;
            bool daily = true;
            RogueTier tier = RogueTier.Normal;
            if (tokens != null)
            {
                foreach (string token in tokens)
                {
                    if (token == "random")
                    {
                        daily = false;
                    }
                    else if (token == "easy")
                    {
                        tier = RogueTier.Easy;
                    }
                    else if (token == "normal")
                    {
                        tier = RogueTier.Normal;
                    }
                    else if (token == "hard")
                    {
                        tier = RogueTier.Hard;
                    }
                    else
                    {
                        candidate = Mathf.Clamp(ParseHelper.ParseInt(token), 0, 2);
                    }
                }
            }
            RogueRun.PrepareDay(daily);
            RogueRunner.Get(state).BeginRun(candidate, tier);
            return $"starting {(daily ? "daily run " + RogueRun.DayLabel : "random run")} with character {candidate} on {tier}";
        }

        public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
        {
            return new List<string> { "0", "1", "2", "random", "easy", "normal", "hard" };
        }

        public static bool IsAvailable()
        {
            return true;
        }

        public static bool ShowInHelpAndAutocomplete()
        {
            return true;
        }
    }

    [ConsoleCommand(new string[] { "rogue_abort" })]
    public class RogueAbortCommand
    {
        public static string Help(string command, bool verbose)
        {
            return "Abort the active roguelike run and return to the main menu.";
        }

        public string Execute(string[] tokens)
        {
            if (!RogueRun.Active)
            {
                return "no active roguelike run";
            }
            DungeonGameMode dungeon = SingletonMonoBehaviour<DungeonGameMode>.Instance;
            if (dungeon == null)
            {
                return "no dungeon running";
            }
            dungeon.ForceGameOver();
            return "aborting run";
        }

        public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
        {
            return null;
        }

        public static bool IsAvailable()
        {
            return true;
        }

        public static bool ShowInHelpAndAutocomplete()
        {
            return true;
        }
    }

    [ConsoleCommand(new string[] { "rogue_info" })]
    public class RogueInfoCommand
    {
        public static string Help(string command, bool verbose)
        {
            return "Show the state of the roguelike run and today's character candidates.";
        }

        public string Execute(string[] tokens)
        {
            if (string.IsNullOrEmpty(RogueRun.DayLabel))
            {
                RogueRun.PrepareDay(daily: true);
            }
            List<(string profileId, string classId)> candidates = RogueRunner.GetDailyCandidates();
            string text = $"day {RogueRun.DayLabel}, seed {RogueRun.DaySeed:X8}, active {RogueRun.Active}, floor {RogueRun.CurrentFloor}/{RogueConfig.FloorCount}";
            for (int i = 0; i < candidates.Count; i++)
            {
                text += $"\n  [{i}] {candidates[i].profileId} ({candidates[i].classId})";
            }
            return text;
        }

        public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
        {
            return null;
        }

        public static bool IsAvailable()
        {
            return true;
        }

        public static bool ShowInHelpAndAutocomplete()
        {
            return true;
        }
    }
}
