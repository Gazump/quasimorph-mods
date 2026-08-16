using HarmonyLib;
using MGSC;

namespace CombatPsychology
{
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.RestoreStateAfterMission))]
    internal static class RestoreStateAfterMission_Patch
    {
        private static void Prefix(Mercenary mercenary)
        {
            TraumaSystem.ProcessRaidReturn(mercenary);
            DebriefNotifier.Notify();
        }
    }

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
