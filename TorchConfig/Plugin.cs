using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;

namespace TorchConfig;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static ConfigEntry<bool> DisableTorchFuelConsumption;
    public static ConfigEntry<float> TorchFuelConsumptionMultiplier;

    private void Awake()
    {
        Logger = base.Logger;
        DisableTorchFuelConsumption = Config.Bind<bool>(
            "General",
            "DisableTorchFuelConsumption",
            false,
            "Set to true to disable torch fuel consumption entirely."
        );
        TorchFuelConsumptionMultiplier = Config.Bind<float>(
            "General",
            "TorchFuelConsumptionMultiplier",
            1f,
            new ConfigDescription(
                "Multiplier for torch fuel consumption rate. Set to less than 1 to slow down fuel consumption, greater than 1 to speed it up.",
                new AcceptableValueRange<float>(0f, 100f)
            )
        );
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        Logger.LogWarning("Torch Consumption: " + !DisableTorchFuelConsumption.Value);
        if (!DisableTorchFuelConsumption.Value)
        {
            Logger.LogWarning("Torch Consumption Multiplier: " + TorchFuelConsumptionMultiplier.Value);
        }
    }

    [HarmonyPatch(typeof(Torch), "TickFuel")]
    public class Torch_TickFuel_Patch
    {
        static void Prefix(ref float deltaTime)
        {
            if (DisableTorchFuelConsumption.Value)
            {
                deltaTime = 0f;
            }
            else
            {
                deltaTime *= TorchFuelConsumptionMultiplier.Value;
            }
        }
    }
}
