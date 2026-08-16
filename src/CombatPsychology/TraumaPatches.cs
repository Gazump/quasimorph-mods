using HarmonyLib;
using MGSC;

namespace CombatPsychology
{
    /// <summary>Raid debrief: runs BEFORE the game wipes the merc's effects controller, so
    /// exit stress, wounds and amputations are still readable.</summary>
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.RestoreStateAfterMission))]
    internal static class RestoreStateAfterMission_Patch
    {
        private static void Prefix(Mercenary mercenary)
        {
            TraumaSystem.ProcessRaidReturn(mercenary);
            DebriefNotifier.Notify();
        }
    }

    /// <summary>Death path (RestoreStateAfterMission is not called for the dead): the clone
    /// remembers dying.</summary>
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.DropOnDeathPenalty))]
    internal static class DropOnDeathPenalty_Patch
    {
        private static void Postfix(Mercenary merc)
        {
            TraumaSystem.ProcessDeath(merc);
            DebriefNotifier.Notify();
        }
    }
}
