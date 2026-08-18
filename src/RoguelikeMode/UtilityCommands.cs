using System.Collections;
using System.Collections.Generic;
using MGSC;
using UnityEngine;
using UnityEngine.Networking;

namespace RoguelikeMode
{
    [ConsoleCommand(new string[] { "rogue_scores" })]
    public class RogueScoresCommand
    {
        public static string Help(string command, bool verbose)
        {
            return "Show the local roguelike high-score table.";
        }

        public string Execute(string[] tokens)
        {
            RogueScoreStore store = ScoreSystem.Load();
            if (store.Entries.Count == 0)
            {
                return "no runs recorded yet";
            }
            string text = "rank | score | floor | kills | turns | tier | day | merc";
            int count = Mathf.Min(10, store.Entries.Count);
            for (int i = 0; i < count; i++)
            {
                RogueScoreEntry e = store.Entries[i];
                string mode = e.Daily ? e.Day : "random";
                text += $"\n{i + 1,4} | {e.Score,5} | {e.Floor,5} | {e.Kills,5} | {e.Turns,5} | {(RogueTier)e.Tier} | {mode} | {e.ProfileId}{(e.Victory ? " (WON)" : "")}{(e.Modded ? " [modded]" : "")}";
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

    [ConsoleCommand(new string[] { "rogue_goto" })]
    public class RogueGotoCommand
    {
        [Inject(false)]
        private readonly GameModeStateMachine _stateMachine = null;

        public static string Help(string command, bool verbose)
        {
            return "Debug: jump to a floor of the active roguelike run. Syntax: rogue_goto <1-" + RogueConfig.FloorCount + ">";
        }

        public string Execute(string[] tokens)
        {
            if (!RogueRun.Active)
            {
                return "no active roguelike run";
            }
            if (SingletonMonoBehaviour<DungeonGameMode>.Instance == null)
            {
                return "not in a dungeon";
            }
            if (tokens == null || tokens.Length == 0)
            {
                return "Usage: rogue_goto <1-" + RogueConfig.FloorCount + ">";
            }
            int floor = Mathf.Clamp(ParseHelper.ParseInt(tokens[0]), 1, RogueConfig.FloorCount);
            State state = HarmonyLib.AccessTools.Field(typeof(GameModeStateMachine), "_state").GetValue(_stateMachine) as State;
            RogueRunner.Get(state).JumpToFloor(floor);
            return "jumping to floor " + floor;
        }

        public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
        {
            return new List<string> { "10" };
        }

        public static bool IsAvailable()
        {
            return RogueRun.Active;
        }

        public static bool ShowInHelpAndAutocomplete()
        {
            return true;
        }
    }

    [ConsoleCommand(new string[] { "rogue_httptest" })]
    public class RogueHttpTestCommand
    {
        public static string Help(string command, bool verbose)
        {
            return "Test outbound HTTPS from the game process (leaderboard prerequisite). Syntax: rogue_httptest [url]";
        }

        public string Execute(string[] tokens)
        {
            string url = (tokens != null && tokens.Length > 0) ? tokens[0] : "https://api.github.com/zen";
            SingletonMonoBehaviour<CoroutineRunner>.Instance.StartCoroutine(Fetch(url));
            return "request sent to " + url + " - result will appear in Player.log with tag [RoguelikeMode]";
        }

        private static IEnumerator Fetch(string url)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string body = request.downloadHandler.text ?? string.Empty;
                    if (body.Length > 200)
                    {
                        body = body.Substring(0, 200) + "...";
                    }
                    Debug.Log($"[RoguelikeMode] HTTPS OK ({request.responseCode}): {body}");
                }
                else
                {
                    Debug.LogError($"[RoguelikeMode] HTTPS FAILED: {request.result} - {request.error}");
                }
            }
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
