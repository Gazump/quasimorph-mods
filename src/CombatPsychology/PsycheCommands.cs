using System.Collections.Generic;
using System.Text;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    internal static class CommandHelpers
    {
        public static Mercenary ResolveMerc(Mercenaries mercenaries, string profileId)
        {
            if (!string.IsNullOrEmpty(profileId))
            {
                foreach (Mercenary value in mercenaries.Values)
                {
                    if (value.ProfileId == profileId)
                    {
                        return value;
                    }
                }
                return null;
            }
            if (mercenaries.MercenaryInRaid != null)
            {
                return mercenaries.MercenaryInRaid;
            }
            if (mercenaries.Values.Count == 1)
            {
                return mercenaries.Values[0];
            }
            return null;
        }

        public static List<string> ProfileIds(Mercenaries mercenaries)
        {
            List<string> list = new List<string>();
            foreach (Mercenary value in mercenaries.Values)
            {
                list.Add(value.ProfileId);
            }
            return list;
        }
    }

    [ConsoleCommand(new string[] { "psy_psyche" })]
    public class PsychePsycheCommand
    {
        [Inject(false)]
        private readonly Mercenaries _mercenaries = null;

        public static string Help(string command, bool verbose)
        {
            return "Lists trauma, scars and streaks for every mercenary.";
        }

        public string Execute(string[] tokens)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Mercenary value in _mercenaries.Values)
            {
                MercPsyche mercPsyche = PsycheStore.Current.Find(value.ProfileId);
                if (mercPsyche == null)
                {
                    stringBuilder.AppendLine(value.ProfileId + ": no psyche record (untouched)");
                    continue;
                }
                stringBuilder.AppendLine($"{value.ProfileId}: trauma {mercPsyche.Trauma}/{TraumaSystem.TraumaMax}, scars [{string.Join(", ", mercPsyche.Scars)}], clean streak {mercPsyche.CleanRaidStreak}, survivors-high pending {mercPsyche.SurvivorsHighPending}");
            }
            return (stringBuilder.Length > 0) ? stringBuilder.ToString() : "no mercenaries";
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

    [ConsoleCommand(new string[] { "psy_trauma" })]
    public class PsycheTraumaCommand
    {
        [Inject(false)]
        private readonly Mercenaries _mercenaries = null;

        public static string Help(string command, bool verbose)
        {
            return "Set a merc's trauma (scars mint at 25/50/75 on the way up). Syntax: psy_trauma <0-100> [profileId]";
        }

        public string Execute(string[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                return "Usage: psy_trauma <0-100> [profileId]";
            }
            Mercenary mercenary = CommandHelpers.ResolveMerc(_mercenaries, (tokens.Length > 1) ? tokens[1] : null);
            if (mercenary == null)
            {
                return "merc not found; pass a profileId (see psy_psyche)";
            }
            int num = Mathf.Clamp(ParseHelper.ParseInt(tokens[0]), 0, TraumaSystem.TraumaMax);
            MercPsyche mercPsyche = PsycheStore.Current.GetOrCreate(mercenary.ProfileId);
            TraumaSystem.ChangeTrauma(mercPsyche, num - mercPsyche.Trauma);
            return $"{mercenary.ProfileId}: trauma {mercPsyche.Trauma}, scars [{string.Join(", ", mercPsyche.Scars)}]";
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

    [ConsoleCommand(new string[] { "psy_scar" })]
    public class PsycheScarCommand
    {
        [Inject(false)]
        private readonly Mercenaries _mercenaries = null;

        public static string Help(string command, bool verbose)
        {
            return "Add/remove scars. Syntax: psy_scar <add|remove|clear> [scarId] [profileId]. Scars: " + string.Join(", ", GetScarIds());
        }

        public string Execute(string[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                return Help("psy_scar", verbose: false);
            }
            string text = tokens[0];
            string scarId = (tokens.Length > 1) ? tokens[1] : null;
            Mercenary mercenary = CommandHelpers.ResolveMerc(_mercenaries, (tokens.Length > 2) ? tokens[2] : null);
            if (mercenary == null)
            {
                return "merc not found; pass a profileId (see psy_psyche)";
            }
            MercPsyche mercPsyche = PsycheStore.Current.GetOrCreate(mercenary.ProfileId);
            switch (text)
            {
            case "add":
                if (ScarCatalog.Get(scarId) == null)
                {
                    return "unknown scar: " + scarId;
                }
                if (!mercPsyche.HasScar(scarId))
                {
                    mercPsyche.Scars.Add(scarId);
                }
                break;
            case "remove":
                mercPsyche.Scars.Remove(scarId);
                break;
            case "clear":
                mercPsyche.Scars.Clear();
                break;
            default:
                return Help("psy_scar", verbose: false);
            }
            return $"{mercenary.ProfileId}: scars [{string.Join(", ", mercPsyche.Scars)}] (applies at next raid start)";
        }

        private static List<string> GetScarIds()
        {
            List<string> list = new List<string>();
            foreach (ScarDef item in ScarCatalog.All)
            {
                list.Add(item.Id);
            }
            return list;
        }

        public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
        {
            if (tokens == null || tokens.Length <= 1)
            {
                return new List<string> { "add", "remove", "clear" };
            }
            if (tokens.Length == 2)
            {
                return GetScarIds();
            }
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
