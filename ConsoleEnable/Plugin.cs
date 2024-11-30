using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ConsoleEnable
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Luma Island.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        public static ConfigEntry<KeyboardShortcut> OpenConsoleKeybind;
        public static KeyboardShortcut OpenConsoleKeybindValue => OpenConsoleKeybind.Value;
        private void Awake()
        {
            Logger = base.Logger;
            OpenConsoleKeybind = Config.Bind(
                "Keybinds",
                "OpenConsole",
                new KeyboardShortcut(KeyCode.BackQuote),
                "Keybind to open the console"
            );

            Harmony.CreateAndPatchAll(typeof(Hooks));
            Harmony.CreateAndPatchAll(typeof(ConsoleUIPatch));
            Harmony.CreateAndPatchAll(typeof(ConsoleUIUpdatePrefixPatch));
            Harmony.CreateAndPatchAll(typeof(ChestCheatPatch));
            Logger.LogWarning("Cheats are now enabled by default.");
            Logger.LogWarning($"Press {OpenConsoleKeybind.Value.MainKey} to open the console after loading a save.");
            Logger.LogWarning("If that fails, use CTRL + F1");
            Logger.LogWarning("Enter 'help' to see the list of available commands.");
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

    [HarmonyPatch(typeof(ConsoleUI), "Update")]
    public static class ConsoleUIUpdatePrefixPatch
    {
        static bool Prefix(ConsoleUI __instance)
        {
            if (__instance.IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                __instance.Close();
                return false;
            }

            if (Plugin.OpenConsoleKeybindValue.IsDown())
            {
                FieldInfo panelField = AccessTools.Field(typeof(ConsoleUI), "m_panel");
                GameObject panel = (GameObject)panelField.GetValue(__instance);
                if (panel.activeSelf)
                {
                    __instance.Close();
                }
                else
                {
                    __instance.Open();
                    __instance.PlayerGameUI.Player.GetComponent<ControlContextManager>().SetConsoleActive(true);
                    __instance.m_input.FixedSelect();
                }
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ConsoleUI), "ChestCheat")]
    public static class ChestCheatPatch
    {
        static bool Prefix(string[] obj)
        {
            var globalStates = GameState.Instance.GlobalStates;
            float currentValue = globalStates.GetValue("ChestCheat");
            float newValue = (currentValue == 1f) ? 0f : 1f;
            globalStates.SetValue("ChestCheat", newValue, false);
            if (newValue == 1f)
            {
                Plugin.Logger.LogWarning("Chest cheat enabled.");
            }
            else
            {
                Plugin.Logger.LogWarning("Chest cheat disabled.");
            }
            return false;
        }
    }
}
