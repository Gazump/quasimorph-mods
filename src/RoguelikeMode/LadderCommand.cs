using System.Collections.Generic;
using MGSC;

namespace RoguelikeMode
{
    [ConsoleCommand(new string[] { "rogue_ladder" })]
    public class RogueLadderCommand
    {
        public static string Help(string command, bool verbose)
        {
            return "Online daily ladder. Syntax: rogue_ladder [on|off|status|board [easy|normal|hard]|retry|endpoint <url>]";
        }

        public string Execute(string[] tokens)
        {
            string action = (tokens != null && tokens.Length > 0) ? tokens[0].ToLowerInvariant() : "status";
            switch (action)
            {
                case "on":
                    LadderConfig.SetEnabled(true);
                    return "Ladder submission ON. Finished daily dives will send your Steam name, Steam id and run stats to " + LadderConfig.Endpoint;
                case "off":
                    LadderConfig.SetEnabled(false);
                    return "Ladder submission OFF. Nothing will be sent.";
                case "endpoint":
                    if (tokens.Length < 2)
                    {
                        return "current endpoint: " + LadderConfig.Endpoint;
                    }
                    LadderConfig.SetEndpoint(tokens[1]);
                    return "endpoint set to " + LadderConfig.Endpoint;
                case "retry":
                    LadderClient.FlushPending();
                    return "retrying any stored submission - watch Player.log for [RoguelikeMode]";
                case "board":
                {
                    RogueTier tier = RogueTier.Normal;
                    if (tokens.Length > 1)
                    {
                        string wanted = tokens[1].ToLowerInvariant();
                        if (wanted == "easy")
                        {
                            tier = RogueTier.Easy;
                        }
                        else if (wanted == "hard")
                        {
                            tier = RogueTier.Hard;
                        }
                    }
                    LadderClient.Fetch(LadderClient.TodayUtc(), tier);
                    return "fetching " + tier + " board for " + LadderClient.TodayUtc() + " - open THE DIVE screen or check Player.log";
                }
                default:
                {
                    string text = "ladder submission: " + (LadderConfig.Enabled ? "ON" : "OFF")
                        + "\nendpoint: " + LadderConfig.Endpoint
                        + "\nmod version: " + LadderConfig.ModVersion
                        + "\nsteam: " + (SteamIdentity.Available ? ("available as " + SteamIdentity.PersonaName) : "unavailable");
                    if (!string.IsNullOrEmpty(LadderClient.LastSubmitResult))
                    {
                        text += "\nlast submission: " + LadderClient.LastSubmitResult;
                    }
                    return text;
                }
            }
        }

        public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
        {
            return new List<string> { "on", "off", "status", "board", "retry", "endpoint" };
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
