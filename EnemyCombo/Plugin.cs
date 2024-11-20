using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace EnemyCombo;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static ConfigEntry<bool> EnableGhostSpawning;
    public static ConfigEntry<int> MaxGhosts;
    public static ConfigEntry<float> SpawnTimingMin;
    public static ConfigEntry<float> SpawnTimingMax;
    public static ConfigEntry<bool> SpawnSpiderEveryXSeconds;
    public static ConfigEntry<float> TimeBetweenSpiderSpawns;

    private void Awake()
    {
        Logger = base.Logger;
        EnableGhostSpawning = Config.Bind<bool>(
            "General",
            "EnableGhostSpawning",
            true,
            "Enable or disable ghosts spawning."
        );
        MaxGhosts = Config.Bind<int>(
            "General",
            "MaxGhosts",
            2,
            new ConfigDescription(
                "Maximum number of ghosts.",
                new AcceptableValueRange<int>(0, 100)
            )
        );
        SpawnTimingMin = Config.Bind<float>(
            "SpawnTiming",
            "SpawnTimingMin",
            5f,
            new ConfigDescription(
                "Minimum time between ghost spawns (in seconds).",
                new AcceptableValueRange<float>(1f, 600f)
            )
        );
        SpawnTimingMax = Config.Bind<float>(
            "SpawnTiming",
            "SpawnTimingMax",
            10f,
            new ConfigDescription(
                "Maximum time between ghost spawns (in seconds).",
                new AcceptableValueRange<float>(1f, 600f)
            )
        );
        SpawnSpiderEveryXSeconds = Config.Bind<bool>(
            "Spiders",
            "SpawnSpiderEveryXSeconds",
            true,
            "Enable or disable spider spawning every X seconds."
        );
        TimeBetweenSpiderSpawns = Config.Bind<float>(
            "Spiders",
            "TimeBetweenSpiderSpawns",
            60f,
            "Time between spider spawns in seconds."
        );
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        Logger.LogWarning("Ghost Spawning: " + EnableGhostSpawning.Value);
        if (EnableGhostSpawning.Value)
        {
            Logger.LogWarning("Max Ghosts to Spawn: " + MaxGhosts.Value);
            Logger.LogWarning("Spawn Timing Min: " + SpawnTimingMin.Value);
            Logger.LogWarning("Spawn Timing Max: " + SpawnTimingMax.Value);
        }
        Logger.LogWarning("Spider Spawns: " + SpawnSpiderEveryXSeconds.Value);
        if (SpawnSpiderEveryXSeconds.Value)
        {
            Logger.LogWarning("Time Between Spawns: " + TimeBetweenSpiderSpawns.Value);
        }
    }

    [HarmonyPatch(typeof(CaveDungeonController), "OnCreate")]
    public class CaveDungeonController_OnCreate_Patch
    {
        static void Prefix(CaveDungeonController __instance)
        {
            FieldInfo spawnSpiderEveryXSecondsField = typeof(CaveDungeonController).GetField("m_spawnSpiderEveryXSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo timeBetweenSpiderSpawnsField = typeof(CaveDungeonController).GetField("m_timeBetweenSpiderSpawns", BindingFlags.NonPublic | BindingFlags.Instance);

            if (spawnSpiderEveryXSecondsField != null)
            {
                spawnSpiderEveryXSecondsField.SetValue(__instance, SpawnSpiderEveryXSeconds.Value);
            }

            if (timeBetweenSpiderSpawnsField != null)
            {
                timeBetweenSpiderSpawnsField.SetValue(__instance, TimeBetweenSpiderSpawns.Value);
            }
        }
    }

    [HarmonyPatch(typeof(GhostAreaSpawner), "Awake")]
    public class GhostAreaSpawner_Awake_Patch
    {
        static void Prefix(GhostAreaSpawner __instance)
        {
            FieldInfo maxGhostsField = typeof(GhostAreaSpawner).GetField("m_maxGhosts", BindingFlags.NonPublic | BindingFlags.Instance);
            if (maxGhostsField != null)
            {
                int maxGhostsValue = Mathf.Clamp(MaxGhosts.Value, 0, 100);
                maxGhostsField.SetValue(__instance, maxGhostsValue);
            }
            FieldInfo spawnTimingField = typeof(GhostAreaSpawner).GetField("m_spawnTiming", BindingFlags.NonPublic | BindingFlags.Instance);
            if (spawnTimingField != null)
            {
                float minValue = Mathf.Clamp(SpawnTimingMin.Value, 1f, 600f);
                float maxValue = Mathf.Clamp(SpawnTimingMax.Value, minValue, 600f);
                Vector2 spawnTiming = new Vector2(minValue, maxValue);
                spawnTimingField.SetValue(__instance, spawnTiming);
            }
        }
    }

    [HarmonyPatch(typeof(GhostAreaSpawner), "Update")]
    public class GhostAreaSpawner_Update_Patch
    {
        static bool Prefix()
        {
            return EnableGhostSpawning.Value;
        }
    }
}
