using System.Collections.Generic;
using System.IO;
using MGSC;
using UnityEngine;

namespace Mechs
{
    [ConsoleCommand(new string[] { "mech_dumptex" })]
    public class DumpTexCommand
    {
        public static string Help(string command, bool verbose)
        {
            return "Dumps the vanilla textures, meshes' paint and icons the Atlas mech reuses (power armor set, minigun, drill, vest, ammo) to the mod folder as art references.";
        }

        public string Execute(string[] tokens)
        {
            string dir = Path.Combine(IconFactory.ContentPath ?? ".", "art_refs");
            Directory.CreateDirectory(dir);
            int count = 0;
            count += DumpItem(MechConfig.ChassisTemplate, dir);
            count += DumpItem(MechConfig.HeadTemplate, dir);
            count += DumpItem(MechConfig.LegsTemplate, dir);
            count += DumpItem(MechConfig.BootsTemplate, dir);
            count += DumpItem(MechConfig.FeedTemplate, dir);
            count += DumpItem(MechConfig.CannonTemplate, dir);
            count += DumpItem(MechConfig.DrillTemplate, dir);
            count += DumpItem(MechConfig.ShellsTemplate, dir);
            count += DumpItem(MechConfig.MechItemTemplate, dir);
            return $"Dumped {count} textures to {dir}";
        }

        private static int DumpItem(string itemId, string dir)
        {
            if (!(Data.Items.GetRecord(itemId) is CompositeItemRecord composite)
                || !(composite.PrimaryRecord.ContentDescriptor is ItemContentDescriptor descriptor))
            {
                Debug.LogWarning("[Mechs] No descriptor to dump for " + itemId);
                return 0;
            }
            int count = 0;
            if (descriptor.Icon != null)
            {
                count += DumpSprite(descriptor.Icon, Path.Combine(dir, itemId + "_icon.png")) ? 1 : 0;
            }
            if (descriptor.SmallIcon != null)
            {
                count += DumpSprite(descriptor.SmallIcon, Path.Combine(dir, itemId + "_smallicon.png")) ? 1 : 0;
            }
            if (descriptor is WeaponDescriptor weaponDescriptor && weaponDescriptor.Texture != null)
            {
                count += DumpTexture(weaponDescriptor.Texture, Path.Combine(dir, itemId + "_mesh_texture.png")) ? 1 : 0;
            }
            foreach (ArmorPartInfo part in GetArmorParts(descriptor))
            {
                if (part.Texture != null)
                {
                    string name = itemId + "_part_" + part.ArmorType + "_" + part.ArmorPart + ".png";
                    count += DumpTexture(part.Texture, Path.Combine(dir, name)) ? 1 : 0;
                }
            }
            return count;
        }

        private static IEnumerable<ArmorPartInfo> GetArmorParts(ItemContentDescriptor descriptor)
        {
            switch (descriptor)
            {
                case ArmorDescriptor armor when armor.Parts != null:
                    return armor.Parts;
                case HelmetDescriptor helmet when helmet.Parts != null:
                    return helmet.Parts;
                case LeggingsDescriptor leggings when leggings.Parts != null:
                    return leggings.Parts;
                case BootsDescriptor boots when boots.Parts != null:
                    return boots.Parts;
                default:
                    return new List<ArmorPartInfo>();
            }
        }

        private static bool DumpSprite(Sprite sprite, string path)
        {
            try
            {
                Texture2D full = ReadTexture(sprite.texture);
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
                Debug.LogWarning("[Mechs] Failed to dump sprite to " + path + ": " + ex.Message);
                return false;
            }
        }

        private static bool DumpTexture(Texture texture, string path)
        {
            try
            {
                Texture2D readable = ReadTexture(texture);
                File.WriteAllBytes(path, readable.EncodeToPNG());
                Object.Destroy(readable);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Mechs] Failed to dump texture to " + path + ": " + ex.Message);
                return false;
            }
        }

        private static Texture2D ReadTexture(Texture texture)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, temporary);
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = temporary;
            Texture2D result = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: false);
            result.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            result.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temporary);
            return result;
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
