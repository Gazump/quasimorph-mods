using HarmonyLib;
using MGSC;

namespace Mechs
{
    [HarmonyPatch(typeof(ItemInteractionSystem), nameof(ItemInteractionSystem.UseDevice))]
    public static class UseDevice_Patch
    {
        public static bool Prefix(Creatures creatures, BasePickupItem item)
        {
            if (!(item.Record<DeviceRecord>() is MechRecord))
            {
                return true;
            }
            MechSystem.EnterMech(creatures, item);
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryScreen), nameof(InventoryScreen.Unequip))]
    public static class InventoryScreenUnequip_Patch
    {
        public static bool Prefix(BasePickupItem item)
        {
            if (!MechSystem.IsMechPart(item))
            {
                return true;
            }
            MechSystem.ExitMechInRaid();
            return false;
        }
    }

    [HarmonyPatch(typeof(ItemSlot), "IsDraggable", MethodType.Getter)]
    public static class ItemSlotIsDraggable_Patch
    {
        public static void Postfix(ItemSlot __instance, ref bool __result)
        {
            if (__result && MechSystem.IsMechPart(__instance.Item))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(DragController), nameof(DragController.CanPutInSlot))]
    public static class CanPutInSlot_Patch
    {
        private static readonly AccessTools.FieldRef<DragController, BasePickupItem> _draggableItem =
            AccessTools.FieldRefAccess<DragController, BasePickupItem>("_draggableItem");

        public static void Postfix(DragController __instance, ItemSlot slot, ref bool __result)
        {
            if (!__result || !MechSystem.IsMechPart(slot.Item))
            {
                return;
            }
            BasePickupItem dragged = _draggableItem(__instance);
            if (dragged != null && !dragged.Is<AmmoRecord>() && !dragged.Is<GrenadeRecord>())
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), "EquipSpecificItem")]
    public static class EquipSpecificItem_Patch
    {
        public static bool Prefix(ItemStorage target, ref bool __result)
        {
            if (!target.Empty && MechSystem.IsMechPart(target.First))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryScreen), "DragControllerShowContextMenuCallback")]
    public static class ShowContextMenu_Patch
    {
        private static readonly AccessTools.FieldRef<InventoryScreen, ItemSlot> _contextMenuItemSlot =
            AccessTools.FieldRefAccess<InventoryScreen, ItemSlot>("_contextMenuItemSlot");

        public static bool Prefix(InventoryScreen __instance, ItemSlot obj)
        {
            BasePickupItem item = obj.Item;
            if (!MechSystem.IsMechPart(item))
            {
                return true;
            }
            _contextMenuItemSlot(__instance) = obj;
            CommonContextMenu contextMenu = UI.Get<CommonContextMenu>();
            WeaponRecord weaponRecord = item.Record<WeaponRecord>();
            if (weaponRecord != null && !string.IsNullOrEmpty(weaponRecord.RequiredAmmo))
            {
                contextMenu.SetupCommand(Localization.Get("ui.context." + ContextMenuCommand.Reload), (int)ContextMenuCommand.Reload);
                WeaponComponent weaponComponent = item.Comp<WeaponComponent>();
                if (weaponComponent != null && weaponComponent.CurrentAmmo > 0)
                {
                    contextMenu.SetupCommand(Localization.Get("ui.context." + ContextMenuCommand.UnloadAmmo), (int)ContextMenuCommand.UnloadAmmo);
                }
            }
            contextMenu.SetupCommand(Localization.Get("mechs.ui.exit"), MechConfig.ExitMechBindValue);
            UI.Chain<CommonContextMenu>().Show().SetBackgroundOrder(-1)
                .SetBackOnBackgroundClick(value: true);
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryScreen), "ContextMenuOnCmdSelected")]
    public static class ContextMenuCmd_Patch
    {
        public static bool Prefix(int bindValue)
        {
            if (bindValue != MechConfig.ExitMechBindValue)
            {
                return true;
            }
            MechSystem.ExitMechInRaid();
            return false;
        }
    }

    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.RestoreStateAfterMission))]
    public static class RestoreStateAfterMission_Patch
    {
        public static void Prefix(SpaceTime spaceTime, Mercenary mercenary, MagnumCargo magnumCargo)
        {
            MechSystem.PackUpAfterMission(mercenary, magnumCargo, spaceTime);
        }
    }

    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.DropOnDeathPenalty))]
    public static class DropOnDeathPenalty_Patch
    {
        public static void Prefix(Mercenary merc)
        {
            MechSystem.PackUpOnDeath(merc);
        }
    }
}
