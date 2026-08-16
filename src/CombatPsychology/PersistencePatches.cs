using System.Reflection;
using HarmonyLib;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace CombatPsychology
{
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveGame))]
    internal static class SaveGame_Patch
    {
        private static readonly FieldInfo _stateField = AccessTools.Field(typeof(SaveManager), "_state");

        private static void Postfix(SaveManager __instance, bool isAutoSave, bool isReport)
        {
            State state = (State)_stateField.GetValue(__instance);
            SavedGameMetadata savedGameMetadata = state.Get<SavedGameMetadata>();
            if (savedGameMetadata != null && savedGameMetadata.Slot != -1)
            {
                string arg = (isReport ? "report_" : (isAutoSave ? "autosave_" : ""));
                string data = SaveToJSON.CreateNode(PsycheStore.Current).ToString();
                SingletonMonoBehaviour<FileManager>.Instance.SaveFile($"{arg}slot_{savedGameMetadata.Slot}_psyche.dat", data);
            }
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadGame))]
    internal static class LoadGame_Patch
    {
        private static void Postfix(int slot, bool isAutoSave, ELoadResult __result)
        {
            if (__result != ELoadResult.Success)
            {
                return;
            }
            PsycheStore.ResetAll();
            string arg = (isAutoSave ? "autosave_" : "");
            string filename = $"{arg}slot_{slot}_psyche.dat";
            if (!SingletonMonoBehaviour<FileManager>.Instance.IsFileExist(filename))
            {
                return;
            }
            string text = SingletonMonoBehaviour<FileManager>.Instance.LoadTextFile(filename);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            try
            {
                JSONNode node = JSON.Parse(text);
                PsycheStore.Current.LoadJSON(node);
                Debug.Log($"[CombatPsychology] Loaded psyche data for {PsycheStore.Current.Entries.Count} mercs.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[CombatPsychology] Failed to load psyche data: " + ex.Message);
                PsycheStore.ResetAll();
            }
        }
    }

    [HarmonyPatch(typeof(ComponentsLayout), nameof(ComponentsLayout.CreateGlobalComponents))]
    internal static class NewGame_Patch
    {
        private static void Postfix()
        {
            PsycheStore.ResetAll();
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.RemoveSlotSave))]
    internal static class RemoveSlotSave_Patch
    {
        private static void Postfix(int slot)
        {
            SingletonMonoBehaviour<FileManager>.Instance.RemoveFile($"slot_{slot}_psyche.dat");
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.RemoveAutoSaves))]
    internal static class RemoveAutoSaves_Patch
    {
        private static void Postfix(int slot)
        {
            SingletonMonoBehaviour<FileManager>.Instance.RemoveFile($"autosave_slot_{slot}_psyche.dat");
        }
    }
}
