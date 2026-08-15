using MGSC;
using UnityEngine;

namespace SampleMod
{
    // Quasimorph reflects over every type in the assembly and collects public static methods
    // carrying [Hook], so the class name does not matter.
    public static class ModMain
    {
        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfigsLoaded(IModContext context)
        {
            Debug.Log("[SampleMod] AfterConfigsLoaded. Content path: " + context.ModContentPath);

            // Harmony ships with the game, so patching needs no extra dependency.
            // new Harmony("com.yourname.samplemod").PatchAll(typeof(ModMain).Assembly);
        }

        [Hook(ModHookType.MainMenuStarted)]
        public static void MainMenuStarted(IModContext context)
        {
            Debug.Log("[SampleMod] MainMenuStarted.");
        }

        [Hook(ModHookType.DungeonStarted)]
        public static void DungeonStarted(IModContext context)
        {
            Debug.Log("[SampleMod] DungeonStarted.");
        }
    }
}
