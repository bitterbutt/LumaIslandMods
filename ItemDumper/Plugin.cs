using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ItemDumper
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(InventoryItems_OnEnable_Patch));
        }

        [HarmonyPatch(typeof(InventoryItems), "OnEnable")]
        public class InventoryItems_OnEnable_Patch
        {
            static async void Postfix(InventoryItems __instance)
            {
                Logger.LogWarning("Waiting for 30 seconds before dumping item data...");
                await Task.Delay(30000);
                Logger.LogWarning("Dumping item data...");
                var dataArray = __instance.dataArray;
                var items = new List<ItemInfo>();
                foreach (var itemData in dataArray)
                {
                    string name = itemData.Name.Replace(",", "\\,");
                    string localizedName = itemData.GetLocalizedName().Replace(",", "\\,");
                    // Logger.LogInfo($"{name} - {localizedName}");
                    items.Add(new ItemInfo { Name = name, LocalizedName = localizedName });
                }
                var sortedItems = items.OrderBy(item => item.LocalizedName).ToList();
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("Name,LocalizedName");
                foreach (var item in sortedItems)
                {
                    csv.AppendLine($"{item.Name},{item.LocalizedName}");
                }
                string pluginDir = Path.GetDirectoryName(Plugin.Instance.Info.Location);
                string outputPath = Path.Combine(pluginDir, "ItemDump.csv");
                File.WriteAllText(outputPath, csv.ToString());
                Logger.LogWarning($"Item dump saved to {outputPath}");
            }
        }
    }

    public class ItemInfo
    {
        public string Name { get; set; }
        public string LocalizedName { get; set; }
    }
}