using System.Collections.Generic;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    public static class Registration
    {
        public static string ContentPath;

        public static void RegisterAll()
        {
            RegisterMissionDifficulty();
            RegisterKeycard();
            ExtendElevatorFloorMap();
            InjectStrings();
        }

        private static void InjectStrings()
        {
            LocalizationInjector.Set(RogueConfig.EasyCaptionKey, "EASY");
            LocalizationInjector.Set(RogueConfig.NormalCaptionKey, "NORMAL");
            LocalizationInjector.Set(RogueConfig.HardCaptionKey, "HARD");
            LocalizationInjector.Set(RogueConfig.BackCaptionKey, "< BACK");
            LocalizationInjector.Set(RogueConfig.ResumeCaptionKey, "RESUME DIVE");
            LocalizationInjector.Set(RogueConfig.DiveButtonCaptionKey, "THE DIVE - ROGUELIKE MODE");
            LocalizationInjector.Set("item." + RogueConfig.KeycardId + ".name", "GOLDEN KEYCARD");
            LocalizationInjector.Set("item." + RogueConfig.KeycardId + ".shortdesc", "The way out.");
            LocalizationInjector.Set("mission." + RogueConfig.StoryId + ".objective0",
                "Descend the facility. The GOLDEN KEYCARD waits on the bottom floor - seize it and evacuate.");
        }

        private static void RegisterMissionDifficulty()
        {
            MissionDifficultyRecord low = Data.MissionDifficulty.Get(1);
            MissionDifficultyRecord high = Data.MissionDifficulty.Get(5);
            foreach (MissionDifficultyRecord record in Data.MissionDifficulty)
            {
                Debug.Log($"[RoguelikeMode] Vanilla mission difficulty {record.DifficultyRating}: monsters {record.MonsterPointsPerStage}, items {record.ItemPointsPerStage}, stages {record.MinStages}-{record.MaxStages}, rooms {record.MinRooms}-{record.MaxRooms}, corridors {record.MinCorridors}-{record.MaxCorridors}");
            }
            Data.MissionDifficulty.Add(new MissionDifficultyRecord
            {
                DifficultyRating = RogueConfig.MissionDifficultyRating,
                MonsterPointsPerStage = high?.MonsterPointsPerStage ?? 100,
                ItemPointsPerStage = high?.ItemPointsPerStage ?? 100,
                MinStages = RogueConfig.FloorCount,
                MaxStages = RogueConfig.FloorCount,
                MinRooms = low?.MinRooms ?? 4,
                MaxRooms = high?.MaxRooms ?? 7,
                MinCorridors = low?.MinCorridors ?? 1,
                MaxCorridors = high?.MaxCorridors ?? 3
            });
        }

        private static void RegisterKeycard()
        {
            TrashRecord template = FindKeycardTemplate();
            if (template == null)
            {
                Debug.LogError("[RoguelikeMode] No trash-item template found; golden keycard not registered.");
                return;
            }
            RogueTrashRecord record = new RogueTrashRecord(RogueConfig.KeycardId)
            {
                ItemClass = template.ItemClass,
                Categories = new List<string>(),
                TechLevel = 1,
                Price = 5000f,
                Weight = 0.1f,
                InventoryWidthSize = 1,
                CanPutInVest = true,
                MaxStack = 1,
                SubType = TrashSubtype.QuestItem,
                ContentDescriptor = CloneDescriptor(template)
            };
            Data.Items.AddRecord(record.Id, record);
        }

        private static TrashRecord FindKeycardTemplate()
        {
            TrashRecord fallback = null;
            foreach (BasePickupItemRecord item in Data.Items.Records)
            {
                if (!(item is CompositeItemRecord compositeItemRecord))
                {
                    continue;
                }
                foreach (BasePickupItemRecord record in compositeItemRecord.Records)
                {
                    if (record is TrashRecord trashRecord && trashRecord.ContentDescriptor is ItemContentDescriptor descriptor && descriptor.Icon != null)
                    {
                        if (trashRecord.Id.Contains("keycard") || trashRecord.Id.Contains("key_card"))
                        {
                            return trashRecord;
                        }
                        if (fallback == null && trashRecord.SubType == TrashSubtype.QuestItem)
                        {
                            fallback = trashRecord;
                        }
                        if (fallback == null)
                        {
                            fallback = trashRecord;
                        }
                    }
                }
            }
            return fallback;
        }

        private static ItemContentDescriptor CloneDescriptor(TrashRecord template)
        {
            ItemContentDescriptor source = template.ContentDescriptor as ItemContentDescriptor;
            ItemContentDescriptor descriptor = Object.Instantiate(source);
            descriptor.name = RogueConfig.KeycardId;
            Sprite customIcon = LoadKeycardIcon(source.Icon, "rogue_golden_keycard.png");
            if (customIcon != null)
            {
                typeof(ItemContentDescriptor)
                    .GetField("_icon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(descriptor, customIcon);
            }
            Sprite customSmallIcon = LoadKeycardIcon(source.SmallIcon, "rogue_golden_keycard_small.png");
            if (customSmallIcon != null)
            {
                typeof(ItemContentDescriptor)
                    .GetField("_smallIcon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(descriptor, customSmallIcon);
            }
            return descriptor;
        }

        private static Sprite LoadKeycardIcon(Sprite reference, string fileName)
        {
            if (string.IsNullOrEmpty(ContentPath) || reference == null)
            {
                return null;
            }
            string path = System.IO.Path.Combine(ContentPath, fileName);
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning("[RoguelikeMode] Keycard icon PNG not found at " + path);
                return null;
            }
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!texture.LoadImage(System.IO.File.ReadAllBytes(path)))
            {
                return null;
            }
            texture.filterMode = FilterMode.Point;
            float ppu = reference.pixelsPerUnit * (texture.width / reference.rect.width);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
            sprite.name = System.IO.Path.GetFileNameWithoutExtension(fileName);
            return sprite;
        }

        private static void ExtendElevatorFloorMap()
        {
            Dictionary<string, int> map = AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(typeof(ElevatorWindow), "_stageToFloor");
            for (int i = 8; i <= 15; i++)
            {
                string key = "stage" + i;
                if (!map.ContainsKey(key))
                {
                    map.Add(key, i);
                }
            }
        }

    }
}
