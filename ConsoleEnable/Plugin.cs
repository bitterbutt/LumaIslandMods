using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ConsoleEnable
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Luma Island.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Setup();
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogWarning("Cheats are now enabled by default.");
            Logger.LogWarning("Press ` to open the console after loading a save...");
            Logger.LogWarning("Enter 'help' to see the list of available commands...");
        }

        private void Setup()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
            Harmony.CreateAndPatchAll(typeof(ConsoleUIPatch));
        }

        public static void NoOpMethod()
        {
            // Do nothing
        }
    }

    internal static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UnityEngine.Debug), nameof(UnityEngine.Debug.isDebugBuild), MethodType.Getter)]
        private static void Patch_isDebugBuild(ref bool __result) => __result = true;
    }

    [HarmonyPatch(typeof(Debug), "isDebugBuild", MethodType.Getter)]
    public static class DebugIsDebugBuildAndIsEditorPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("get_isDebugBuild"))
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4_1);
                }

                if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("get_isEditor"))
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4_1);
                }
            }
            return codes;
        }
    }

    [HarmonyPatch(typeof(ConsoleUI), "DoCommand")]
    public static class ConsoleUIPatch
    {
        static void Prefix(ConsoleUI __instance)
        {
            FieldInfo cheatsEnabledField = AccessTools.Field(typeof(ConsoleUI), "m_cheatsEnabled");
            if (cheatsEnabledField != null)
            {
                cheatsEnabledField.SetValue(__instance, true);
            }
        }
    }
}
