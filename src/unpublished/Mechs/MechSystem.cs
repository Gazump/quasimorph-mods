using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace Mechs
{
    public static class MechSystem
    {
        public static bool IsMechItem(BasePickupItem item)
        {
            return item != null && item.Id == MechConfig.MechItemId;
        }

        public static bool IsMechPart(BasePickupItem item)
        {
            return item != null && MechConfig.PartIds.Contains(item.Id);
        }

        public static bool IsPiloted(Inventory inventory)
        {
            foreach (ItemStorage slot in inventory.Slots)
            {
                if (!slot.Empty && IsMechPart(slot.First))
                {
                    return true;
                }
            }
            return false;
        }

        public static void EnterMech(Creatures creatures, BasePickupItem mechItem)
        {
            if (!Registered() || creatures?.Player == null || MechContext.ItemsOnFloor == null)
            {
                return;
            }
            Player player = creatures.Player;
            Inventory inventory = player.CreatureData.Inventory;
            if (IsPiloted(inventory))
            {
                PlaySound(SingletonMonoBehaviour<SoundsStorage>.Instance.EmptyAttack);
                return;
            }
            float condition = mechItem.Comp<BreakableItemComponent>()?.CurrentPercent ?? 1f;
            ItemInteractionSystem.ConsumeItem(mechItem);
            ItemStorage floorStorage = MechContext.ItemsOnFloor.GetOrCreate(player.CreatureData.Position).Storage;
            UnequipIfPresent(inventory, inventory.HelmetSlot, floorStorage);
            UnequipIfPresent(inventory, inventory.ArmorSlot, floorStorage);
            UnequipIfPresent(inventory, inventory.LeggingsSlot, floorStorage);
            UnequipIfPresent(inventory, inventory.BootsSlot, floorStorage);
            UnequipIfPresent(inventory, inventory.VestSlot, floorStorage);
            UnequipIfPresent(inventory, inventory.PrimarySlot, floorStorage);
            UnequipIfPresent(inventory, inventory.SecondarySlot, floorStorage);
            EquipPart(MechConfig.HeadId, inventory.HelmetSlot, condition);
            EquipPart(MechConfig.ChassisId, inventory.ArmorSlot, condition);
            EquipPart(MechConfig.LegsId, inventory.LeggingsSlot, condition);
            EquipPart(MechConfig.BootsId, inventory.BootsSlot, condition);
            EquipPart(MechConfig.FeedId, inventory.VestSlot, condition);
            EquipPart(MechConfig.CannonId, inventory.PrimarySlot, condition);
            EquipPart(MechConfig.DrillId, inventory.SecondarySlot, condition);
            List<BasePickupItem> shells = new List<BasePickupItem>();
            ItemInteractionSystem.CreateItem(shells, MechConfig.ShellsId, 30);
            foreach (BasePickupItem stack in shells)
            {
                if (!ItemInteractionSystem.Move(stack, inventory.VestStore, CellPosition.Zero, sendEvent: true))
                {
                    ItemOnFloorSystem.StackItemOnFloor(stack, floorStorage);
                }
            }
            inventory.SetCurrentWeaponSlot(WeaponSlotType.Primary);
            PlaySound(SingletonMonoBehaviour<SoundsStorage>.Instance.EquipWeapon);
        }

        public static void ExitMechInRaid()
        {
            Creatures creatures = MechContext.Creatures;
            if (creatures?.Player == null || MechContext.ItemsOnFloor == null)
            {
                return;
            }
            if (!TurnSystem.CanProcessPlayerTurn(MechContext.TurnController, MechContext.TurnMetadata, creatures))
            {
                PlaySound(SingletonMonoBehaviour<SoundsStorage>.Instance.EmptyAttack);
                return;
            }
            Player player = creatures.Player;
            Inventory inventory = player.CreatureData.Inventory;
            if (!IsPiloted(inventory))
            {
                return;
            }
            ItemStorage floorStorage = MechContext.ItemsOnFloor.GetOrCreate(player.CreatureData.Position).Storage;
            float condition = RemoveParts(inventory);
            BasePickupItem mechItem = CreateMechItem(condition);
            if (mechItem != null && !inventory.TakeOrEquip(mechItem, putIfSlotBusy: true))
            {
                ItemOnFloorSystem.StackItemOnFloor(mechItem, floorStorage);
            }
            PlaySound(SingletonMonoBehaviour<SoundsStorage>.Instance.TakeItem);
            player.CreatureData.EffectsController.PropagateAction(PlayerActionHappened.HandAction);
            UI.Get<InventoryScreen>()?.RefreshItemsList();
            PlayerInteractionSystem.EndPlayerTurn(creatures, PlayerEndTurnContext.InventoryInteraction);
        }

        public static void PackUpAfterMission(Mercenary mercenary, MagnumCargo magnumCargo, SpaceTime spaceTime)
        {
            Inventory inventory = mercenary.CreatureData.Inventory;
            if (!IsPiloted(inventory))
            {
                return;
            }
            EmptyVestStore(inventory);
            float condition = RemoveParts(inventory);
            BasePickupItem mechItem = CreateMechItem(condition);
            if (mechItem != null)
            {
                mechItem.ExaminedItem = false;
                MagnumCargoSystem.AddCargo(magnumCargo, spaceTime, mechItem);
            }
        }

        public static void PackUpOnDeath(Mercenary mercenary)
        {
            Inventory inventory = mercenary.CreatureData.Inventory;
            if (!IsPiloted(inventory))
            {
                return;
            }
            EmptyVestStore(inventory);
            float condition = RemoveParts(inventory);
            BasePickupItem mechItem = CreateMechItem(condition);
            if (mechItem != null && !inventory.TakeOrEquip(mechItem, putIfSlotBusy: true))
            {
                inventory.BackpackStore.ExpandHeightAndPutItem(mechItem);
            }
        }

        private static bool Registered()
        {
            return Registration.Registered;
        }

        private static void UnequipIfPresent(Inventory inventory, ItemStorage slot, ItemStorage floorStorage)
        {
            if (!slot.Empty)
            {
                inventory.Unequip(slot.First, floorStorage);
            }
        }

        private static void EquipPart(string partId, ItemStorage slot, float condition)
        {
            BasePickupItem part = SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(partId);
            if (part == null)
            {
                return;
            }
            ApplyCondition(part, condition);
            if (!ItemInteractionSystem.Move(part, slot, CellPosition.Zero, sendEvent: true))
            {
                Debug.LogError("[Mechs] Failed to equip " + partId + " into its slot.");
            }
        }

        private static void ApplyCondition(BasePickupItem item, float condition)
        {
            BreakableItemComponent breakable = item.Comp<BreakableItemComponent>();
            if (breakable == null || condition >= 1f)
            {
                return;
            }
            int damage = Mathf.RoundToInt((1f - Mathf.Clamp01(condition)) * breakable.MaxDurability);
            if (damage > 0)
            {
                breakable.Break(damage);
            }
        }

        private static float RemoveParts(Inventory inventory)
        {
            float sum = 0f;
            int count = 0;
            foreach (ItemStorage slot in inventory.Slots)
            {
                if (slot.Empty || !IsMechPart(slot.First))
                {
                    continue;
                }
                BasePickupItem part = slot.First;
                BreakableItemComponent breakable = part.Comp<BreakableItemComponent>();
                sum += breakable?.CurrentPercent ?? 1f;
                count++;
                slot.Remove(part);
            }
            return count > 0 ? sum / count : 1f;
        }

        private static void EmptyVestStore(Inventory inventory)
        {
            if (inventory.VestStore.Empty)
            {
                return;
            }
            List<BasePickupItem> items = new List<BasePickupItem>(inventory.VestStore.Items);
            foreach (BasePickupItem item in items)
            {
                inventory.VestStore.Remove(item, sendEvent: false);
                if (!ItemInteractionSystem.Move(item, inventory.BackpackStore, CellPosition.Zero, sendEvent: false))
                {
                    inventory.BackpackStore.ExpandHeightAndPutItem(item);
                }
            }
        }

        private static BasePickupItem CreateMechItem(float condition)
        {
            BasePickupItem mechItem = SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(MechConfig.MechItemId);
            if (mechItem == null)
            {
                Debug.LogError("[Mechs] Failed to recreate the mech item.");
                return null;
            }
            ApplyCondition(mechItem, condition);
            return mechItem;
        }

        private static void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                SingletonMonoBehaviour<SoundController>.Instance.PlayUiSound(clip);
            }
        }
    }
}
