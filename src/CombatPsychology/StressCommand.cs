using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    [ConsoleCommand(new string[] { "psy_stress" })]
    public class StressCommand
    {
        [Inject(false)]
        private readonly Creatures _creatures = null;

        public static string Help(string command, bool verbose)
        {
            return "Set player stress to an exact level. Syntax: psy_stress <0-100>. 0 removes it.";
        }

        public string Execute(string[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                return "Usage: psy_stress <0-100>";
            }
            int level = Mathf.Clamp(ParseHelper.ParseInt(tokens[0]), 0, 100);
            Player player = _creatures.Player;
            EffectsController effectsController = player.CreatureData.EffectsController;
            StatusEffect statusEffect = StressSystem.Find(player);
            if (statusEffect == null)
            {
                if (level <= 0)
                {
                    return "stress already at 0";
                }
                effectsController.Add(new StatusEffect(PsyConfig.StressId, level), merge: false);
            }
            else if (level <= 0)
            {
                effectsController.Remove(statusEffect.ID);
            }
            else
            {
                statusEffect.ApplyLevelChange(level, force: true, updateStage: true);
                effectsController.SetEffectDirty(statusEffect);
            }
            return $"stress set to {level} (fortitude {StressSystem.GetFortitude(player)}, gain x{StressSystem.GetGainMultiplier(player):0.00})";
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
