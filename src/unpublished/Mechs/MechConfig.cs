using System.Collections.Generic;

namespace Mechs
{
    public static class MechConfig
    {
        public const string MechItemId = "mech_atlas";
        public const string HeadId = "mech_atlas_head";
        public const string ChassisId = "mech_atlas_chassis";
        public const string LegsId = "mech_atlas_legs";
        public const string BootsId = "mech_atlas_boots";
        public const string FeedId = "mech_atlas_feed";
        public const string DrillId = "mech_atlas_drill";
        public const string CannonId = "mech_atlas_cannon";
        public const string ShellsId = "mech_cannon_shells";
        public const string ShellsAmmoType = "MechShell";

        public const int ExitMechBindValue = 5001;

        public static readonly HashSet<string> PartIds = new HashSet<string>
        {
            HeadId, ChassisId, LegsId, BootsId, FeedId, DrillId, CannonId
        };

        public const string ChassisTemplate = "military_power_armor_1";
        public const string HeadTemplate = "military_power_helmet_1";
        public const string LegsTemplate = "military_power_pants_1";
        public const string BootsTemplate = "military_power_boots_1";
        public const string FeedTemplate = "heavy_armored_vest_1";
        public const string CannonTemplate = "military_minigun_1";
        public const string DrillTemplate = "cyborg_drill";
        public const string ShellsTemplate = "rifle_basic_ammo";
        public const string MechItemTemplate = "packaged_security_robot";
    }
}
