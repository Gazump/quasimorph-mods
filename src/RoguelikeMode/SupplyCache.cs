using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    public static class SupplyCache
    {
        public static void Spawn(State state)
        {
            if (RogueRun.Tier == RogueTier.Hard)
            {
                return;
            }
            Creatures creatures = state.Get<Creatures>();
            MapGrid mapGrid = state.Get<MapGrid>();
            ItemsOnFloor itemsOnFloor = state.Get<ItemsOnFloor>();
            Player player = creatures?.Player;
            if (player == null || mapGrid == null || itemsOnFloor == null)
            {
                return;
            }
            List<string> supplies = PickSupplies(new System.Random(RogueRun.SeedFor("cache:" + RogueRun.CurrentLocationId)));
            if (supplies.Count == 0)
            {
                return;
            }
            CellPosition pos = SpawnSystem.FindValidSpawnPoint(mapGrid, creatures, player.CreatureData.Position, 1, 200, 4);
            if (pos.Equals(CellPosition.Zero))
            {
                pos = player.CreatureData.Position;
            }
            foreach (string itemId in supplies)
            {
                ItemOnFloorSystem.SpawnItem(itemsOnFloor, mapGrid, itemId, pos, 0f, rndConditionAndCapacity: false);
            }
            Debug.Log($"[RoguelikeMode] Supply cache at {pos}: {string.Join(", ", supplies)}");
        }

        private static List<string> PickSupplies(System.Random rng)
        {
            List<string> supplies = new List<string>();
            AddIfFound(supplies, PickCheap(ItemClass.Dressing, rng));
            AddIfFound(supplies, PickCheap(RogueRun.Tier == RogueTier.Easy ? ItemClass.Medpack : ItemClass.Dressing, rng));
            AddIfFound(supplies, PickCheap(ItemClass.Food, rng));
            if (RogueRun.Tier == RogueTier.Easy)
            {
                AddIfFound(supplies, PickCheap(ItemClass.Drink, rng));
                AddIfFound(supplies, PickPainkiller());
            }
            return supplies;
        }

        private static void AddIfFound(List<string> supplies, string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                supplies.Add(itemId);
            }
        }

        private static string PickCheap(ItemClass itemClass, System.Random rng)
        {
            List<ConsumableRecord> candidates = new List<ConsumableRecord>();
            foreach (BasePickupItemRecord item in Data.Items.Records)
            {
                if (!(item is CompositeItemRecord compositeItemRecord))
                {
                    continue;
                }
                foreach (BasePickupItemRecord record in compositeItemRecord.Records)
                {
                    if (record is ConsumableRecord consumableRecord && consumableRecord.ItemClass == itemClass && consumableRecord.TechLevel <= 1 && !consumableRecord.Id.Contains("_custom"))
                    {
                        candidates.Add(consumableRecord);
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return null;
            }
            candidates.Sort((a, b) => a.Price.CompareTo(b.Price));
            int poolSize = Mathf.Min(3, candidates.Count);
            return candidates[rng.Next(poolSize)].Id;
        }

        private static string PickPainkiller()
        {
            ConsumableRecord best = null;
            foreach (BasePickupItemRecord item in Data.Items.Records)
            {
                if (!(item is CompositeItemRecord compositeItemRecord))
                {
                    continue;
                }
                foreach (BasePickupItemRecord record in compositeItemRecord.Records)
                {
                    if (record is ConsumableRecord consumableRecord && consumableRecord.ItemClass == ItemClass.Pills && consumableRecord.PainValue < 0 && consumableRecord.TechLevel <= 1 && !consumableRecord.Id.Contains("_custom"))
                    {
                        if (best == null || consumableRecord.Price < best.Price)
                        {
                            best = consumableRecord;
                        }
                    }
                }
            }
            return best?.Id;
        }
    }
}
