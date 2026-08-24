using HarmonyLib;
using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    [HarmonyPatch(typeof(InventoryScreen), "DragControllerShowContextMenuCallback")]
    public static class LearnClassMenuPatch
    {
        public static void Prefix(ItemSlot obj)
        {
            if (!RogueRun.Active || obj?.Item == null)
            {
                return;
            }
            BasePickupItem item = obj.Item;
            if (item.Locked || item.IsImplicit)
            {
                return;
            }
            string classId = GetLearnableClassId(item);
            if (classId == null)
            {
                return;
            }
            string caption = "Learn " + Localization.Get("class." + classId + ".name");
            UI.Get<CommonContextMenu>().SetupCommand(caption, RogueConfig.LearnClassContextBind);
        }

        internal static string GetLearnableClassId(BasePickupItem item)
        {
            DatadiskRecord record = item.Record<DatadiskRecord>();
            if (record == null || record.UnlockType != DatadiskUnlockType.MercenaryClass)
            {
                return null;
            }
            DatadiskComponent component = item.Comp<DatadiskComponent>();
            if (component == null || string.IsNullOrEmpty(component.UnlockId))
            {
                return null;
            }
            if (Data.MercenaryClasses.GetRecord(component.UnlockId) == null)
            {
                return null;
            }
            DungeonGameMode dungeon = SingletonMonoBehaviour<DungeonGameMode>.Instance;
            Mercenary mercenary = dungeon?.Get<Mercenaries>()?.MercenaryInRaid;
            if (mercenary == null || mercenary.MercClassId == component.UnlockId)
            {
                return null;
            }
            return component.UnlockId;
        }
    }

    [HarmonyPatch(typeof(InventoryScreen), "ContextMenuOnCmdSelected")]
    public static class LearnClassCommandPatch
    {
        public static void Postfix(InventoryScreen __instance, int bindValue)
        {
            if (bindValue != RogueConfig.LearnClassContextBind || !RogueRun.Active)
            {
                return;
            }
            ItemSlot slot = AccessTools.Field(typeof(InventoryScreen), "_contextMenuItemSlot").GetValue(__instance) as ItemSlot;
            BasePickupItem item = slot?.Item;
            if (item == null)
            {
                return;
            }
            string classId = LearnClassMenuPatch.GetLearnableClassId(item);
            if (classId == null)
            {
                return;
            }
            DungeonGameMode dungeon = SingletonMonoBehaviour<DungeonGameMode>.Instance;
            Mercenaries mercenaries = dungeon.Get<Mercenaries>();
            Mercenary mercenary = mercenaries.MercenaryInRaid;
            MercenarySystem.ApplyClassForMercenary(dungeon.Get<MagnumProjects>(), mercenary, classId, dungeon.Get<PerkFactory>());
            if (!mercenaries.UnlockedClasses.Contains(classId))
            {
                mercenaries.UnlockedClasses.Add(classId);
            }
            if (item.StackCount > 1)
            {
                item.StackCount--;
            }
            else
            {
                item.Storage?.Remove(item);
            }
            __instance.RefreshItemsList();
            SingletonMonoBehaviour<TooltipFactory>.Instance.ShowSimpleTextTooltip("Class learned: " + Localization.Get("class." + classId + ".name"));
            Debug.Log($"[RoguelikeMode] Learned class {classId} from chip.");
        }
    }
}
