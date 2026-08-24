using HarmonyLib;
using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    [HarmonyPatch(typeof(InventoryScreen), "DragControllerShowContextMenuCallback")]
    public static class InstallMenuPatch
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
            if (item.Record<ImplantRecord>() != null)
            {
                UI.Get<CommonContextMenu>().SetupCommand("Install implant", RogueConfig.InstallImplantContextBind);
            }
            else if (item.Record<AugmentationRecord>() != null)
            {
                UI.Get<CommonContextMenu>().SetupCommand("Install prosthetic", RogueConfig.InstallProstheticContextBind);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryScreen), "ContextMenuOnCmdSelected")]
    public static class InstallCommandPatch
    {
        public static void Postfix(InventoryScreen __instance, int bindValue)
        {
            if (!RogueRun.Active || (bindValue != RogueConfig.InstallImplantContextBind && bindValue != RogueConfig.InstallProstheticContextBind))
            {
                return;
            }
            try
            {
                Execute(__instance, bindValue);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Install failed: " + ex);
            }
        }

        private static void Execute(InventoryScreen screen, int bindValue)
        {
            ItemSlot slot = AccessTools.Field(typeof(InventoryScreen), "_contextMenuItemSlot").GetValue(screen) as ItemSlot;
            BasePickupItem item = slot?.Item;
            if (item == null)
            {
                return;
            }
            DungeonGameMode dungeon = SingletonMonoBehaviour<DungeonGameMode>.Instance;
            Mercenary mercenary = dungeon?.Get<Mercenaries>()?.MercenaryInRaid;
            if (mercenary == null)
            {
                return;
            }
            ItemStorage spill = new ItemStorage(ItemStorageSource.ShipCargo, 10, 6);
            string feedback;
            if (bindValue == RogueConfig.InstallImplantContextBind)
            {
                string itemName = Localization.Get("item." + item.Id + ".name");
                int before = AugmentationSystem.GetInstalledImplantCount(mercenary.CreatureData);
                AugmentationSystem.Implant(mercenary, item, spill, dungeon.Get<PerkFactory>());
                bool installed = AugmentationSystem.GetInstalledImplantCount(mercenary.CreatureData) > before || !spill.Empty;
                feedback = installed ? ("Implant installed: " + itemName) : "No compatible socket for that implant.";
            }
            else
            {
                string itemName = Localization.Get("item." + item.Id + ".name");
                AugmentationSystem.Augment(mercenary, item, spill, dungeon.Get<SpaceTime>(), dungeon.Get<MagnumCargo>());
                feedback = "Prosthetic installed: " + itemName;
            }
            ReturnSpill(dungeon, mercenary, spill);
            screen.RefreshItemsList();
            SingletonMonoBehaviour<TooltipFactory>.Instance.ShowSimpleTextTooltip(feedback);
            Debug.Log("[RoguelikeMode] " + feedback);
        }

        private static void ReturnSpill(DungeonGameMode dungeon, Mercenary mercenary, ItemStorage spill)
        {
            ItemsOnFloor itemsOnFloor = dungeon.Get<ItemsOnFloor>();
            MapGrid mapGrid = dungeon.Get<MapGrid>();
            Creatures creatures = dungeon.Get<Creatures>();
            while (!spill.Empty)
            {
                BasePickupItem removed = spill.First;
                spill.Remove(removed);
                if (!mercenary.CreatureData.Inventory.TakeOrEquip(removed))
                {
                    if (creatures?.Player != null && itemsOnFloor != null && mapGrid != null)
                    {
                        ItemOnFloorSystem.SpawnItem(itemsOnFloor, mapGrid, removed, creatures.Player.CreatureData.Position);
                    }
                }
            }
        }
    }
}
