using System;
using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace LootTracker
{
    internal static class OwnershipIndex
    {
        private static readonly HashSet<string> OwnedNow = new HashSet<string>(StringComparer.Ordinal);

        private static int _builtFrame = -1;

        public static void Invalidate()
        {
            _builtFrame = -1;
        }

        public static bool HasNow(string itemId)
        {
            EnsureFresh();
            return OwnedNow.Contains(itemId);
        }

        private static void EnsureFresh()
        {
            int frame = Time.frameCount;
            if (_builtFrame == frame)
            {
                return;
            }
            _builtFrame = frame;
            Rebuild();
        }

        private static void Rebuild()
        {
            OwnedNow.Clear();

            MagnumCargo cargo = Resolve<MagnumCargo>();
            if (cargo != null)
            {
                List<ItemStorage> shipCargo = cargo.ShipCargo;
                if (shipCargo != null)
                {
                    for (int i = 0; i < shipCargo.Count; i++)
                    {
                        Collect(shipCargo[i]);
                    }
                }
                Collect(cargo.FridgeStorage);
                Collect(cargo.RecyclingStorage);
            }

            Collect(Resolve<MagnumProgression>()?.GetDepartment<ShuttleCargoDepartment>()?.ShuttleCargo);

            Mercenaries mercenaries = Resolve<Mercenaries>();
            if (mercenaries != null)
            {
                List<Mercenary> roster = mercenaries.Values;
                for (int i = 0; i < roster.Count; i++)
                {
                    CollectInventory(roster[i]?.CreatureData?.Inventory);
                }
            }

            CollectInventory(Resolve<Creatures>()?.Player?.CreatureData?.Inventory);

            OwnedRegistry.NoteOwned(OwnedNow);
        }

        private static void CollectInventory(Inventory inventory)
        {
            if (inventory == null)
            {
                return;
            }
            List<ItemStorage> containers = inventory.AllContainers;
            if (containers == null)
            {
                return;
            }
            for (int i = 0; i < containers.Count; i++)
            {
                Collect(containers[i]);
            }
        }

        private static void Collect(ItemStorage storage)
        {
            if (storage == null)
            {
                return;
            }
            List<BasePickupItem> items = storage.Items;
            if (items == null)
            {
                return;
            }
            for (int i = 0; i < items.Count; i++)
            {
                BasePickupItem item = items[i];
                if (item != null && !string.IsNullOrEmpty(item.Id))
                {
                    OwnedNow.Add(item.Id);
                }
            }
        }

        private static T Resolve<T>() where T : class
        {
            return SingletonMonoBehaviour<UI>.Instance == null ? null : UI.Resolve<T>();
        }
    }
}
