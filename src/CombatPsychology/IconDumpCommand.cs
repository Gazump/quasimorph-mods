using System.Collections.Generic;
using System.IO;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    [ConsoleCommand(new string[] { "psy_dumpicons" })]
    public class IconDumpCommand
    {
        public static string Help(string command, bool verbose)
        {
            return "Dumps vanilla icon PNGs (status effects, tooltip icons, item icons) to the mod folder as art references. 'psy_dumpicons all' dumps every tooltip icon.";
        }

        public string Execute(string[] tokens)
        {
            bool all = tokens != null && tokens.Length > 0 && tokens[0] == "all";
            string dir = Path.Combine(IconFactory.ContentPath ?? ".", "icon_refs");
            Directory.CreateDirectory(dir);
            int count = 0;

            foreach (StatusEffectsRecord record in Data.StatusEffects.Records)
            {
                if (record.Id != PsyConfig.StressId && record.Id != PsyConfig.SedativeAddictionId
                    && record.ContentDescriptor is StatusEffectDescriptor descriptor && descriptor.StatusEffectIcon != null)
                {
                    count += Dump(descriptor.StatusEffectIcon, Path.Combine(dir, "status_" + record.Id + ".png")) ? 1 : 0;
                }
            }

            var wanted = new HashSet<string>
            {
                "common_pills", "common_health_regen", "common_fire_green", "common_no_effects",
                "statuseffect_alcoholAddiction_chance", "common_shield", "common_damage_bonus", "common_firerate"
            };
            foreach (TooltipIconEntry entry in Data.TooltipIcons.Entries)
            {
                if (entry.Sprite != null && !string.IsNullOrEmpty(entry.Tag) && !entry.Tag.StartsWith("statuseffect_stress")
                    && !entry.Tag.StartsWith("statuseffect_sedativeAddiction") && (all || wanted.Contains(entry.Tag)))
                {
                    count += Dump(entry.Sprite, Path.Combine(dir, "tooltip_" + entry.Tag + ".png")) ? 1 : 0;
                }
            }

            int itemsDumped = 0;
            foreach (BasePickupItemRecord item in Data.Items.Records)
            {
                if (itemsDumped >= 6 || !(item is CompositeItemRecord composite))
                {
                    continue;
                }
                foreach (BasePickupItemRecord record in composite.Records)
                {
                    if (record.Id != PsyConfig.SedativeItemId && record is ConsumableRecord consumable
                        && (consumable.ItemClass == ItemClass.Pills || consumable.ItemClass == ItemClass.Medpack || consumable.ItemClass == ItemClass.Alcohol)
                        && consumable.ContentDescriptor is ItemContentDescriptor itemDescriptor && itemDescriptor.Icon != null)
                    {
                        if (Dump(itemDescriptor.Icon, Path.Combine(dir, "item_" + record.Id + ".png")))
                        {
                            count++;
                            itemsDumped++;
                        }
                        break;
                    }
                }
            }

            return $"Dumped {count} icons to {dir}";
        }

        private static bool Dump(Sprite sprite, string path)
        {
            try
            {
                Texture2D texture = sprite.texture;
                RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(texture, temporary);
                RenderTexture active = RenderTexture.active;
                RenderTexture.active = temporary;
                Texture2D full = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: false);
                full.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                full.Apply();
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(temporary);

                Rect rect = sprite.textureRect;
                Texture2D cropped = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, mipChain: false);
                cropped.SetPixels(full.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
                cropped.Apply();
                File.WriteAllBytes(path, cropped.EncodeToPNG());
                Object.Destroy(full);
                Object.Destroy(cropped);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CombatPsychology] Failed to dump sprite to " + path + ": " + ex.Message);
                return false;
            }
        }

        public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
        {
            return null;
        }

        public static bool IsAvailable()
        {
            return true;
        }

        public static bool ShowInHelpAndAutocomplete()
        {
            return true;
        }
    }
}
