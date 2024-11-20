using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;

namespace SpiderSpawn;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static ConfigEntry<bool> SpawnSpiderEveryXSeconds;
    public static ConfigEntry<float> TimeBetweenSpiderSpawns;

    private void Awake()
    {
        Logger = base.Logger;
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
}
