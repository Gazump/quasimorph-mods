using MGSC;
using UnityEngine;

namespace SampleMod
{
    public static class ModMain
    {
        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfigsLoaded(IModContext context)
        {
            Debug.Log("[SampleMod] AfterConfigsLoaded. Content path: " + context.ModContentPath);

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
