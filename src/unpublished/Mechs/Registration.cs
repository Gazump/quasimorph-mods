using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MGSC;
using UnityEngine;

namespace Mechs
{
    public static class Registration
    {
        public static bool Registered { get; private set; }

        public static void RegisterAll()
        {
            ArmorRecord chassisTemplate = Data.Items.GetSimpleRecord<ArmorRecord>(MechConfig.ChassisTemplate);
            HelmetRecord headTemplate = Data.Items.GetSimpleRecord<HelmetRecord>(MechConfig.HeadTemplate);
            LeggingsRecord legsTemplate = Data.Items.GetSimpleRecord<LeggingsRecord>(MechConfig.LegsTemplate);
            BootsRecord bootsTemplate = Data.Items.GetSimpleRecord<BootsRecord>(MechConfig.BootsTemplate);
            VestRecord feedTemplate = Data.Items.GetSimpleRecord<VestRecord>(MechConfig.FeedTemplate);
            WeaponRecord cannonTemplate = Data.Items.GetSimpleRecord<WeaponRecord>(MechConfig.CannonTemplate);
            WeaponRecord drillTemplate = Data.Items.GetSimpleRecord<WeaponRecord>(MechConfig.DrillTemplate);
            AmmoRecord shellsTemplate = Data.Items.GetSimpleRecord<AmmoRecord>(MechConfig.ShellsTemplate);
            if (chassisTemplate == null || headTemplate == null || legsTemplate == null || bootsTemplate == null
                || feedTemplate == null || cannonTemplate == null || drillTemplate == null || shellsTemplate == null)
            {
                Debug.LogError("[Mechs] Missing vanilla template records; mod content disabled. Game version mismatch?");
                return;
            }

            RegisterShells(shellsTemplate);
            RegisterArmorPiece(chassisTemplate.Clone(MechConfig.ChassisId), MechConfig.ChassisTemplate, "mech_chassis_icon", 400, 12f, 2000f);
            RegisterHead(headTemplate);
            RegisterArmorPiece(CloneLeggings(legsTemplate), MechConfig.LegsTemplate, "mech_legs_icon", 350, 8f, 1600f);
            RegisterArmorPiece(CloneBoots(bootsTemplate), MechConfig.BootsTemplate, "mech_boots_icon", 300, 5f, 1000f);
            RegisterFeed(feedTemplate);
            RegisterCannon(cannonTemplate);
            RegisterDrill(drillTemplate);
            RegisterMechItem();
            Registered = true;
        }

        private static LeggingsRecord CloneLeggings(LeggingsRecord template)
        {
            return template.Clone(MechConfig.LegsId);
        }

        private static BootsRecord CloneBoots(BootsRecord template)
        {
            return template.Clone(MechConfig.BootsId);
        }

        private static void RegisterArmorPiece(ResistRecord record, string templateId, string iconName, int durability, float weight, float price)
        {
            ApplyCommonPartFields(record, weight, price);
            record.MaxDurability = durability;
            record.MinDurabilityAfterRepair = 0;
            record.Unbreakable = false;
            record.RepairItemIds = new List<string> { "armor_plates", "ceramite_plates" };
            ScaleResists(record, 1.5f, poisonFloor: 20f);
            record.ContentDescriptor = CloneDescriptor(templateId, iconName);
            Data.Items.AddRecord(record.Id, record);
        }

        private static void RegisterHead(HelmetRecord template)
        {
            HelmetRecord record = template.Clone(MechConfig.HeadId);
            record.HideHair = true;
            RegisterArmorPiece(record, MechConfig.HeadTemplate, "mech_head_icon", 300, 4f, 1200f);
        }

        private static void RegisterFeed(VestRecord template)
        {
            VestRecord record = new VestRecord();
            RecordReflection.SetId(record, MechConfig.FeedId);
            ApplyCommonPartFields(record, 3f, 800f);
            record.SlotCapacity = 1;
            record.ReloadTurnMod = -1;
            record.DropChanceOnBroken = 0f;
            record.MaxDurability = 250;
            record.MinDurabilityAfterRepair = 0;
            record.Unbreakable = false;
            record.RepairItemIds = new List<string> { "armor_plates" };
            RecordReflection.SetResistSheet(record, BuildResistSheet(20f, 20f, 20f, 10f, 10f, 10f, 15f, 20f));
            record.ContentDescriptor = CloneDescriptor(MechConfig.FeedTemplate, "mech_feed_icon");
            Data.Items.AddRecord(record.Id, record);
        }

        private static void RegisterCannon(WeaponRecord template)
        {
            WeaponRecord record = template.Clone(MechConfig.CannonId);
            ApplyCommonPartFields(record, 10f, 2500f);
            record.InventoryWidthSize = 2;
            record.MaxDurability = 350;
            record.MinDurabilityAfterRepair = 0;
            record.Unbreakable = false;
            record.RepairItemIds = new List<string> { "weapon_parts" };
            DmgInfo damage = record.Damage;
            damage.minDmg = 55;
            damage.maxDmg = 110;
            damage.critDmg = 1.85f;
            record.Damage = damage;
            record.RequiredAmmo = MechConfig.ShellsAmmoType;
            record.OverrideAmmo = new List<string>();
            record.DefaultAmmoId = MechConfig.ShellsId;
            record.MagazineCapacity = 60;
            record.ReloadDuration = 6;
            record.Range = 7;
            record.Falloff = 0.15f;
            record.BonusAccuracy = 0.25f;
            record.MinRandomAmmoCount = 0;
            record.Traits = new List<string> { "piercing", "ramp_up" };
            record.ContentDescriptor = CloneDescriptor(MechConfig.CannonTemplate, "mech_cannon_icon");
            Data.Items.AddRecord(record.Id, record);
        }

        private static void RegisterDrill(WeaponRecord template)
        {
            WeaponRecord record = template.Clone(MechConfig.DrillId);
            ApplyCommonPartFields(record, 8f, 1500f);
            record.InventoryWidthSize = 2;
            record.MaxDurability = 300;
            record.MinDurabilityAfterRepair = 0;
            record.Unbreakable = false;
            record.RepairItemIds = new List<string> { "weapon_parts" };
            DmgInfo damage = record.Damage;
            damage.minDmg = 70;
            damage.maxDmg = 90;
            damage.critDmg = 1.6f;
            record.Damage = damage;
            if (Data.Items.GetSimpleRecord<AmmoRecord>("implicted_drill_long") != null)
            {
                record.DefaultAmmoId = "implicted_drill_long";
            }
            record.RequiredAmmo = "";
            record.MagazineCapacity = 0;
            record.MeleeCanAmputate = true;
            record.GetMeleeDamageFromCreature = false;
            record.IsMelee = true;
            record.Range = 1;
            record.Traits = new List<string>();
            record.ContentDescriptor = CloneDescriptor(MechConfig.DrillTemplate, "mech_drill_icon");
            Data.Items.AddRecord(record.Id, record);
        }

        private static void RegisterShells(AmmoRecord template)
        {
            AmmoRecord record = new AmmoRecord();
            RecordReflection.SetId(record, MechConfig.ShellsId);
            record.Categories = new List<string>();
            record.TechLevel = 10;
            record.Price = 25f;
            record.Weight = 0.15f;
            record.InventoryWidthSize = 1;
            record.ItemClass = ItemClass.Ammo;
            record.CanPutInVest = true;
            record.Disassembly = new List<ItemQuantity>();
            record.MinAmmoAmount = 10;
            record.MaxAmmoAmount = 30;
            record.MaxStack = 30;
            record.AmmoType = MechConfig.ShellsAmmoType;
            record.DmgType = "pierce";
            record.DmgCritChance = 0.1f;
            record.RangeBonus = 0;
            record.AccuracyMult = 1f;
            record.ScatterMult = 1f;
            record.DamageMult = 1f;
            record.BulletCastsPerShot = 1;
            record.StatusEffectId = "";
            record.ChanceToApply = 0f;
            record.Traits = new List<string>();
            record.ProjectileId = template.ProjectileId;
            record.ContentDescriptor = CloneDescriptor(MechConfig.ShellsTemplate, "mech_shells_icon");
            Data.Items.AddRecord(record.Id, record);
        }

        private static void RegisterMechItem()
        {
            MechRecord record = new MechRecord(MechConfig.MechItemId)
            {
                Categories = new List<string>(),
                TechLevel = 10,
                Price = 9000f,
                Weight = 25f,
                InventoryWidthSize = 2,
                ItemClass = ItemClass.Cyborg,
                CanPutInVest = false,
                Disassembly = new List<ItemQuantity>(),
                MaxUsage = 1,
                UsageCost = 1,
                MaxStack = 1,
                ShowConfirmBox = false,
                UseEffect = new Dictionary<string, float>(),
                Duration = 0,
                ContentDescriptor = CloneDescriptor(MechConfig.MechItemTemplate, "mech_atlas_icon")
            };
            Data.Items.AddRecord(record.Id, record);

            MechFrameRecord frame = new MechFrameRecord(MechConfig.MechItemId)
            {
                Categories = new List<string>(),
                TechLevel = 10,
                Price = 9000f,
                Weight = 25f,
                InventoryWidthSize = 2,
                ItemClass = ItemClass.Cyborg,
                CanPutInVest = false,
                Disassembly = new List<ItemQuantity>(),
                MaxDurability = 400,
                MinDurabilityAfterRepair = 0,
                Unbreakable = false,
                RepairItemIds = new List<string> { "weapon_parts", "armor_plates" }
            };
            Data.Items.AddRecord(frame.Id, frame);
        }

        private static void ApplyCommonPartFields(ItemRecord record, float weight, float price)
        {
            record.Categories = new List<string>();
            record.TechLevel = 10;
            record.Price = price;
            record.Weight = weight;
            record.CanPutInVest = false;
            record.Disassembly = new List<ItemQuantity>();
        }

        private static void ScaleResists(ResistRecord record, float factor, float poisonFloor)
        {
            foreach (DmgResist resist in new List<DmgResist>(record.ResistSheet))
            {
                record.SetResist(resist.damage, Mathf.Round(resist.resistPercent * factor));
            }
            if (record.GetResist("poison") < poisonFloor)
            {
                record.SetResist("poison", poisonFloor);
            }
        }

        private static List<DmgResist> BuildResistSheet(float blunt, float pierce, float lacer, float fire, float cold, float poison, float shock, float beam)
        {
            return new List<DmgResist>
            {
                new DmgResist { damage = "blunt", resistPercent = blunt },
                new DmgResist { damage = "pierce", resistPercent = pierce },
                new DmgResist { damage = "lacer", resistPercent = lacer },
                new DmgResist { damage = "fire", resistPercent = fire },
                new DmgResist { damage = "cold", resistPercent = cold },
                new DmgResist { damage = "poison", resistPercent = poison },
                new DmgResist { damage = "shock", resistPercent = shock },
                new DmgResist { damage = "beam", resistPercent = beam }
            };
        }

        private static ItemContentDescriptor CloneDescriptor(string templateItemId, string iconName)
        {
            ItemContentDescriptor template = null;
            if (Data.Items.GetRecord(templateItemId) is CompositeItemRecord composite
                && composite.PrimaryRecord.ContentDescriptor is ItemContentDescriptor itemContentDescriptor)
            {
                template = itemContentDescriptor;
            }
            ItemContentDescriptor descriptor;
            if (template != null)
            {
                descriptor = Object.Instantiate(template);
            }
            else
            {
                Debug.LogWarning("[Mechs] No descriptor found on template '" + templateItemId + "'; using a bare descriptor.");
                descriptor = ScriptableObject.CreateInstance<ItemContentDescriptor>();
            }
            Sprite customIcon = IconFactory.GetOrNull(iconName, template?.Icon);
            if (customIcon != null)
            {
                typeof(ItemContentDescriptor).GetField("_icon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(descriptor, customIcon);
            }
            Sprite customSmallIcon = IconFactory.GetOrNull(iconName + "_small", template?.SmallIcon);
            if (customSmallIcon != null)
            {
                typeof(ItemContentDescriptor).GetField("_smallIcon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(descriptor, customSmallIcon);
            }
            return descriptor;
        }

        public static string BuildLocalizationRows()
        {
            StringBuilder stringBuilder = new StringBuilder();
            AddRow(stringBuilder, "item.mech_atlas.name", "MK-1 'Atlas' exoframe");
            AddRow(stringBuilder, "item.mech_atlas.shortdesc", "Crated combat exoframe. Use in the field to suit up.");
            AddRow(stringBuilder, "item.mech_atlas_head.name", "Atlas sensor head");
            AddRow(stringBuilder, "item.mech_atlas_head.shortdesc", "Sealed sensor module of the Atlas exoframe.");
            AddRow(stringBuilder, "item.mech_atlas_chassis.name", "Atlas chassis");
            AddRow(stringBuilder, "item.mech_atlas_chassis.shortdesc", "Armored core hull of the Atlas exoframe.");
            AddRow(stringBuilder, "item.mech_atlas_legs.name", "Atlas leg actuators");
            AddRow(stringBuilder, "item.mech_atlas_legs.shortdesc", "Servo-driven legs of the Atlas exoframe.");
            AddRow(stringBuilder, "item.mech_atlas_boots.name", "Atlas stabilizer feet");
            AddRow(stringBuilder, "item.mech_atlas_boots.shortdesc", "Ground-shock stabilizers of the Atlas exoframe.");
            AddRow(stringBuilder, "item.mech_atlas_feed.name", "Atlas ammo feed");
            AddRow(stringBuilder, "item.mech_atlas_feed.shortdesc", "Autoloader rig. Holds one stack of cannon shells.");
            AddRow(stringBuilder, "item.mech_atlas_drill.name", "Atlas breaker drill");
            AddRow(stringBuilder, "item.mech_atlas_drill.shortdesc", "Mining drill arm. Chews through flesh and bone.");
            AddRow(stringBuilder, "item.mech_atlas_cannon.name", "Atlas autocannon");
            AddRow(stringBuilder, "item.mech_atlas_cannon.shortdesc", "Arm-mounted rotary cannon. Feeds on mech shells.");
            AddRow(stringBuilder, "item.mech_cannon_shells.name", "Mech shells");
            AddRow(stringBuilder, "item.mech_cannon_shells.shortdesc", "Belted shells for mech autocannons.");
            AddRow(stringBuilder, "mechs.ui.exit", "Exit mech");
            return stringBuilder.ToString();
        }

        private static void AddRow(StringBuilder sb, string key, string english)
        {
            sb.Append('\n').Append(key);
            for (int i = 0; i < 11; i++)
            {
                sb.Append('\t').Append(english);
            }
        }
    }
}
