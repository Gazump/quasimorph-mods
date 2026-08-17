using HarmonyLib;
using MGSC;

namespace LootTracker
{
    [HarmonyPatch(typeof(ItemSlot))]
    internal static class ItemSlotPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemSlot.Initialize))]
        public static void AfterInitialize(ItemSlot __instance, BasePickupItem item)
        {
            Marker.Apply(__instance, item);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemSlot.InitializeEmpty))]
        public static void AfterInitializeEmpty(ItemSlot __instance)
        {
            Marker.Apply(__instance, null);
        }
    }
}
